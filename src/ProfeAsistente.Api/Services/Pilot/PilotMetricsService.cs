using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.Pilot;
using ProfeAsistente.Api.Services.Authorization;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services.Pilot;

public interface IPilotMetricsService
{
    Task<PilotMetricsDto> GetMetricsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
    Task<PilotSessionReportDto> SubmitSessionReportAsync(SubmitPilotSessionReportRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PilotSessionReportDto>> ListSessionReportsAsync(int take = 50, CancellationToken ct = default);
}

public sealed class PilotMetricsService : IPilotMetricsService
{
    private readonly ProfeAsistenteDbContext _db;
    private readonly ICurrentUserService _current;

    public PilotMetricsService(ProfeAsistenteDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<PilotMetricsDto> GetMetricsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var from = fromUtc ?? DateTime.UtcNow.AddDays(-30);
        var to = toUtc ?? DateTime.UtcNow.AddDays(1);
        var classIds = await ResolveScopedClassIdsAsync(ct);

        var docs = await _db.EducationalDocuments.AsNoTracking()
            .Where(d => !d.IsDeleted && classIds.Contains(d.ClassId) && d.CreatedAt >= from && d.CreatedAt < to)
            .Select(d => new { d.Id, d.ClassId, d.IsCurrentVersion, d.SourceDocumentId })
            .ToListAsync(ct);

        var docIds = docs.Select(d => d.Id).ToList();
        var exportedDocIds = await _db.DocumentExports.AsNoTracking()
            .Where(e => !e.IsDeleted
                        && e.EducationalDocumentId != null
                        && docIds.Contains(e.EducationalDocumentId.Value)
                        && e.Status == ExportStatus.Completed)
            .Select(e => e.EducationalDocumentId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var feedback = await _db.EducationalDocumentFeedbacks.AsNoTracking()
            .Where(f => docIds.Contains(f.EducationalDocumentId) && f.CreatedAt >= from && f.CreatedAt < to)
            .Select(f => f.Useful)
            .ToListAsync(ct);

        var classIdsWithMaterial = docs.Select(d => d.ClassId).Distinct().ToList();
        var classesWithEvidence = await _db.LearningAssessments.AsNoTracking()
            .Where(a => a.ClassId != null
                        && classIdsWithMaterial.Contains(a.ClassId.Value)
                        && a.CreatedAt >= from && a.CreatedAt < to)
            .Select(a => a.ClassId!.Value)
            .Distinct()
            .CountAsync(ct);

        var aiQ = _db.AiUsageRecords.AsNoTracking()
            .Where(r => r.StartedAt >= from && r.StartedAt < to);
        if (_current.UserId is Guid uid && !IsPrivileged())
            aiQ = aiQ.Where(r => r.UserId == null || r.UserId == uid);
        if (_current.ActiveInstitutionId is Guid inst && inst != Guid.Empty)
            aiQ = aiQ.Where(r => r.InstitutionId == null || r.InstitutionId == inst);

        var aiRows = await aiQ.Select(r => new { r.EstimatedCostUsd, r.LatencyMilliseconds }).ToListAsync(ct);

        var reportsQ = _db.PilotSessionReports.AsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt < to);
        if (_current.UserId is Guid uid2 && !IsPrivileged())
            reportsQ = reportsQ.Where(r => r.UserId == uid2);
        var reports = await reportsQ.Select(r => r.MinutesSavedEstimate).ToListAsync(ct);

        var created = docs.Count;
        var exported = exportedDocIds.Count;
        var exportRate = created == 0 ? 0 : Math.Round(100.0 * exported / created, 1);
        var useful = feedback.Count(f => f);
        var feedbackUsefulPct = feedback.Count == 0 ? 0 : Math.Round(100.0 * useful / feedback.Count, 1);
        var evidencePct = classIdsWithMaterial.Count == 0
            ? 0
            : Math.Round(100.0 * classesWithEvidence / classIdsWithMaterial.Count, 1);

        var dto = new PilotMetricsDto
        {
            FromUtc = from,
            ToUtc = to,
            MaterialsCreated = created,
            MaterialsCurrent = docs.Count(d => d.IsCurrentVersion),
            MaterialsExported = exported,
            ExportRatePercent = exportRate,
            FeedbackCount = feedback.Count,
            FeedbackUsefulCount = useful,
            FeedbackUsefulPercent = feedbackUsefulPct,
            MaterialsReused = docs.Count(d => d.SourceDocumentId != null),
            ClassesWithMaterial = classIdsWithMaterial.Count,
            ClassesWithEvidence = classesWithEvidence,
            EvidenceCoveragePercent = evidencePct,
            AiGenerations = aiRows.Count,
            EstimatedAiCostUsd = aiRows.Sum(r => r.EstimatedCostUsd ?? 0),
            AvgAiLatencyMs = aiRows.Count == 0 ? 0 : (long)aiRows.Average(r => r.LatencyMilliseconds),
            SessionReports = reports.Count,
            AvgMinutesSavedReported = reports.Count == 0 ? null : Math.Round(reports.Average(), 1)
        };
        dto.SummaryLine =
            $"{dto.MaterialsCreated} materiales · {dto.ExportRatePercent:0.#}% exportados · " +
            $"{dto.FeedbackUsefulPercent:0.#}% feedback útil · " +
            $"evidencia en {dto.EvidenceCoveragePercent:0.#}% clases con material" +
            (dto.AvgMinutesSavedReported is double m ? $" · ~{m:0.#} min ahorrados (autoreporte)" : "");
        return dto;
    }

