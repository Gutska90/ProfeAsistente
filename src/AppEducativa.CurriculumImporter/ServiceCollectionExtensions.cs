using AppEducativa.CurriculumImporter.Abstractions;
using AppEducativa.CurriculumImporter.Diff;
using AppEducativa.CurriculumImporter.Download;
using AppEducativa.CurriculumImporter.Extractors;
using AppEducativa.CurriculumImporter.Models.Sources;
using AppEducativa.CurriculumImporter.Services.Extraction;
using AppEducativa.CurriculumImporter.Services.Normalization;
using AppEducativa.CurriculumImporter.Services.Parsing;
using AppEducativa.CurriculumImporter.Services.Storage;
using AppEducativa.CurriculumImporter.Services.Validation;
using AppEducativa.CurriculumImporter.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace AppEducativa.CurriculumImporter;

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
