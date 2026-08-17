using AngleSharp;
using ProfeAsistente.CurriculumImporter.Abstractions;
using ProfeAsistente.Shared.Dtos;

namespace ProfeAsistente.CurriculumImporter.Extractors;

public class HtmlCurriculumExtractor : ICurriculumExtractor
{
    public bool CanHandle(CurriculumSourceConfig source) =>
        source.Formato.Equals("Html", StringComparison.OrdinalIgnoreCase);

    public async Task<CurriculumExtractionResult> ExtractAsync(
        CurriculumSourceConfig source,
        DownloadedSource downloadedSource,
        CancellationToken cancellationToken = default)
    {
        var result = new CurriculumExtractionResult
        {
            SourceTitle = source.Nombre,
            SourceUrl = source.Url,
            DocumentType = source.Tipo,
            ConfianzaExtraccion = 0.25
        };

        var html = System.Text.Encoding.UTF8.GetString(downloadedSource.Content);
        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html), cancellationToken);
        var text = document.Body?.TextContent?.Trim() ?? string.Empty;
        result.ExtractedText = text.Length > 100_000 ? text[..100_000] : text;

        result.Advertencias.Add(
            "Extractor HTML: texto obtenido sin estructurar OA/indicadores. " +
            "Complete vía JSON manual. No se inventaron códigos curriculares.");

        if (string.IsNullOrWhiteSpace(text))
            result.Errores.Add("HTML sin texto útil.");

        return result;
    }
}
