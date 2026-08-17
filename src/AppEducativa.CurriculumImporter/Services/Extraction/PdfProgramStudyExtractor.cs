using AppEducativa.CurriculumImporter.Models.Download;
using AppEducativa.CurriculumImporter.Models.Extraction;
using AppEducativa.CurriculumImporter.Models.Sources;
using AppEducativa.CurriculumImporter.Services.Normalization;
using AppEducativa.CurriculumImporter.Services.Storage;
using UglyToad.PdfPig;

namespace AppEducativa.CurriculumImporter.Services.Extraction;

public interface ICurriculumExtractor
{
    bool CanHandle(CurriculumSourceDefinition definition, DownloadedSource downloaded);
    Task<CurriculumExtractionResult> ExtractAsync(
        CurriculumSourceDefinition definition, DownloadedSource downloaded, CancellationToken cancellationToken = default);
}

public sealed class PdfProgramStudyExtractor : ICurriculumExtractor
{
    private readonly ICurriculumTextNormalizer _normalizer;
    private readonly ICurriculumFileStorage _storage;

    public PdfProgramStudyExtractor(ICurriculumTextNormalizer normalizer, ICurriculumFileStorage storage)
    {
        _normalizer = normalizer;
        _storage = storage;
    }

    public bool CanHandle(CurriculumSourceDefinition definition, DownloadedSource downloaded) =>
        string.Equals(definition.Formato, "Pdf", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(definition.TipoFuente, "ProgramaEstudio", StringComparison.OrdinalIgnoreCase);

    public async Task<CurriculumExtractionResult> ExtractAsync(
        CurriculumSourceDefinition definition, DownloadedSource downloaded, CancellationToken cancellationToken = default)
    {
        var result = new CurriculumExtractionResult { SourceId = definition.Id };
        try
        {
            using var document = PdfDocument.Open(downloaded.LocalFilePath);
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var original = page.Text ?? string.Empty;
                var normalized = _normalizer.Normalize(original);
                result.Pages.Add(new ExtractedPage(page.Number, original, normalized));
                if (string.IsNullOrWhiteSpace(normalized))
                    result.Warnings.Add(new ExtractionWarning("empty-page", "Página sin texto extraíble.", page.Number));
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            result.Warnings.Add(new ExtractionWarning("pdf-read-failed", $"No se pudo leer el PDF: {ex.Message}"));
            result.RequiresManualReview = true;
            return result;
        }

        var text = string.Join(Environment.NewLine, result.Pages.Select(p => $"[Página {p.PageNumber}]{Environment.NewLine}{p.OriginalText}"));
        result.ExtractedTextPath = await _storage.SaveTextAsync($"{definition.Id}.txt", text, cancellationToken);
        if (result.Pages.All(p => string.IsNullOrWhiteSpace(p.NormalizedText)))
        {
            result.RequiresManualReview = true;
            result.Warnings.Add(new ExtractionWarning("no-text", "El PDF no contiene texto extraíble; requiere revisión manual."));
        }
        return result;
    }
}
