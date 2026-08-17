using AppEducativa.Shared.Dtos;

namespace AppEducativa.Api.Services.Export;

public interface IWordExportService
{
    Task<ExportResultDto> ExportAsync(CreateExportRequest request, CancellationToken cancellationToken = default);

    Task<ExportResultDto> ExportPlanningAsync(
        Guid planningId, CreateExportRequest request, CancellationToken cancellationToken = default);

    Task<ExportResultDto> ExportClassAsync(
        Guid classId, CreateExportRequest request, CancellationToken cancellationToken = default);

    Task<ExportResultDto> ExportEducationalDocumentAsync(
        Guid documentId, CreateExportRequest request, CancellationToken cancellationToken = default);

    Task<ExportResultDto> ExportPlanningPackageAsync(
        Guid planningId, CreateExportRequest request, CancellationToken cancellationToken = default);

    Task<ExportResultDto?> GetAsync(Guid exportId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExportSummaryDto>> ListAsync(int take = 50, CancellationToken cancellationToken = default);

    Task<(Stream Stream, string FileName, string ContentType)> OpenDownloadAsync(
        Guid exportId, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid exportId, CancellationToken cancellationToken = default);
}
