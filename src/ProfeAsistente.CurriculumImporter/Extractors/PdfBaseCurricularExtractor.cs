using ProfeAsistente.CurriculumImporter.Abstractions;
using ProfeAsistente.Shared.Dtos;

namespace ProfeAsistente.CurriculumImporter.Extractors;

/// <summary>Reutiliza la lógica de PdfProgramaEstudioExtractor para bases curriculares.</summary>
public class PdfBaseCurricularExtractor : ICurriculumExtractor
{
    private readonly PdfProgramaEstudioExtractor _inner = new();

    public bool CanHandle(CurriculumSourceConfig source) =>
        source.Formato.Equals("Pdf", StringComparison.OrdinalIgnoreCase) &&
        source.Tipo.Equals("BaseCurricular", StringComparison.OrdinalIgnoreCase);

    public Task<CurriculumExtractionResult> ExtractAsync(
        CurriculumSourceConfig source,
        DownloadedSource downloadedSource,
        CancellationToken cancellationToken = default) =>
        _inner.ExtractAsync(source, downloadedSource, cancellationToken);
}