    public async Task<PilotSessionReportDto> SubmitSessionReportAsync(
        SubmitPilotSessionReportRequest request, CancellationToken ct = default)
    {
        if (_current.UserId is not Guid userId)
            throw new InvalidOperationException("Debe iniciar sesión.");

        if (request.MinutesSavedEstimate is < 0 or > 480)
            throw new ArgumentOutOfRangeException(nameof(request.MinutesSavedEstimate), "Indique entre 0 y 480 minutos.");

        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        if (comment is { Length: > 1000 })
            comment = comment[..1000];

        var row = new PilotSessionReport
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InstitutionId = _current.ActiveInstitutionId is Guid i && i != Guid.Empty ? i : null,
            ClassId = request.ClassId,
            MinutesSavedEstimate = request.MinutesSavedEstimate,
            WouldUseAgain = request.WouldUseAgain,
            MaterialsUsedInClass = request.MaterialsUsedInClass,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };
        _db.PilotSessionReports.Add(row);
        await _db.SaveChangesAsync(ct);
        return Map(row);
    }

    public async Task<IReadOnlyList<PilotSessionReportDto>> ListSessionReportsAsync(
        int take = 50, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var q = _db.PilotSessionReports.AsNoTracking().AsQueryable();
        if (_current.UserId is Guid uid && !IsPrivileged())
            q = q.Where(r => r.UserId == uid);
        return await q.OrderByDescending(r => r.CreatedAt).Take(take)
            .Select(r => new PilotSessionReportDto
            {
                Id = r.Id,
                ClassId = r.ClassId,
                MinutesSavedEstimate = r.MinutesSavedEstimate,
                WouldUseAgain = r.WouldUseAgain,
                MaterialsUsedInClass = r.MaterialsUsedInClass,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);
    }

    private async Task<List<Guid>> ResolveScopedClassIdsAsync(CancellationToken ct)
    {
        var isAdmin = IsPrivileged();
        var plans = _db.Planificaciones.AsNoTracking().Where(p => !p.IsDeleted);
        if (!isAdmin && _current.UserId is Guid uid)
        {
            var inst = _current.ActiveInstitutionId;
            plans = plans.Where(p => p.OwnerUserId == uid || (inst != null && p.InstitutionId == inst));
        }

        var planIds = await plans.Select(p => p.Id).ToListAsync(ct);
        return await _db.Clases.AsNoTracking()
            .Where(c => planIds.Contains(c.PlanificacionId))
            .Select(c => c.Id)
            .ToListAsync(ct);
    }

    private bool IsPrivileged() =>
        _current.IsInRole(nameof(ApplicationRole.SystemAdministrator))
        || _current.IsInRole(nameof(ApplicationRole.SchoolAdministrator));

    private static PilotSessionReportDto Map(PilotSessionReport r) => new()
    {
        Id = r.Id,
        ClassId = r.ClassId,
        MinutesSavedEstimate = r.MinutesSavedEstimate,
        WouldUseAgain = r.WouldUseAgain,
        MaterialsUsedInClass = r.MaterialsUsedInClass,
        Comment = r.Comment,
        CreatedAt = r.CreatedAt
    };
}
