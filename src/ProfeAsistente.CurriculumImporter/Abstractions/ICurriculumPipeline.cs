using ProfeAsistente.Shared.Dtos;

namespace ProfeAsistente.CurriculumImporter.Abstractions;

public interface ISourceDownloader
{
    Task<DownloadedSource> DownloadAsync(CurriculumSourceConfig source, CancellationToken cancellationToken = default);
}

public interface ICurriculumExtractor
{
    bool CanHandle(CurriculumSourceConfig source);

    Task<CurriculumExtractionResult> ExtractAsync(
        CurriculumSourceConfig source,
        DownloadedSource downloadedSource,
        CancellationToken cancellationToken = default);
}

public interface ICurriculumValidator
{
    CurriculumValidationResult Validate(CurriculumExtractionResult extraction);
}

public interface ICurriculumImportService
{
    Task<CurriculumImportResult> ImportAsync(
        CurriculumExtractionResult extraction,
        Guid? documentId = null,
        bool autoApprove = false,
        CancellationToken cancellationToken = default);

    Task<CurriculumImportResult> ApproveBatchAsync(Guid batchId, CancellationToken cancellationToken = default);
}

public interface ICurriculumDiffService
{
    CurriculumDiffResult Compare(CurriculumExtractionResult extraction, CurriculumExtractionResult? currentPublished);
}
