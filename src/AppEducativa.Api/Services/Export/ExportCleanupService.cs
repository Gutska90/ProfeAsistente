using AppEducativa.Api.Configuration;
using AppEducativa.Api.Data;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AppEducativa.Api.Services.Export;

public sealed class ExportCleanupService : IExportCleanupService
{
    private readonly AppEducativaDbContext _db;
    private readonly ExportOptions _options;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ExportCleanupService> _logger;

    public ExportCleanupService(
        AppEducativaDbContext db,
        IOptions<ExportOptions> options,
        IHostEnvironment env,
        ILogger<ExportCleanupService> logger)
    {
        _db = db;
        _options = options.Value;
        _env = env;
        _logger = logger;
    }

    public async Task<ExportCleanupResultDto> CleanupAsync(CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var now = DateTime.UtcNow;
        var expired = await _db.DocumentExports
            .Where(e => !e.IsDeleted
                        && e.Status == ExportStatus.Completed
                        && e.ExpiresAt != null
                        && e.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        var deletedFiles = 0;
        foreach (var exp in expired)
        {
            if (_options.DeleteExpiredFiles && !string.IsNullOrWhiteSpace(exp.RelativeFilePath))
            {
                var full = ResolvePath(exp.RelativeFilePath);
                try
                {
                    if (File.Exists(full))
                    {
                        File.Delete(full);
                        deletedFiles++;
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"No se pudo eliminar {exp.FileName}: {ex.Message}");
                }
            }

            exp.Status = ExportStatus.Expired;
            exp.IsDeleted = true;
            exp.RelativeFilePath = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ExportExpired marked={Count} filesDeleted={Files}", expired.Count, deletedFiles);
        return new ExportCleanupResultDto
        {
            ExpiredMarked = expired.Count,
            FilesDeleted = deletedFiles,
            Warnings = warnings
        };
    }

    public async Task<ExportStorageSummaryDto> GetStorageSummaryAsync(CancellationToken cancellationToken = default)
    {
        var all = await _db.DocumentExports.AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Select(e => new { e.Status, e.SizeBytes, e.RequestedAt })
            .ToListAsync(cancellationToken);

        return new ExportStorageSummaryDto
        {
            FileCount = all.Count(e => e.Status == ExportStatus.Completed),
            TotalSizeBytes = all.Where(e => e.SizeBytes is not null).Sum(e => e.SizeBytes!.Value),
            CompletedCount = all.Count(e => e.Status == ExportStatus.Completed),
            ExpiredCount = await _db.DocumentExports.CountAsync(e => e.Status == ExportStatus.Expired, cancellationToken),
            FailedCount = all.Count(e => e.Status is ExportStatus.Failed or ExportStatus.Invalid),
            OldestRequestedAt = all.OrderBy(e => e.RequestedAt).Select(e => (DateTime?)e.RequestedAt).FirstOrDefault(),
            NewestRequestedAt = all.OrderByDescending(e => e.RequestedAt).Select(e => (DateTime?)e.RequestedAt).FirstOrDefault()
        };
    }

    private string ResolvePath(string relative)
    {
        if (Path.IsPathRooted(relative)) return relative;
        return Path.Combine(_env.ContentRootPath, relative);
    }
}
