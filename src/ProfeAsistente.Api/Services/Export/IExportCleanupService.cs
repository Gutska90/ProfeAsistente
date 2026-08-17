using ProfeAsistente.Shared.Dtos;

namespace ProfeAsistente.Api.Services.Export;

public interface IExportCleanupService
{
    Task<ExportCleanupResultDto> CleanupAsync(CancellationToken cancellationToken = default);
    Task<ExportStorageSummaryDto> GetStorageSummaryAsync(CancellationToken cancellationToken = default);
}
