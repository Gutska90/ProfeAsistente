using AppEducativa.Shared.Dtos;

namespace AppEducativa.Api.Services.Export;

public interface IExportCleanupService
{
    Task<ExportCleanupResultDto> CleanupAsync(CancellationToken cancellationToken = default);
    Task<ExportStorageSummaryDto> GetStorageSummaryAsync(CancellationToken cancellationToken = default);
}
