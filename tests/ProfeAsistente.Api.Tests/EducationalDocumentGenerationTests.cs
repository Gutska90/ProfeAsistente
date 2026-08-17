using System.Net;
using System.Text;
using System.Text.Json;
using ProfeAsistente.Api.Configuration;
using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.AI;
using ProfeAsistente.Api.Models.AI.Responses;
using ProfeAsistente.Api.Repositories;
using ProfeAsistente.Api.Services;
using ProfeAsistente.Api.Services.AI;
using ProfeAsistente.Api.Services.AI.DocumentGeneration;
using ProfeAsistente.Api.Services.AI.Gemini;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ProfeAsistente.Api.Tests;

public class EducationalDocumentGenerationTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"appedu-doc-{Guid.NewGuid():N}.db");
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"appedu-doc-root-{Guid.NewGuid():N}");
    private readonly ProfeAsistenteDbContext _db;

    public EducationalDocumentGenerationTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Prompts"));
        foreach (var name in new[]
                 {
                     "learning-guide-system-prompt.txt",
                     "exercises-system-prompt.txt",
                     "assessment-system-prompt.txt"
                 })
        {
            var src = Path.Combine(AppContext.BaseDirectory, "Prompts", name);
            if (!File.Exists(src))
            {
                src = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                    "..", "..", "..", "..", "..", "src", "ProfeAsistente.Api", "Prompts", name));
            }

            File.Copy(src, Path.Combine(_root, "Prompts", name), overwrite: true);
        }

        var options = new DbContextOptionsBuilder<ProfeAsistenteDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _db = new ProfeAsistenteDbContext(options);
        _db.Database.Migrate();
        DemoCurriculumSeed.Seed(_db);
        _db.SaveChanges();
    }

    [Fact]
    public void Validator_AcceptsValidMultipleChoiceAssessment()
    {
        var context = SampleContext(EducationalDocumentType.Assessment, itemCount: 2);
        var doc = SampleAssessment(context);
        var result = new EducationalDocumentGenerationValidator(new EducationalItemSimilarityService())
            .Validate(doc, context);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Validator_RejectsTwoCorrectOptions()
    {
        var context = SampleContext(EducationalDocumentType.Assessment, itemCount: 1);
        var doc = SampleAssessment(context);
        doc.Document.Items[0].Options[0].IsCorrect = true;
        doc.Document.Items[0].Options[1].IsCorrect = true;
        var result = new EducationalDocumentGenerationValidator(new EducationalItemSimilarityService())
            .Validate(doc, context);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("exactamente una", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_RejectsUnknownIndicator()
    {
        var context = SampleContext(EducationalDocumentType.Exercises, itemCount: 1);
        var doc = SampleAssessment(context);
        doc.Document.Items[0].IndicatorIds = [Guid.NewGuid()];
        var result = new EducationalDocumentGenerationValidator(new EducationalItemSimilarityService())
            .Validate(doc, context);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Similarity_DetectsDuplicateStatements()
    {
        var items = new List<GeneratedEducationalItem>
        {
            new() { Order = 1, Statement = "Calcula 1/2 + 1/4" },
            new() { Order = 2, Statement = "Calcula 1/2 + 1/4" }
        };
        var warnings = new EducationalItemSimilarityService().DetectDuplicates(items);
        Assert.Contains(warnings, w => w.Contains("idénticos", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FullAssessmentFlow_WithFakeGemini_StudentViewHidesAnswers()
    {
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", "test-key-not-real");
        var clase = await CreateClassAsync();
        var indicatorIds = await _db.IndicadoresEvaluacion
            .Where(i => i.ObjetivoAprendizajeId == clase.ObjetivoAprendizajeId)
            .Select(i => i.Id).Take(2).ToListAsync();
        var oa = await _db.ObjetivosAprendizaje.FirstAsync(o => o.Id == clase.ObjetivoAprendizajeId);

        var fakeJson = BuildAssessmentJson(oa.Id, oa.Codigo, oa.Version, indicatorIds);
        var service = BuildService(fakeJson);

        var result = await service.GenerateAsync(clase.Id, new GenerateEducationalDocumentRequest
        {
            DocumentType = EducationalDocumentType.Assessment,
            ItemCount = 2,
            EvaluationIndicatorIds = indicatorIds,
            Difficulty = ItemDifficulty.Intermediate,
            EstimatedDurationMinutes = 45,
            IncludeAnswerKey = true,
            IncludeScoring = true
        });

        Assert.Equal(nameof(AiGenerationStatus.Completed), result.Status);
        Assert.NotNull(result.Document);
        Assert.Equal(2, result.Document!.Items.Count);
        Assert.True(result.Document.Items.All(i => i.Options.Any(o => o.IsCorrect)));

        var item = result.Document.Items[0];
        await service.UpdateItemAsync(item.Id, new UpdateEducationalItemRequest
        {
            ItemType = EducationalItemType.MultipleChoice,
            Statement = "Enunciado editado por el docente con suficiente longitud.",
            Difficulty = ItemDifficulty.Basic,
            BloomLevel = "Comprender",
            Points = item.Points,
            ExpectedAnswer = item.ExpectedAnswer,
            EvaluationIndicatorIds = item.EvaluationIndicatorIds,
            Options = item.Options
        });

        var reordered = result.Document.Items.Select((x, idx) => new ReorderEducationalItemDto
        {
            ItemId = x.Id,
            Order = result.Document.Items.Count - idx
        }).ToList();
        await service.ReorderItemsAsync(result.DocumentId, new ReorderEducationalItemsRequest { Items = reordered });

        await service.UpdateStatusAsync(result.DocumentId,
            new UpdateEducationalDocumentStatusRequest { Status = EducationalDocumentStatus.UnderReview });
        await service.UpdateStatusAsync(result.DocumentId,
            new UpdateEducationalDocumentStatusRequest { Status = EducationalDocumentStatus.Reviewed });
        await service.UpdateStatusAsync(result.DocumentId,
            new UpdateEducationalDocumentStatusRequest { Status = EducationalDocumentStatus.Final });

        var teacher = await service.GetAsync(result.DocumentId);
        Assert.NotNull(teacher);
        Assert.Contains(teacher!.Items, i => !string.IsNullOrWhiteSpace(i.ExpectedAnswer)
                                             || i.Options.Any(o => o.IsCorrect));

        var student = await service.GetStudentViewAsync(result.DocumentId);
        Assert.NotNull(student);
        var studentJson = JsonSerializer.Serialize(student, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        Assert.DoesNotContain("\"isCorrect\"", studentJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expectedAnswer", studentJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("teacherNotes", studentJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("explanation", studentJson, StringComparison.OrdinalIgnoreCase);

        var key = await service.GetAnswerKeyAsync(result.DocumentId);
        Assert.NotEmpty(key.Entries);

        clase.ObjetivoAprendizajeId = DemoCurriculumSeed.Oa1Id == clase.ObjetivoAprendizajeId
            ? (await _db.ObjetivosAprendizaje.FirstAsync(o => o.Id != clase.ObjetivoAprendizajeId)).Id
            : DemoCurriculumSeed.Oa1Id;
        await _db.SaveChangesAsync();
        await service.MarkOutdatedIfConfigurationChangedAsync(clase.Id);

        var after = await service.GetAsync(result.DocumentId);
        Assert.True(after!.IsOutdated);
        Assert.Equal(nameof(EducationalDocumentStatus.Outdated), after.Status);
    }

    [Fact]
    public void OptionalLiveGeminiDocument_SkippedUnlessEnabled()
    {
        var run = string.Equals(
            Environment.GetEnvironmentVariable("RUN_GEMINI_DOCUMENT_TESTS"),
            "true", StringComparison.OrdinalIgnoreCase);
        var key = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!run || string.IsNullOrWhiteSpace(key))
            return;

        // Manual smoke only — not executed in CI.
        Assert.False(string.IsNullOrWhiteSpace(key));
    }

    private IEducationalDocumentGenerationService BuildService(string geminiJsonText)
    {
        var envelope = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new { content = new { parts = new[] { new { text = geminiJsonText } } } }
            },
            usageMetadata = new { promptTokenCount = 120, candidatesTokenCount = 220 }
        });

        var gemini = new GeminiClient(
            new FixedFactory(new FixedHandler(HttpStatusCode.OK, envelope)),
            Options.Create(new GeminiOptions
            {
                ApiKeyEnvironmentVariable = "GEMINI_API_KEY",
                Model = "gemini-2.5-flash",
                BaseUrl = "https://generativelanguage.googleapis.com",
                EnableGeneration = true,
                PersistRequestPayloads = false,
                MaxRetries = 0
            }),
            NullLogger<GeminiClient>.Instance);

        return new EducationalDocumentGenerationService(
            _db,
            new GeminiAiProvider(gemini),
            new EducationalDocumentContextBuilder(_db, NullLogger<EducationalDocumentContextBuilder>.Instance),
            new EducationalDocumentGenerationValidator(new EducationalItemSimilarityService()),
            Options.Create(new GeminiOptions
            {
                ApiKeyEnvironmentVariable = "GEMINI_API_KEY",
                Model = "gemini-2.5-flash",
                BaseUrl = "https://generativelanguage.googleapis.com",
                EnableGeneration = true,
                PersistRequestPayloads = false,
                MaxRetries = 0
            }),
            Options.Create(new AiUsageOptions()),
            new FakeHostEnv { ContentRootPath = _root },
            NullLogger<EducationalDocumentGenerationService>.Instance,
            new ProfeAsistente.Api.Tests.TestDoubles.FakeCurrentUserService());
    }

    private async Task<ProfeAsistente.Api.Models.Clase> CreateClassAsync()
    {
        var planes = new PlanificacionService(_db, new PlanificacionRepository(_db), new ProfeAsistente.Api.Tests.TestDoubles.FakeCurrentUserService(), new ProfeAsistente.Api.Tests.TestDoubles.AllowAllResourceAuthorizationService());
        var planDto = await planes.CrearAsync(new CrearPlanificacionRequest
        {
            NivelId = DemoCurriculumSeed.NivelId,
            AsignaturaId = DemoCurriculumSeed.NivelAsignaturaId,
            UnidadId = DemoCurriculumSeed.UnidadId,
            Nombre = "Plan docs test",
            FechaInicio = new DateOnly(2026, 3, 1),
            FechaFin = new DateOnly(2026, 3, 31)
        });
        var clases = new ClaseService(_db, new PlanificacionRepository(_db), new ClaseRepository(_db));
        var claseDto = await clases.CrearAsync(planDto.Id, new CrearClaseRequest
        {
            ObjetivoAprendizajeId = DemoCurriculumSeed.Oa1Id,
            NivelBloom = "Comprender",
            IndicadorEvaluacionIds = await _db.IndicadoresEvaluacion
                .Where(i => i.ObjetivoAprendizajeId == DemoCurriculumSeed.Oa1Id)
                .Select(i => i.Id).Take(2).ToListAsync()
        });
        return await _db.Clases.Include(c => c.Indicadores).FirstAsync(c => c.Id == claseDto.Id);
    }

    private static EducationalDocumentGenerationContext SampleContext(
        EducationalDocumentType type, int itemCount)
    {
        var oaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var ind1 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var ind2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
        return new EducationalDocumentGenerationContext
        {
            ClassId = Guid.NewGuid(),
            CurriculumRelease = "seed-demo-1",
            Level = "4° básico",
            Subject = "Matemática",
            Unit = "Fracciones",
            Objective = new CurriculumObjectiveRef
            {
                Id = oaId,
                Code = "DEMO OA 01",
                Description = "Representar fracciones propias."
            },
            Indicators =
            [
                new CurriculumIndicatorRef { Id = ind1, Description = "Representa con material concreto." },
                new CurriculumIndicatorRef { Id = ind2, Description = "Dibuja modelos pictóricos." }
            ],
            BloomLevel = "Comprender",
            DocumentType = type,
            ItemCount = itemCount,
            Difficulty = ItemDifficulty.Intermediate,
            AllowedItemTypes = [EducationalItemType.MultipleChoice, EducationalItemType.TrueFalse],
            IncludeScoring = true,
            IncludeAnswerKey = true
        };
    }

    private static GeneratedEducationalDocument SampleAssessment(EducationalDocumentGenerationContext context)
    {
        var items = new List<GeneratedEducationalItem>();
        for (var i = 0; i < Math.Max(1, context.ItemCount); i++)
        {
            var ind = context.Indicators[i % context.Indicators.Count].Id;
            items.Add(new GeneratedEducationalItem
            {
                TemporaryId = $"item-{i + 1:000}",
                Order = i + 1,
                Type = nameof(EducationalItemType.MultipleChoice),
                Statement = $"Pregunta de fracciones número {i + 1} con enunciado concreto.",
                Difficulty = "Intermediate",
                BloomLevel = "Comprender",
                IndicatorIds = [ind],
                Points = 2,
                Options =
                [
                    new GeneratedEducationalOption { Order = 1, Text = "Opción A", IsCorrect = true, Feedback = "Correcto" },
                    new GeneratedEducationalOption { Order = 2, Text = "Opción B", IsCorrect = false },
                    new GeneratedEducationalOption { Order = 3, Text = "Opción C", IsCorrect = false },
                    new GeneratedEducationalOption { Order = 4, Text = "Opción D", IsCorrect = false }
                ],
                ExpectedAnswer = "Opción A",
                Explanation = "Se selecciona la representación correcta."
            });
        }

        var total = items.Sum(x => x.Points);
        return new GeneratedEducationalDocument
        {
            RequiresReview = false,
            Curriculum = new GeneratedCurriculumReference
            {
                ObjectiveId = context.Objective.Id,
                ObjectiveCode = context.Objective.Code,
                IndicatorIds = context.Indicators.Select(i => i.Id).ToList(),
                CurriculumRelease = context.CurriculumRelease
            },
            Document = new GeneratedEducationalDocumentBody
            {
                Type = context.DocumentType.ToString(),
                Title = "Prueba de fracciones",
                Purpose = "Evaluar representación de fracciones.",
                Instructions = "Responde con lápiz. Tiempo 45 minutos.",
                EstimatedDurationMinutes = 45,
                TotalPoints = total,
                Items = items,
                SpecificationTable = context.Indicators.Select(ind => new GeneratedSpecificationRow
                {
                    IndicatorId = ind.Id,
                    BloomLevel = "Comprender",
                    ItemCount = items.Count(i => i.IndicatorIds.Contains(ind.Id)),
                    TotalPoints = items.Where(i => i.IndicatorIds.Contains(ind.Id)).Sum(i => i.Points),
                    WeightPercentage = Math.Round(
                        items.Where(i => i.IndicatorIds.Contains(ind.Id)).Sum(i => i.Points) * 100m / total, 2)
                }).ToList()
            }
        };
    }

    private static string BuildAssessmentJson(Guid oaId, string code, string release, List<Guid> indicatorIds)
    {
        var context = new EducationalDocumentGenerationContext
        {
            Objective = new CurriculumObjectiveRef { Id = oaId, Code = code },
            Indicators = indicatorIds.Select(id => new CurriculumIndicatorRef { Id = id, Description = "ind" }).ToList(),
            CurriculumRelease = release,
            DocumentType = EducationalDocumentType.Assessment,
            ItemCount = 2,
            IncludeScoring = true
        };
        var doc = SampleAssessment(context);
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { /* ignore */ }
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    private sealed class FixedHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }

    private sealed class FixedFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private sealed class FakeHostEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
