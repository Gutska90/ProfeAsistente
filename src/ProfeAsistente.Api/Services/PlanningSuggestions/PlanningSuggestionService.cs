using System.Text.Json;
using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.Planning;
using ProfeAsistente.Api.Services.Coverage;
using ProfeAsistente.Api.Services.DateTimeServices;
using ProfeAsistente.Api.Services.PlanningCalendar;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services.PlanningSuggestions;

public interface IPlanningSuggestionService
{
    Task<IReadOnlyList<PlanningSuggestionDto>> GetSuggestionsAsync(Guid planningId, CancellationToken cancellationToken = default);
    Task ApplyAsync(Guid planningId, Guid suggestionId, CancellationToken cancellationToken = default);
    Task IgnoreAsync(Guid planningId, Guid suggestionId, CancellationToken cancellationToken = default);
}

public sealed class PlanningSuggestionService : IPlanningSuggestionService
{
    private readonly ProfeAsistenteDbContext _db;
    private readonly ICurriculumCoverageService _coverage;
    private readonly IPlanningCalendarService _calendar;
    private readonly IApplicationClock _clock;
    private readonly ILogger<PlanningSuggestionService> _logger;

    public PlanningSuggestionService(
        ProfeAsistenteDbContext db,
        ICurriculumCoverageService coverage,
        IPlanningCalendarService calendar,
        IApplicationClock clock,
        ILogger<PlanningSuggestionService> logger)
    {
        _db = db;
        _coverage = coverage;
        _calendar = calendar;
        _clock = clock;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PlanningSuggestionDto>> GetSuggestionsAsync(Guid planningId, CancellationToken cancellationToken = default)
    {
        var coverage = await _coverage.GetCoverageAsync(planningId, "Planned", cancellationToken);
        var ignored = await _db.PlanningSuggestionStates.AsNoTracking()
            .Where(s => s.PlanningId == planningId && (s.IsIgnored || s.IsApplied))
            .Select(s => s.SuggestionCode)
            .ToListAsync(cancellationToken);
        var ignoredSet = ignored.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var suggestions = new List<PlanningSuggestionDto>();

        foreach (var oa in coverage.Objectives.Where(o => o.Status is PlanningCoverageStatus.Missing or PlanningCoverageStatus.Partial))
        {
            var code = $"ADD_CLASS_FOR_{oa.ObjectiveId:N}";
            if (ignoredSet.Contains(code)) continue;
            suggestions.Add(new PlanningSuggestionDto
            {
                Id = StableId(planningId, code),
                Code = code,
                Reason = $"El OA {oa.Code} no alcanza el mínimo de sesiones.",
                Impact = "Agrega una sesión disponible o amplía el calendario.",
                Severity = PlanningAlertSeverity.Warning,
                CanApplyAutomatically = coverage.FreeSessions > 0,
                ProposedAction = "Asignar una sesión libre al OA",
                Preview = $"OA {oa.Code}: {oa.AssignedSessions}/{oa.MinimumSessions}"
            });
        }

        var cancelled = await _db.PlanningCalendarSessions.AsNoTracking()
            .Where(s => s.PlanningId == planningId && s.Status == PlanningSessionStatus.Cancelled)
            .OrderBy(s => s.ScheduledDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (cancelled is not null)
        {
            var code = $"RESTORE_SESSION_{cancelled.Id:N}";
            if (!ignoredSet.Contains(code))
            {
                suggestions.Add(new PlanningSuggestionDto
                {
                    Id = StableId(planningId, code),
                    Code = code,
                    Reason = "Hay una sesión cancelada que podría reprogramarse.",
                    Impact = "Recupera cobertura perdida.",
                    Severity = PlanningAlertSeverity.Information,
                    CanApplyAutomatically = true,
                    ProposedAction = "Restaurar sesión cancelada",
                    AffectedClassIds = cancelled.ClassId is Guid cid ? [cid] : [],
                    Preview = cancelled.ScheduledDate.ToString("yyyy-MM-dd")
                });
            }
        }

        if (coverage.Assessments == 0)
        {
            const string code = "ADD_ASSESSMENT";
            if (!ignoredSet.Contains(code))
            {
                suggestions.Add(new PlanningSuggestionDto
                {
                    Id = StableId(planningId, code),
                    Code = code,
                    Reason = "La unidad no tiene evaluación sumativa/formativa.",
                    Impact = "Mejora el cierre evaluativo de la unidad.",
                    Severity = PlanningAlertSeverity.Information,
                    CanApplyAutomatically = false,
                    ProposedAction = "Agregar clase de evaluación en la secuencia",
                    Preview = "Incluir AssessmentClassCount >= 1"
                });
            }
        }

        // Persist newly seen suggestions as states for apply/ignore
        foreach (var s in suggestions)
        {
            var exists = await _db.PlanningSuggestionStates.AnyAsync(
                x => x.PlanningId == planningId && x.Id == s.Id, cancellationToken);
            if (exists) continue;
            _db.PlanningSuggestionStates.Add(new PlanningSuggestionState
            {
                Id = s.Id,
                PlanningId = planningId,
                SuggestionCode = s.Code,
                PayloadJson = JsonSerializer.Serialize(s),
                CreatedAt = _clock.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return suggestions;
    }

    public async Task ApplyAsync(Guid planningId, Guid suggestionId, CancellationToken cancellationToken = default)
    {
        var state = await _db.PlanningSuggestionStates
            .FirstOrDefaultAsync(s => s.Id == suggestionId && s.PlanningId == planningId, cancellationToken)
            ?? throw new InvalidOperationException("Sugerencia no encontrada.");

        if (state.SuggestionCode.StartsWith("RESTORE_SESSION_", StringComparison.OrdinalIgnoreCase))
        {
            var sessionIdText = state.SuggestionCode["RESTORE_SESSION_".Length..];
            if (Guid.TryParseExact(sessionIdText, "N", out var sessionId))
                await _calendar.RestoreSessionAsync(sessionId, cancellationToken);
        }

        state.IsApplied = true;
        state.AppliedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _coverage.RecalculateAsync(planningId, cancellationToken);
        _logger.LogInformation("PlanningSuggestionApplied planning={PlanningId} suggestion={SuggestionId}", planningId, suggestionId);
    }

    public async Task IgnoreAsync(Guid planningId, Guid suggestionId, CancellationToken cancellationToken = default)
    {
        var state = await _db.PlanningSuggestionStates
            .FirstOrDefaultAsync(s => s.Id == suggestionId && s.PlanningId == planningId, cancellationToken);
        if (state is null)
        {
            _db.PlanningSuggestionStates.Add(new PlanningSuggestionState
            {
                Id = suggestionId,
                PlanningId = planningId,
                SuggestionCode = suggestionId.ToString("N"),
                IsIgnored = true,
                CreatedAt = _clock.UtcNow
            });
        }
        else
        {
            state.IsIgnored = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static Guid StableId(Guid planningId, string code)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{planningId:N}:{code}"));
        Span<byte> g = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(g);
        return new Guid(g);
    }
}
