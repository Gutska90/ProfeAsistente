using System.Text.Json.Serialization;
using AppEducativa.Api.Configuration;
using AppEducativa.Api.Data;
using AppEducativa.Api.Middleware;
using AppEducativa.Api.Repositories;
using AppEducativa.Api.Services;
using AppEducativa.Api.Services.AI;
using AppEducativa.Api.Services.AI.ClassGeneration;
using AppEducativa.Api.Services.AI.DocumentGeneration;
using AppEducativa.Api.Services.AI.Gemini;
using AppEducativa.Api.Services.Curriculum;
using AppEducativa.Api.Services.Coverage;
using AppEducativa.Api.Services.DateTimeServices;
using AppEducativa.Api.Services.Export;
using AppEducativa.Api.Services.PlanningCalendar;
using AppEducativa.Api.Services.PlanningSequence;
using AppEducativa.Api.Services.PlanningSuggestions;
using AppEducativa.Api.Security;
using AppEducativa.Api.Services.Auth;
using AppEducativa.CurriculumImporter;
using AppEducativa.CurriculumImporter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api;

/// <summary>
/// Construye el host de la API para uso standalone o embebido (desde MAUI).
/// </summary>
public static class ApiHostBuilder
{
    public const string DefaultUrl = "http://127.0.0.1:5180";

