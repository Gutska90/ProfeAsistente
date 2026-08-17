using ProfeAsistente.CurriculumImporter.Abstractions;
using ProfeAsistente.CurriculumImporter.Diff;
using ProfeAsistente.CurriculumImporter.Download;
using ProfeAsistente.CurriculumImporter.Extractors;
using ProfeAsistente.CurriculumImporter.Models.Sources;
using ProfeAsistente.CurriculumImporter.Services.Extraction;
using ProfeAsistente.CurriculumImporter.Services.Normalization;
using ProfeAsistente.CurriculumImporter.Services.Parsing;
using ProfeAsistente.CurriculumImporter.Services.Storage;
using ProfeAsistente.CurriculumImporter.Services.Validation;
using ProfeAsistente.CurriculumImporter.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace ProfeAsistente.CurriculumImporter;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCurriculumImporter(this IServiceCollection services, Action<DownloaderOptions>? configure = null)
    {
        var options = new DownloaderOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddHttpClient<HttpSourceDownloader>();
        services.AddTransient<ISourceDownloader>(sp => sp.GetRequiredService<HttpSourceDownloader>());
        services.AddTransient<Services.Download.ISourceDownloader>(sp => sp.GetRequiredService<HttpSourceDownloader>());
        services.AddSingleton<ICurriculumValidator, CurriculumValidator>();
        services.AddSingleton<ICurriculumDiffService, CurriculumDiffService>();
        services.AddSingleton<ManualJsonCurriculumExtractor>();
        services.AddSingleton<PdfProgramaEstudioExtractor>();
        services.AddSingleton<PdfBaseCurricularExtractor>();
        services.AddSingleton<HtmlCurriculumExtractor>();
        services.AddSingleton<IEnumerable<Abstractions.ICurriculumExtractor>>(sp => new Abstractions.ICurriculumExtractor[]
        {
            sp.GetRequiredService<ManualJsonCurriculumExtractor>(),
            sp.GetRequiredService<PdfProgramaEstudioExtractor>(),
            sp.GetRequiredService<PdfBaseCurricularExtractor>(),
            sp.GetRequiredService<HtmlCurriculumExtractor>()
        });
        services.AddSingleton<CurriculumSourceOptions>();
        services.AddSingleton<SourceConfigurationLoader>();
        services.AddSingleton<ICurriculumTextNormalizer, CurriculumTextNormalizer>();
        services.AddSingleton<ICurriculumFileStorage>(sp =>
            new CurriculumFileStorage(Path.Combine(
                Path.GetDirectoryName(sp.GetRequiredService<DownloaderOptions>().CacheDirectory) ?? "App_Data",
                "Curriculum")));
        services.AddSingleton<Services.Extraction.ICurriculumExtractor, PdfProgramStudyExtractor>();
        services.AddSingleton<IProgramStudyParser, MathematicsFourthGradeProgramParser>();
        services.AddSingleton<ICurriculumExtractionValidator, CurriculumExtractionValidator>();
        return services;
    }
}
