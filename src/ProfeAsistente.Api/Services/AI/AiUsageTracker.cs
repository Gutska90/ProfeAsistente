using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.AI;
using ProfeAsistente.Api.Services.Authorization;
using ProfeAsistente.Api.Services.DateTimeServices;
using ProfeAsistente.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services.AI;

public interface IAiUsageTracker
{
    Task<AiUsageRecord> BeginAsync(AiUsageBeginRequest request, CancellationToken ct = default);

    Task CompleteAsync(
        AiUsageRecord usage,
        bool success,
        int? inputTokens,
        int? outputTokens,
        long latencyMs,
        string? errorCode = null,
        string? model = null,
        CancellationToken ct = default);

    Task<AiUsageSummaryDto> GetSummaryAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);

    Task<IReadOnlyList<AiUsageRecordDto>> GetRecentAsync(int take = 50, CancellationToken ct = default);
}

public sealed class AiUsageBeginRequest
{
    public string OperationType { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public string PromptId { get; init; } = string.Empty;
    public string PromptVersion { get; init; } = string.Empty;
    public string Provider { get; init; } = "Gemini";
    public string Model { get; init; } = string.Empty;
    public Guid? ClassId { get; init; }
    public Guid? DocumentId { get; init; }
    public Guid? ItemId { get; init; }
    public Guid? GenerationId { get; init; }
    public string? DocumentType { get; init; }
    public string? GenerationType { get; init; }
}

public sealed class AiUsageTracker : IAiUsageTracker
{
    private readonly ProfeAsistenteDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAiCostEstimator _cost;
    private readonly IApplicationClock _clock;

    public AiUsageTracker(
        ProfeAsistenteDbContext db,
        ICurrentUserService current,
        IAiCostEstimator cost,
        IApplicationClock clock)
    {
        _db = db;
        _current = current;
        _cost = cost;
        _clock = clock;
    }

    public async Task<AiUsageRecord> BeginAsync(AiUsageBeginRequest request, CancellationToken ct = default)
    {
        var usage = new AiUsageRecord
        {
            Id = Guid.NewGuid(),
            OperationType = request.OperationType,
            Purpose = request.Purpose,
            PromptId = request.PromptId,
            PromptVersion = request.PromptVersion,
            Provider = request.Provider,
            Model = request.Model,
            ClassId = request.ClassId,
            DocumentId = request.DocumentId,
            ItemId = request.ItemId,
            GenerationId = request.GenerationId,
            DocumentType = request.DocumentType,
            GenerationType = request.GenerationType,
            UserId = _current.UserId,
            InstitutionId = _current.ActiveInstitutionId is Guid iid && iid != Guid.Empty ? iid : null,
            StartedAt = _clock.UtcNow
        };
        _db.AiUsageRecords.Add(usage);
        await _db.SaveChangesAsync(ct);
        return usage;
    }

    public async Task CompleteAsync(
        AiUsageRecord usage,
        bool success,
        int? inputTokens,
        int? outputTokens,
        long latencyMs,
        string? errorCode = null,
        string? model = null,
        CancellationToken ct = default)
    {
        usage.Success = success;
        usage.InputTokens = inputTokens;
        usage.OutputTokens = outputTokens;
        usage.LatencyMilliseconds = latencyMs;
        usage.EstimatedCostUsd = _cost.EstimateUsd(model ?? usage.Model, inputTokens, outputTokens);
        usage.CompletedAt = _clock.UtcNow;
        usage.ErrorCode = errorCode;
        if (!string.IsNullOrWhiteSpace(model))
            usage.Model = model;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AiUsageSummaryDto> GetSummaryAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var from = fromUtc ?? _clock.UtcNow.AddDays(-30);
        var to = toUtc ?? _clock.UtcNow.AddDays(1);

        var q = ApplyTenantScope(_db.AiUsageRecords.AsNoTracking()
            .Where(r => r.StartedAt >= from && r.StartedAt < to));

        var rows = await q.ToListAsync(ct);
        var byPurpose = rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Purpose) ? r.OperationType : r.Purpose)
            .Select(g => new AiUsagePurposeBreakdownDto
            {
                Purpose = g.Key,
                Count = g.Count(),
                SuccessCount = g.Count(x => x.Success),
                InputTokens = g.Sum(x => x.InputTokens ?? 0),
                OutputTokens = g.Sum(x => x.OutputTokens ?? 0),
                EstimatedCostUsd = g.Sum(x => x.EstimatedCostUsd ?? 0),
                AvgLatencyMs = g.Count() == 0 ? 0 : (long)g.Average(x => x.LatencyMilliseconds)
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return new AiUsageSummaryDto
        {
            FromUtc = from,
            ToUtc = to,
            TotalGenerations = rows.Count,
            SuccessfulGenerations = rows.Count(r => r.Success),
            FailedGenerations = rows.Count(r => !r.Success),
            TotalInputTokens = rows.Sum(r => r.InputTokens ?? 0),
            TotalOutputTokens = rows.Sum(r => r.OutputTokens ?? 0),
            EstimatedCostUsd = rows.Sum(r => r.EstimatedCostUsd ?? 0),
            AvgLatencyMs = rows.Count == 0 ? 0 : (long)rows.Average(r => r.LatencyMilliseconds),
            ByPurpose = byPurpose
        };
    }

    public async Task<IReadOnlyList<AiUsageRecordDto>> GetRecentAsync(int take = 50, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var q = ApplyTenantScope(_db.AiUsageRecords.AsNoTracking());

        return await q.OrderByDescending(r => r.StartedAt)
            .Take(take)
            .Select(r => new AiUsageRecordDto
            {
                Id = r.Id,
                Purpose = r.Purpose,
                OperationType = r.OperationType,
                PromptId = r.PromptId,
                PromptVersion = r.PromptVersion,
                Provider = r.Provider,
                Model = r.Model,
                InputTokens = r.InputTokens,
                OutputTokens = r.OutputTokens,
                LatencyMilliseconds = r.LatencyMilliseconds,
                EstimatedCostUsd = r.EstimatedCostUsd,
                Success = r.Success,
                ErrorCode = r.ErrorCode,
                ClassId = r.ClassId,
                DocumentId = r.DocumentId,
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt
            })
            .ToListAsync(ct);
    }

    /// <summary>Aislamiento estricto: sin InstitutionId/UserId nulos mezclados en vistas tenant.</summary>
    private IQueryable<AiUsageRecord> ApplyTenantScope(IQueryable<AiUsageRecord> q)
    {
        if (IsSystemAdmin())
            return q;

        if (_current.ActiveInstitutionId is Guid inst && inst != Guid.Empty)
            q = q.Where(r => r.InstitutionId == inst);
        else
            q = q.Where(r => false); // sin institución activa no hay telemetría visible

        if (_current.UserId is Guid uid && !IsSchoolOrCurriculumAdmin())
            q = q.Where(r => r.UserId == uid);

        return q;
    }

    private bool IsSystemAdmin() =>
        _current.IsInRole(nameof(Shared.Enums.ApplicationRole.SystemAdministrator));

    private bool IsSchoolOrCurriculumAdmin() =>
        _current.IsInRole(nameof(Shared.Enums.ApplicationRole.SchoolAdministrator))
        || _current.IsInRole(nameof(Shared.Enums.ApplicationRole.CurriculumAdministrator));
}