    public static WebApplication Build(string[]? args = null, string? urls = null, string? contentRoot = null)
    {
        var builder = string.IsNullOrWhiteSpace(contentRoot)
            ? WebApplication.CreateBuilder(args ?? [])
            : WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args ?? [],
                ContentRootPath = contentRoot
            });

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(ApiHostBuilder).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
        builder.Services.AddEndpointsApiExplorer();
        builder.AddAppEducativaSecurity();

        var root = contentRoot ?? builder.Environment.ContentRootPath;
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                               ?? "Data Source=appeducativa.db";
        // Resolver ruta relativa al content root
        if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            var file = connectionString["Data Source=".Length..].Trim();
            if (!Path.IsPathRooted(file))
                connectionString = $"Data Source={Path.Combine(root, file)}";
        }

        builder.Services.AddDbContext<AppEducativaDbContext>(options =>
            options.UseSqlite(connectionString));

        builder.Services.AddScoped<IDocumentoRepository, DocumentoRepository>();
        builder.Services.AddScoped<IPlanificacionRepository, PlanificacionRepository>();
        builder.Services.AddScoped<IClaseRepository, ClaseRepository>();
        builder.Services.AddScoped<ICurriculumRepository, CurriculumRepository>();
        builder.Services.AddScoped<IPlanificacionService, PlanificacionService>();
        builder.Services.AddScoped<IClaseService, ClaseService>();
        builder.Services.AddScoped<IExportService, ExportService>();
        builder.Services.AddCurriculumImporter(opts =>
        {
            var curriculumRoot = builder.Configuration["Curriculum:StorageRoot"] ?? "App_Data/Curriculum";
            opts.CacheDirectory = Path.IsPathRooted(curriculumRoot) ? curriculumRoot : Path.Combine(root, curriculumRoot);
            opts.MaxDownloadSizeBytes = builder.Configuration.GetValue<long?>("Curriculum:MaxDownloadBytes") ?? 26_214_400;
        });
        builder.Services.AddScoped<ICurriculumImportService, EfCurriculumImportService>();
        builder.Services.AddScoped<OfficialCurriculumImportOrchestrator>();
        builder.Services.AddScoped<ICurriculumReviewService, CurriculumReviewService>();

        builder.Services.AddOptions<GeminiOptions>()
            .Bind(builder.Configuration.GetSection(GeminiOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(o =>
            {
                if (string.IsNullOrWhiteSpace(o.Model)) return false;
                if (!Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out var uri)) return false;
                if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;
                if (o.TimeoutSeconds is < 5 or > 300) return false;
                if (o.Temperature is < 0 or > 1) return false;
                if (o.MaxRetries is < 0 or > 5) return false;
                if (o.MaxOutputTokens <= 0) return false;
                return true;
            }, "GeminiOptions inválidas.")
            .ValidateOnStart();

        builder.Services.AddOptions<AiUsageOptions>()
            .Bind(builder.Configuration.GetSection(AiUsageOptions.SectionName))
            .Validate(o => o.MaximumGenerationsPerClassPerDay > 0
                           && o.MaximumConcurrentGenerations > 0
                           && o.MaximumDocumentGenerationsPerClassPerDay > 0
                           && o.MaximumItemRegenerationsPerDocumentPerDay > 0,
                "AiUsageOptions inválidas.")
            .ValidateOnStart();

        builder.Services.AddHttpClient(nameof(GeminiClient), client =>
        {
            client.Timeout = TimeSpan.FromSeconds(90);
        });
        builder.Services.AddScoped<IGeminiClient, GeminiClient>();
        builder.Services.AddScoped<IAiProvider, GeminiAiProvider>();
        builder.Services.AddScoped<ClassGenerationContextBuilder>();
        builder.Services.AddScoped<ClassGenerationValidator>();
        builder.Services.AddScoped<IClassStructureGenerationService, ClassStructureGenerationService>();
        builder.Services.AddScoped<IEducationalItemSimilarityService, EducationalItemSimilarityService>();
        builder.Services.AddScoped<IEducationalDocumentGenerationValidator, EducationalDocumentGenerationValidator>();
        builder.Services.AddScoped<EducationalDocumentContextBuilder>();
        builder.Services.AddScoped<IEducationalDocumentGenerationService, EducationalDocumentGenerationService>();

        builder.Services.AddOptions<ExportOptions>()
            .Bind(builder.Configuration.GetSection(ExportOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.RootPath)
                           && o.KeepFilesForDays > 0
                           && o.MaximumFileSizeMb > 0
                           && o.SynchronousTimeoutSeconds >= 10,
                "ExportOptions inválidas.")
            .ValidateOnStart();
        builder.Services.AddScoped<IWordExportValidator, WordExportValidator>();
        builder.Services.AddScoped<IWordExportService, WordExportService>();
        builder.Services.AddScoped<IExportCleanupService, ExportCleanupService>();

        builder.Services.AddSingleton<IApplicationClock, SystemApplicationClock>();
        builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();
        builder.Services.AddSingleton<PlanningCalendarGenerator>();
        builder.Services.AddSingleton<PlanningCalendarValidator>();
        builder.Services.AddScoped<IPlanningCalendarService, PlanningCalendarService>();
        builder.Services.AddSingleton<BloomProgressionService>();
        builder.Services.AddSingleton<PlanningSequenceGenerator>();
        builder.Services.AddSingleton<PlanningSequenceValidator>();
        builder.Services.AddSingleton<CurriculumCoverageCalculator>();
        builder.Services.AddSingleton<CurriculumCoverageValidator>();
        builder.Services.AddScoped<ICurriculumCoverageService, CurriculumCoverageService>();
        builder.Services.AddScoped<IPlanningSequenceService, PlanningSequenceService>();
        builder.Services.AddScoped<IPlanningSuggestionService, PlanningSuggestionService>();

        builder.Services.AddHttpClient<IGeminiService, GeminiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(90);
        });

        var corsSection = builder.Configuration.GetSection(CorsOptions.SectionName);
        builder.Services.Configure<CorsOptions>(corsSection);
        var corsOpts = corsSection.Get<CorsOptions>() ?? new CorsOptions();
        var isDev = builder.Environment.IsDevelopment();
        var allowAny = isDev && !corsOpts.RestrictInDevelopment;
        var origins = corsOpts.AllowedOrigins
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("ProfeAsistente", policy =>
            {
                if (allowAny)
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    return;
                }

                if (origins.Length == 0)
                {
                    // Producción sin orígenes: no abrir el navegador a cualquier sitio.
                    policy.SetIsOriginAllowed(_ => false);
                    return;
                }

                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        if (!string.IsNullOrWhiteSpace(urls))
            builder.WebHost.UseUrls(urls);
        else if (string.IsNullOrWhiteSpace(builder.Configuration["Urls"]))
            builder.WebHost.UseUrls(DefaultUrl);

        var app = builder.Build();

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        DatabaseInitializer.InitializeAsync(app.Services).GetAwaiter().GetResult();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
            ctx.Response.Headers.TryAdd("X-Frame-Options", "DENY");
            ctx.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
            await next();
        });

        app.UseCors("ProfeAsistente");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow })).AllowAnonymous();
        app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow })).AllowAnonymous();

        return app;
    }
}
