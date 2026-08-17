using System.Net;
using System.Text;
using System.Text.Json;
using AppEducativa.Api.Configuration;
using AppEducativa.Api.Data;
using AppEducativa.Api.Models.AI;
using AppEducativa.Api.Models.AI.Responses;
using AppEducativa.Api.Repositories;
using AppEducativa.Api.Services;
using AppEducativa.Api.Services.AI.ClassGeneration;
using AppEducativa.Api.Services.AI.Gemini;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AppEducativa.Api.Tests;

public class ClassStructureGenerationTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"appedu-ai-{Guid.NewGuid():N}.db");
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"appedu-ai-root-{Guid.NewGuid():N}");
    private readonly AppEducativaDbContext _db;

    public ClassStructureGenerationTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Prompts"));
        var promptSrc = Path.Combine(AppContext.BaseDirectory, "Prompts", "class-structure-system-prompt.txt");
        if (!File.Exists(promptSrc))
        {
            // Fallback: copy from source tree relative to test assembly
            promptSrc = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", "src", "AppEducativa.Api", "Prompts", "class-structure-system-prompt.txt"));
        }
        File.Copy(promptSrc, Path.Combine(_root, "Prompts", "class-structure-system-prompt.txt"), overwrite: true);

        var options = new DbContextOptionsBuilder<AppEducativaDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _db = new AppEducativaDbContext(options);
        _db.Database.Migrate();
        DemoCurriculumSeed.Seed(_db);
        _db.SaveChanges();
    }

    [Fact]
    public void Validator_AcceptsValidStructure()
    {
        var context = SampleContext();
        var structure = SampleStructure(context);
        var result = new ClassGenerationValidator().Validate(structure, context);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Validator_RejectsWrongObjectiveCode()
    {
        var context = SampleContext();
        var structure = SampleStructure(context);
        structure.Curriculum.ObjectiveCode = "OA FAKE";
        var result = new ClassGenerationValidator().Validate(structure, context);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_RejectsLargeDurationMismatch()
    {
        var context = SampleContext();
        var structure = SampleStructure(context, start: 20, development: 70, closure: 20, total: 90);
        var result = new ClassGenerationValidator().Validate(structure, context);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GeminiClient_ParsesSuccessfulJsonResponse()
    {
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", "test-key-not-real");
        var handler = new FixedHandler(HttpStatusCode.OK, """
            {
              "candidates": [ { "content": { "parts": [ { "text": "{\"ok\":true}" } ] } } ],
              "usageMetadata": { "promptTokenCount": 10, "candidatesTokenCount": 5 }
            }
            """);
        var client = new GeminiClient(
            new FixedFactory(handler),
            Options.Create(ValidGeminiOptions()),
            NullLogger<GeminiClient>.Instance);

        var result = await client.GenerateJsonAsync("sys", "user", null, CancellationToken.None);
        Assert.Contains("ok", result.Text);
        Assert.Equal(10, result.InputTokenCount);
    }

    [Fact]
    public async Task GeminiClient_ThrowsOnMissingApiKey()
    {
        var client = new GeminiClient(
            new FixedFactory(new FixedHandler(HttpStatusCode.OK, "{}")),
            Options.Create(new GeminiOptions
            {
                ApiKeyEnvironmentVariable = "GEMINI_API_KEY_MISSING_FOR_TEST_" + Guid.NewGuid().ToString("N"),
                Model = "gemini-2.5-flash",
                BaseUrl = "https://generativelanguage.googleapis.com",
                EnableGeneration = true,
                MaxRetries = 0
            }),
            NullLogger<GeminiClient>.Instance);

        await Assert.ThrowsAsync<GeminiConfigurationException>(() =>
            client.GenerateJsonAsync("s", "u", null, CancellationToken.None));
    }

    [Fact]
    public async Task FullGenerationFlow_WithFakeGemini_SavesAndMarksOutdatedOnOaChange()
    {
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", "test-key-not-real");
        var clase = await CreateClassAsync();
        var oa = await _db.ObjetivosAprendizaje.FirstAsync(o => o.Id == clase.ObjetivoAprendizajeId);
        var indicatorIds = await _db.IndicadoresEvaluacion
            .Where(i => i.ObjetivoAprendizajeId == oa.Id)
            .Select(i => i.Id)
            .Take(2)
            .ToListAsync();

        var release = oa.Version; // demo without CurriculumRelease
        var service = BuildService(BuildGeminiJson(oa.Id, oa.Codigo, indicatorIds, release));

        var result = await service.GenerateAsync(clase.Id, new GenerateClassStructureRequest
        {
            DurationMinutes = 90,
            EvaluationIndicatorIds = indicatorIds,
            PreviousKnowledge = "Fracciones simples",
            AvailableResources = "Pizarra y material concreto",
            IncludeFormativeAssessment = true,
            IncludeDifferentiation = true
        });

        Assert.Equal(nameof(AiGenerationStatus.Completed), result.Status);
        Assert.NotNull(result.Structure);
        Assert.True(result.IsCurrentVersion);

        var updateReq = new UpdateClassStructureContentRequest
        {
            Title = result.Structure!.Title,
            Purpose = "Propósito editado por el docente con suficiente longitud.",
            Start = result.Structure.Start,
            Development = result.Structure.Development,
            Closure = result.Structure.Closure,
            ChangeSummary = "Ajuste de propósito"
        };
        updateReq.Development.Objective = "Desarrollo editado con actividades concretas para el OA.";
        var afterEdit = await service.UpdateContentAsync(result.GenerationId, updateReq);
        Assert.Contains("editado", afterEdit.Structure!.Purpose, StringComparison.OrdinalIgnoreCase);

        var otherOa = await _db.ObjetivosAprendizaje.FirstAsync(o => o.Id != oa.Id);
        clase.ObjetivoAprendizajeId = otherOa.Id;
        await _db.SaveChangesAsync();
        await service.MarkOutdatedIfConfigurationChangedAsync(clase.Id);

        var current = await service.GetCurrentAsync(clase.Id);
        Assert.NotNull(current);
        Assert.True(current!.IsOutdated);
    }

    private ClassStructureGenerationService BuildService(string geminiJsonText)
    {
        var envelope = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new { content = new { parts = new[] { new { text = geminiJsonText } } } }
            },
            usageMetadata = new { promptTokenCount = 100, candidatesTokenCount = 200 }
        });

        var gemini = new GeminiClient(
            new FixedFactory(new FixedHandler(HttpStatusCode.OK, envelope)),
            Options.Create(ValidGeminiOptions()),
            NullLogger<GeminiClient>.Instance);

        return new ClassStructureGenerationService(
            _db,
            gemini,
            new ClassGenerationContextBuilder(_db, NullLogger<ClassGenerationContextBuilder>.Instance),
            new ClassGenerationValidator(),
            Options.Create(ValidGeminiOptions()),
            Options.Create(new AiUsageOptions()),
            new FakeHostEnv { ContentRootPath = _root },
            NullLogger<ClassStructureGenerationService>.Instance);
    }

    private async Task<AppEducativa.Api.Models.Clase> CreateClassAsync()
    {
        var planes = new PlanificacionService(_db, new PlanificacionRepository(_db), new AppEducativa.Api.Tests.TestDoubles.FakeCurrentUserService(), new AppEducativa.Api.Tests.TestDoubles.AllowAllResourceAuthorizationService());
        var planDto = await planes.CrearAsync(new CrearPlanificacionRequest
        {
            NivelId = DemoCurriculumSeed.NivelId,
            AsignaturaId = DemoCurriculumSeed.NivelAsignaturaId,
            UnidadId = DemoCurriculumSeed.UnidadId,
            Nombre = "Plan IA test",
            FechaInicio = new DateOnly(2026, 3, 1),
            FechaFin = new DateOnly(2026, 3, 31)
        });
        var clases = new ClaseService(_db, new PlanificacionRepository(_db), new ClaseRepository(_db));
        var claseDto = await clases.CrearAsync(planDto.Id, new CrearClaseRequest
        {
            ObjetivoAprendizajeId = DemoCurriculumSeed.Oa1Id,
            NivelBloom = "Comprender"
        });
        var clase = await _db.Clases.FirstAsync(c => c.Id == claseDto.Id);
        var inds = await _db.IndicadoresEvaluacion.Where(i => i.ObjetivoAprendizajeId == DemoCurriculumSeed.Oa1Id).Take(2).ToListAsync();
        foreach (var ind in inds)
        {
            if (!await _db.ClaseIndicadores.AnyAsync(x => x.ClaseId == clase.Id && x.IndicadorEvaluacionId == ind.Id))
                _db.ClaseIndicadores.Add(new AppEducativa.Api.Models.ClaseIndicadorEvaluacion
                {
                    ClaseId = clase.Id,
                    IndicadorEvaluacionId = ind.Id
                });
        }
        await _db.SaveChangesAsync();
        return clase;
    }

    private static GeminiOptions ValidGeminiOptions() => new()
    {
        ApiKeyEnvironmentVariable = "GEMINI_API_KEY",
        Model = "gemini-2.5-flash",
        BaseUrl = "https://generativelanguage.googleapis.com",
        EnableGeneration = true,
        PersistRequestPayloads = false,
        MaxRetries = 0,
        PromptVersion = "class-structure-v1"
    };

    private static ClassGenerationContext SampleContext()
    {
        var oaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var ind1 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        return new ClassGenerationContext
        {
            ClassId = Guid.NewGuid(),
            CurriculumRelease = "2026.08",
            Level = "4° básico",
            Subject = "Matemática",
            Unit = "Fracciones",
            Objective = new CurriculumObjectiveRef
            {
                Id = oaId,
                Code = "DEMO OA 01",
                Description = "Representar fracciones propias con material concreto."
            },
            Indicators =
            [
                new CurriculumIndicatorRef { Id = ind1, Description = "Identifican fracciones en figuras." }
            ],
            BloomLevel = "Comprender",
            DurationMinutes = 90
        };
    }

    private static GeneratedClassStructure SampleStructure(
        ClassGenerationContext context,
        int start = 15,
        int development = 60,
        int closure = 15,
        int total = 90) => new()
    {
        RequiresReview = false,
        Curriculum = new GeneratedCurriculumReference
        {
            ObjectiveId = context.Objective.Id,
            ObjectiveCode = context.Objective.Code,
            IndicatorIds = context.Indicators.Select(i => i.Id).ToList(),
            CurriculumRelease = context.CurriculumRelease
        },
        Class = new GeneratedClassBody
        {
            Title = "Clase de fracciones",
            Purpose = "Que los estudiantes representen fracciones propias.",
            TotalDurationMinutes = total,
            Start = Phase(start, "Activar conocimientos previos"),
            Development = Phase(development, "Trabajar representaciones"),
            Closure = Phase(closure, "Sintetizar aprendizajes"),
            FormativeAssessment = new GeneratedFormativeAssessment
            {
                Included = true,
                Strategy = "Preguntas orales",
                Evidence = "Respuestas de los estudiantes",
                FeedbackMethod = "Retroalimentación verbal"
            },
            Differentiation = new GeneratedDifferentiation
            {
                Included = true,
                SupportActions = ["Apoyo con material concreto"],
                ExtensionActions = ["Problemas de mayor complejidad"],
                AccessibilityConsiderations = ["Instrucciones orales y escritas"]
            }
        }
    };

    private static GeneratedClassPhase Phase(int minutes, string objective) => new()
    {
        DurationMinutes = minutes,
        Objective = objective,
        TeacherActions = ["Guía la actividad"],
        StudentActions = ["Participan activamente"],
        Activities = [new GeneratedActivity { Name = "Actividad", Description = "Desarrollo concreto de la actividad en aula." }],
        Resources = ["Pizarra"],
        Evidence = ["Participación y productos"]
    };

    private static string BuildGeminiJson(Guid oaId, string code, List<Guid> indicatorIds, string release) =>
        JsonSerializer.Serialize(new GeneratedClassStructure
        {
            RequiresReview = false,
            Warnings = [],
            Curriculum = new GeneratedCurriculumReference
            {
                ObjectiveId = oaId,
                ObjectiveCode = code,
                IndicatorIds = indicatorIds,
                CurriculumRelease = release
            },
            Class = new GeneratedClassBody
            {
                Title = "Estructura generada de prueba",
                Purpose = "Propósito pedagógico coherente con el OA seleccionado.",
                TotalDurationMinutes = 90,
                Start = Phase(15, "Inicio motivador"),
                Development = Phase(60, "Desarrollo de la actividad principal"),
                Closure = Phase(15, "Cierre y síntesis formativa"),
                FormativeAssessment = new GeneratedFormativeAssessment
                {
                    Included = true,
                    Strategy = "Ticket de salida",
                    Evidence = "Respuestas escritas",
                    FeedbackMethod = "Comentario oral"
                },
                Differentiation = new GeneratedDifferentiation
                {
                    Included = true,
                    SupportActions = ["Andamiaje"],
                    ExtensionActions = ["Desafío extra"],
                    AccessibilityConsiderations = ["Lenguaje claro"]
                }
            }
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    /// <summary>
    /// Prueba manual opcional. Solo corre con RUN_GEMINI_INTEGRATION_TESTS=true y GEMINI_API_KEY.
    /// No se ejecuta en CI por defecto.
    /// </summary>
    [Fact]
    public async Task OptionalLiveGemini_Smoke_WhenEnabled()
    {
        var run = string.Equals(
            Environment.GetEnvironmentVariable("RUN_GEMINI_INTEGRATION_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        var key = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!run || string.IsNullOrWhiteSpace(key))
            return; // omitida a propósito (sin falla)

        var services = new ServiceCollection();
        services.AddHttpClient(nameof(GeminiClient));
        var sp = services.BuildServiceProvider();
        var client = new GeminiClient(
            sp.GetRequiredService<IHttpClientFactory>(),
            Options.Create(ValidGeminiOptions()),
            NullLogger<GeminiClient>.Instance);

        var result = await client.GenerateJsonAsync(
            "Responde solo JSON válido.",
            """{"task":"reply","schema":{"ok":true}} Responde {"ok":true}""",
            null,
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Text));
        Assert.DoesNotContain("GEMINI_API_KEY", result.Text, StringComparison.OrdinalIgnoreCase);
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
