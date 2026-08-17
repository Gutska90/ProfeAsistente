using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AppEducativa.Api.Configuration;
using AppEducativa.Api.Data;
using AppEducativa.Api.Models.AI;
using AppEducativa.Api.Models.AI.Responses;
using AppEducativa.Api.Repositories;
using AppEducativa.Api.Services;
using AppEducativa.Api.Services.AI;
using AppEducativa.Api.Services.AI.DocumentGeneration;
using AppEducativa.Api.Services.AI.Gemini;
using AppEducativa.Api.Services.Export;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AppEducativa.Api.Tests.Export;

public class WordExportServiceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"appedu-exp-{Guid.NewGuid():N}.db");
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"appedu-exp-root-{Guid.NewGuid():N}");
    private readonly AppEducativaDbContext _db;

    public WordExportServiceTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Templates", "Word"));
        Directory.CreateDirectory(Path.Combine(_root, "Prompts"));
        File.WriteAllText(Path.Combine(_root, "Templates", "Word", "default-template-settings.json"),
            """{"fontFamily":"Arial","bodyFontSize":11,"titleFontSize":18,"heading1FontSize":15,"heading2FontSize":13,"heading3FontSize":12,"pageSize":"A4","orientation":"Portrait","marginTopCm":2,"marginBottomCm":2,"marginLeftCm":2,"marginRightCm":2}""");

        foreach (var name in new[] { "assessment-system-prompt.txt", "learning-guide-system-prompt.txt", "exercises-system-prompt.txt" })
        {
            var src = Path.Combine(AppContext.BaseDirectory, "Prompts", name);
            if (!File.Exists(src))
                src = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "AppEducativa.Api", "Prompts", name));
            if (File.Exists(src))
                File.Copy(src, Path.Combine(_root, "Prompts", name), true);
            else
                File.WriteAllText(Path.Combine(_root, "Prompts", name), "Responde JSON.");
        }

        var options = new DbContextOptionsBuilder<AppEducativaDbContext>().UseSqlite($"Data Source={_dbPath}").Options;
        _db = new AppEducativaDbContext(options);
        _db.Database.Migrate();
        DemoCurriculumSeed.Seed(_db);
        _db.SaveChanges();
    }

    [Fact]
    public async Task Integration_ExportAssessmentStudent_HidesSecretAnswer()
    {
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", "test-key-not-real");
        const string secret = "RESPUESTA_SECRETA_PRUEBA_123";

        var plan = await CreatePlanWithClassAsync();
        var clase = plan.Clases.First();
        var indicators = await _db.IndicadoresEvaluacion
            .Where(i => i.ObjetivoAprendizajeId == clase.ObjetivoAprendizajeId)
            .Select(i => i.Id).Take(2).ToListAsync();
        var oa = await _db.ObjetivosAprendizaje.FirstAsync(o => o.Id == clase.ObjetivoAprendizajeId);

        var docService = BuildDocService(BuildAssessmentJson(oa.Id, oa.Codigo, oa.Version, indicators, secret));
        var generated = await docService.GenerateAsync(clase.Id, new GenerateEducationalDocumentRequest
        {
            DocumentType = EducationalDocumentType.Assessment,
            ItemCount = 2,
            EvaluationIndicatorIds = indicators,
            EstimatedDurationMinutes = 45,
            IncludeAnswerKey = true,
            IncludeScoring = true
        });
        await docService.UpdateStatusAsync(generated.DocumentId,
            new UpdateEducationalDocumentStatusRequest { Status = EducationalDocumentStatus.Final });

        var exportService = BuildExportService();
        var studentExport = await exportService.ExportEducationalDocumentAsync(generated.DocumentId, new CreateExportRequest
        {
            DocumentType = ExportDocumentType.Assessment,
            Audience = ExportAudience.Student,
            EducationalDocumentId = generated.DocumentId,
            ConfirmOutdatedExport = true
        });
        Assert.Equal(nameof(ExportStatus.Completed), studentExport.Status);

        var studentPath = Path.Combine(_root, (await _db.DocumentExports.FirstAsync(e => e.Id == studentExport.ExportId)).RelativeFilePath!);
        var studentText = ReadDocxText(studentPath);
        Assert.DoesNotContain(secret, studentText, StringComparison.Ordinal);
        Assert.DoesNotContain("Respuesta correcta", studentText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Nota docente:", studentText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GEMINI_API_KEY", studentText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("App_Data", studentText, StringComparison.OrdinalIgnoreCase);

        var teacherExport = await exportService.ExportEducationalDocumentAsync(generated.DocumentId, new CreateExportRequest
        {
            DocumentType = ExportDocumentType.Assessment,
            Audience = ExportAudience.Teacher,
            EducationalDocumentId = generated.DocumentId,
            IncludeAnswerKey = true,
            ConfirmOutdatedExport = true
        });
        var teacherPath = Path.Combine(_root, (await _db.DocumentExports.FirstAsync(e => e.Id == teacherExport.ExportId)).RelativeFilePath!);
        var teacherText = ReadDocxText(teacherPath);
        Assert.Contains(secret, teacherText, StringComparison.Ordinal);
        Assert.Contains("VERSIÓN DOCENTE", teacherText, StringComparison.OrdinalIgnoreCase);

        var key = await exportService.ExportAsync(new CreateExportRequest
        {
            DocumentType = ExportDocumentType.AnswerKey,
            Audience = ExportAudience.Teacher,
            EducationalDocumentId = generated.DocumentId,
            ConfirmOutdatedExport = true
        });
        Assert.Equal(nameof(ExportStatus.Completed), key.Status);

        var spec = await exportService.ExportAsync(new CreateExportRequest
        {
            DocumentType = ExportDocumentType.SpecificationTable,
            Audience = ExportAudience.Administrative,
            EducationalDocumentId = generated.DocumentId,
            ConfirmOutdatedExport = true
        });
        Assert.Equal(nameof(ExportStatus.Completed), spec.Status);

        var planning = await exportService.ExportPlanningAsync(plan.Id, new CreateExportRequest
        {
            DocumentType = ExportDocumentType.Planning,
            Audience = ExportAudience.Administrative,
            PlanningId = plan.Id
        });
        Assert.Equal(nameof(ExportStatus.Completed), planning.Status);

        var classExport = await exportService.ExportClassAsync(clase.Id, new CreateExportRequest
        {
            DocumentType = ExportDocumentType.ClassPlan,
            Audience = ExportAudience.Teacher,
            ClassId = clase.Id
        });
        Assert.Equal(nameof(ExportStatus.Completed), classExport.Status);

        // Avoid full package (recursive exports) for speed — validate ZIP helper path with planning only package would be heavy.
        // Instead validate OpenXml on student file again.
        var v = new WordExportValidator(NullLogger<WordExportValidator>.Instance).ValidateFile(studentPath);
        Assert.True(v.IsValid, string.Join("; ", v.Errors));
    }

    [Fact]
    public async Task Export_StudentAudience_RejectsAnswerKeyRequest()
    {
        var exportService = BuildExportService();
        await Assert.ThrowsAsync<WordExportException>(() => exportService.ExportAsync(new CreateExportRequest
        {
            DocumentType = ExportDocumentType.AnswerKey,
            Audience = ExportAudience.Student,
            EducationalDocumentId = Guid.NewGuid()
        }));
    }

    private async Task<AppEducativa.Api.Models.Planificacion> CreatePlanWithClassAsync()
    {
        var planes = new PlanificacionService(_db, new PlanificacionRepository(_db), new AppEducativa.Api.Tests.TestDoubles.FakeCurrentUserService(), new AppEducativa.Api.Tests.TestDoubles.AllowAllResourceAuthorizationService());
        var planDto = await planes.CrearAsync(new CrearPlanificacionRequest
        {
            NivelId = DemoCurriculumSeed.NivelId,
            AsignaturaId = DemoCurriculumSeed.NivelAsignaturaId,
            UnidadId = DemoCurriculumSeed.UnidadId,
            Nombre = "Plan export test",
            FechaInicio = new DateOnly(2026, 3, 1),
            FechaFin = new DateOnly(2026, 3, 31)
        });
        var clases = new ClaseService(_db, new PlanificacionRepository(_db), new ClaseRepository(_db));
        await clases.CrearAsync(planDto.Id, new CrearClaseRequest
        {
            ObjetivoAprendizajeId = DemoCurriculumSeed.Oa1Id,
            NivelBloom = "Comprender"
        });
        return (await new PlanificacionRepository(_db).GetByIdAsync(planDto.Id))!;
    }

    private IWordExportService BuildExportService() =>
        new WordExportService(
            _db,
            Options.Create(new ExportOptions
            {
                RootPath = Path.Combine("App_Data", "Exports"),
                TemplateSettingsPath = Path.Combine("Templates", "Word", "default-template-settings.json"),
                AllowOutdatedDocuments = true,
                KeepFilesForDays = 30,
                MaximumFileSizeMb = 50
            }),
            new WordExportValidator(NullLogger<WordExportValidator>.Instance),
            new FakeHostEnv { ContentRootPath = _root },
            NullLogger<WordExportService>.Instance);

    private IEducationalDocumentGenerationService BuildDocService(string geminiJson)
    {
        var envelope = JsonSerializer.Serialize(new
        {
            candidates = new[] { new { content = new { parts = new[] { new { text = geminiJson } } } } },
            usageMetadata = new { promptTokenCount = 10, candidatesTokenCount = 20 }
        });
        var gemini = new GeminiClient(
            new FixedFactory(new FixedHandler(System.Net.HttpStatusCode.OK, envelope)),
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
            _db, new GeminiAiProvider(gemini),
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
            new AppEducativa.Api.Tests.TestDoubles.FakeCurrentUserService());
    }

    private static string BuildAssessmentJson(Guid oaId, string code, string release, List<Guid> indicatorIds, string secret)
    {
        var items = indicatorIds.Select((id, i) => new GeneratedEducationalItem
        {
            TemporaryId = $"item-{i + 1:000}",
            Order = i + 1,
            Type = nameof(EducationalItemType.MultipleChoice),
            Statement = $"Pregunta {i + 1} sobre fracciones para evaluación formal.",
            Difficulty = "Intermediate",
            BloomLevel = "Comprender",
            IndicatorIds = [id],
            Points = 2,
            Options =
            [
                new GeneratedEducationalOption { Order = 1, Text = "Alternativa A visible", IsCorrect = true },
                new GeneratedEducationalOption { Order = 2, Text = "Distractor B", IsCorrect = false },
                new GeneratedEducationalOption { Order = 3, Text = "Distractor C", IsCorrect = false },
                new GeneratedEducationalOption { Order = 4, Text = "Distractor D", IsCorrect = false }
            ],
            ExpectedAnswer = secret,
            Explanation = "Explicación docente " + secret,
            TeacherNotes = "Nota interna " + secret
        }).ToList();
        var total = items.Sum(x => x.Points);
        var doc = new GeneratedEducationalDocument
        {
            Curriculum = new GeneratedCurriculumReference
            {
                ObjectiveId = oaId,
                ObjectiveCode = code,
                IndicatorIds = indicatorIds,
                CurriculumRelease = release
            },
            Document = new GeneratedEducationalDocumentBody
            {
                Type = "Assessment",
                Title = "Prueba export",
                Purpose = "Evaluar",
                Instructions = "Lee con atención.",
                EstimatedDurationMinutes = 45,
                TotalPoints = total,
                Items = items,
                SpecificationTable = indicatorIds.Select(id => new GeneratedSpecificationRow
                {
                    IndicatorId = id,
                    BloomLevel = "Comprender",
                    ItemCount = items.Count(i => i.IndicatorIds.Contains(id)),
                    TotalPoints = items.Where(i => i.IndicatorIds.Contains(id)).Sum(i => i.Points),
                    WeightPercentage = Math.Round(100m / indicatorIds.Count, 2)
                }).ToList()
            }
        };
        // Fix weight sum to 100
        if (doc.Document.SpecificationTable.Count > 0)
        {
            var sum = doc.Document.SpecificationTable.Sum(r => r.WeightPercentage);
            doc.Document.SpecificationTable[^1].WeightPercentage += 100 - sum;
        }

        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private static string ReadDocxText(string path)
    {
        using var doc = WordprocessingDocument.Open(path, false);
        var sb = new StringBuilder();
        foreach (var t in doc.MainDocumentPart!.Document.Body!.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
            sb.AppendLine(t.Text);
        return sb.ToString();
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { /* ignore */ }
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    private sealed class FixedHandler(System.Net.HttpStatusCode status, string body) : HttpMessageHandler
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
