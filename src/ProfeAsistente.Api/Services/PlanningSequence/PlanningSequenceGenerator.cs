using ProfeAsistente.Api.Models.Planning;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Services.PlanningSequence;

public sealed class SequenceDraftItem
{
    public Guid CalendarSessionId { get; init; }
    public int Order { get; init; }
    public Guid? ObjectiveId { get; set; }
    public string BloomLevel { get; set; } = "Recordar";
    public PlanningClassType ClassType { get; set; }
    public string? Title { get; set; }
    public string? Purpose { get; set; }
    public List<Guid> IndicatorIds { get; set; } = [];
    public List<(Guid IndicatorId, IndicatorUsageType Usage)> IndicatorUsages { get; set; } = [];
    public bool IsLocked { get; set; }
    public List<string> Warnings { get; set; } = [];
}

public sealed class SequenceGenerationResult
{
    public List<SequenceDraftItem> Items { get; init; } = [];
    public SequenceDeficitDto? Deficit { get; init; }
    public List<string> Warnings { get; init; } = [];
}

public sealed class PlanningSequenceGenerator
{
    private readonly BloomProgressionService _bloom;

    public PlanningSequenceGenerator(BloomProgressionService bloom) => _bloom = bloom;

    public SequenceGenerationResult Generate(
        IReadOnlyList<PlanningCalendarSession> availableSessions,
        GeneratePlanningSequenceRequest request,
        IReadOnlyDictionary<Guid, string> objectiveCodes,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> indicatorsByObjective)
    {
        var sessions = availableSessions
            .Where(s => s.Status is PlanningSessionStatus.Available or PlanningSessionStatus.Assigned or PlanningSessionStatus.Planned or PlanningSessionStatus.Rescheduled)
            .OrderBy(s => s.SessionNumber)
            .ToList();

        var reserved =
            (request.IncludeDiagnosticClass ? 1 : 0)
            + (request.IncludeReviewClasses ? Math.Max(0, request.ReviewClassCount) : 0)
            + (request.IncludeAssessmentClass ? Math.Max(0, request.AssessmentClassCount) : 0);

        var objectives = request.Objectives.OrderBy(o => o.Priority).ThenBy(o => o.ObjectiveId).ToList();
        var minRequired = objectives.Sum(o => Math.Max(1, o.MinimumSessions)) + reserved;

        if (sessions.Count < minRequired)
        {
            return new SequenceGenerationResult
            {
                Deficit = new SequenceDeficitDto
                {
                    AvailableSessions = sessions.Count,
                    RequiredMinimumSessions = minRequired,
                    Deficit = minRequired - sessions.Count,
                    Alternatives =
                    [
                        "Reducir sesiones mínimas por objetivo",
                        "Ampliar el rango de fechas",
                        "Agregar otro día de clases",
                        "Eliminar una sesión de repaso",
                        "Combinar objetivos compatibles (requiere aprobación)"
                    ]
                },
                Warnings = [$"Déficit de {minRequired - sessions.Count} sesiones para cubrir mínimos solicitados."]
            };
        }

        var items = sessions.Select((s, i) => new SequenceDraftItem
        {
            CalendarSessionId = s.Id,
            Order = i + 1,
            IsLocked = s.IsLocked || (request.PreserveLockedSessions && s.IsLocked),
            ClassType = PlanningClassType.Regular
        }).ToList();

        var cursor = 0;
        if (request.IncludeDiagnosticClass && cursor < items.Count)
        {
            items[cursor].ClassType = PlanningClassType.Diagnostic;
            items[cursor].Title = "Diagnóstico inicial";
            items[cursor].Purpose = "Identificar conocimientos previos.";
            items[cursor].BloomLevel = request.BloomProgression.InitialLevel.ToString();
            if (objectives.Count > 0)
            {
                items[cursor].ObjectiveId = objectives[0].ObjectiveId;
                items[cursor].IndicatorIds = PickIndicators(objectives[0], indicatorsByObjective, 1);
            }
            cursor++;
        }

        var contentSlots = items.Count
            - cursor
            - (request.IncludeReviewClasses ? Math.Max(0, request.ReviewClassCount) : 0)
            - (request.IncludeAssessmentClass ? Math.Max(0, request.AssessmentClassCount) : 0);

        var allocation = AllocateSessions(objectives, contentSlots);
        foreach (var oa in objectives)
        {
            var count = allocation[oa.ObjectiveId];
            var blooms = _bloom.SuggestForObjective(count, request.BloomProgression);
            var indicators = oa.IndicatorIds.Count > 0
                ? oa.IndicatorIds.ToList()
                : indicatorsByObjective.GetValueOrDefault(oa.ObjectiveId, []).ToList();

            for (var i = 0; i < count && cursor < items.Count; i++)
            {
                var item = items[cursor++];
                if (item.IsLocked && request.PreserveLockedSessions) continue;
                item.ObjectiveId = oa.ObjectiveId;
                item.BloomLevel = blooms[Math.Min(i, blooms.Count - 1)].ToString();
                item.ClassType = i == 0 ? PlanningClassType.Introduction :
                    i == count - 1 ? PlanningClassType.Practice : PlanningClassType.Practice;
                var code = objectiveCodes.GetValueOrDefault(oa.ObjectiveId, "OA");
                item.Title = $"{code} — sesión {i + 1}";
                item.Purpose = $"Trabajar {code} en nivel {item.BloomLevel}.";
                item.IndicatorIds = PickRoundRobin(indicators, i, Math.Max(1, Math.Min(2, indicators.Count)));
                var usage = i == 0 ? IndicatorUsageType.Introduction :
                    i == count - 1 ? IndicatorUsageType.Practice : IndicatorUsageType.Practice;
                item.IndicatorUsages = item.IndicatorIds.Select(id => (id, usage)).ToList();
            }
        }

        if (request.IncludeReviewClasses)
        {
            for (var r = 0; r < request.ReviewClassCount && cursor < items.Count; r++)
            {
                var item = items[cursor++];
                item.ClassType = PlanningClassType.Review;
                item.Title = "Repaso";
                item.Purpose = "Consolidar aprendizajes de la unidad.";
                item.BloomLevel = request.BloomProgression.TargetLevel.ToString();
                if (objectives.Count > 0)
                {
                    var oa = objectives[r % objectives.Count];
                    item.ObjectiveId = oa.ObjectiveId;
                    item.IndicatorIds = PickIndicators(oa, indicatorsByObjective, 2);
                    item.IndicatorUsages = item.IndicatorIds.Select(id => (id, IndicatorUsageType.Review)).ToList();
                }
            }
        }

        if (request.IncludeAssessmentClass)
        {
            for (var a = 0; a < request.AssessmentClassCount && cursor < items.Count; a++)
            {
                var item = items[cursor++];
                item.ClassType = PlanningClassType.SummativeAssessment;
                item.Title = "Evaluación";
                item.Purpose = "Evaluar logros de la unidad.";
                item.BloomLevel = request.BloomProgression.TargetLevel.ToString();
                if (objectives.Count > 0)
                {
                    var oa = objectives[a % objectives.Count];
                    item.ObjectiveId = oa.ObjectiveId;
                    item.IndicatorIds = PickIndicators(oa, indicatorsByObjective, 2);
                    item.IndicatorUsages = item.IndicatorIds
                        .Select(id => (id, IndicatorUsageType.SummativeAssessment)).ToList();
                }
            }
        }

        // Remaining sessions: distribute leftover to highest priority OA
        while (cursor < items.Count && objectives.Count > 0)
        {
            var oa = objectives[0];
            var item = items[cursor++];
            item.ObjectiveId = oa.ObjectiveId;
            item.ClassType = PlanningClassType.Practice;
            item.BloomLevel = request.BloomProgression.TargetLevel.ToString();
            item.Title = $"{objectiveCodes.GetValueOrDefault(oa.ObjectiveId, "OA")} — práctica adicional";
            item.IndicatorIds = PickIndicators(oa, indicatorsByObjective, 1);
        }

        DetectBloomIssues(items, request.BloomProgression);

        return new SequenceGenerationResult { Items = items, Warnings = items.SelectMany(i => i.Warnings).Distinct().ToList() };
    }

    private static Dictionary<Guid, int> AllocateSessions(IReadOnlyList<ObjectiveCoverageRequest> objectives, int contentSlots)
    {
        var alloc = objectives.ToDictionary(o => o.ObjectiveId, o => Math.Max(1, o.MinimumSessions));
        var used = alloc.Values.Sum();
        var remaining = Math.Max(0, contentSlots - used);
        var idx = 0;
        while (remaining > 0 && objectives.Count > 0)
        {
            var oa = objectives[idx % objectives.Count];
            if (oa.MaximumSessions is int max && alloc[oa.ObjectiveId] >= max)
            {
                idx++;
                if (objectives.All(o => o.MaximumSessions is int m && alloc[o.ObjectiveId] >= m))
                    break;
                continue;
            }

            alloc[oa.ObjectiveId]++;
            remaining--;
            idx++;
        }

        // If still over contentSlots (shouldn't), trim from lowest priority
        used = alloc.Values.Sum();
        while (used > contentSlots)
        {
            var candidate = objectives.OrderByDescending(o => o.Priority).ThenByDescending(o => o.ObjectiveId)
                .FirstOrDefault(o => alloc[o.ObjectiveId] > o.MinimumSessions);
            if (candidate is null) break;
            alloc[candidate.ObjectiveId]--;
            used--;
        }

        return alloc;
    }

    private static List<Guid> PickIndicators(
        ObjectiveCoverageRequest oa,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> byObjective,
        int count)
    {
        var source = oa.IndicatorIds.Count > 0
            ? oa.IndicatorIds
            : byObjective.GetValueOrDefault(oa.ObjectiveId, []);
        return source.Take(count).ToList();
    }

    private static List<Guid> PickRoundRobin(IReadOnlyList<Guid> indicators, int index, int count)
    {
        if (indicators.Count == 0) return [];
        var result = new List<Guid>();
        for (var i = 0; i < count; i++)
            result.Add(indicators[(index + i) % indicators.Count]);
        return result.Distinct().ToList();
    }

    private void DetectBloomIssues(List<SequenceDraftItem> items, BloomProgressionSettingsRequest settings)
    {
        NivelBloom? prev = null;
        var streak = new List<NivelBloom>();
        foreach (var item in items.Where(i => i.ClassType is not PlanningClassType.Diagnostic))
        {
            if (!Enum.TryParse<NivelBloom>(item.BloomLevel, true, out var level))
                continue;
            streak.Add(level);
            if (prev is NivelBloom p && _bloom.IsExcessiveJump(p, level, settings.MaximumLevelJump)
                && item.ClassType is not (PlanningClassType.Review or PlanningClassType.SummativeAssessment))
            {
                item.Warnings.Add($"Posible salto de Bloom: {p} → {level}.");
            }

            if (!settings.AllowRegression && prev is NivelBloom prevLevel && (int)level < (int)prevLevel
                && item.ClassType is not (PlanningClassType.Review or PlanningClassType.Diagnostic))
            {
                item.Warnings.Add($"Retroceso de Bloom no permitido por configuración: {prevLevel} → {level}.");
            }

            prev = level;
        }

        if (_bloom.IsStagnation(streak))
            items[^1].Warnings.Add("Repetición excesiva del mismo nivel de Bloom.");
    }
}

public sealed class PlanningSequenceException : Exception
{
    public string ErrorCode { get; }
    public PlanningSequenceException(string code, string message) : base(message) => ErrorCode = code;
}

public sealed class PlanningSequenceValidator
{
    public PlanningSequenceValidationDto Validate(
        PlanningSequenceProposal proposal,
        IReadOnlyList<PlanningCalendarSession> sessions,
        IReadOnlySet<Guid> unitObjectiveIds,
        IReadOnlyDictionary<Guid, Guid> indicatorToObjective)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (proposal.Items.Count == 0)
            errors.Add("La propuesta no tiene sesiones.");

        var sessionIds = sessions.Select(s => s.Id).ToHashSet();
        if (proposal.Items.Select(i => i.CalendarSessionId).Distinct().Count() != proposal.Items.Count)
            errors.Add("Existen sesiones duplicadas en la propuesta.");

        foreach (var item in proposal.Items)
        {
            if (!sessionIds.Contains(item.CalendarSessionId))
                errors.Add($"Ítem {item.Order}: sesión calendario inexistente.");

            var session = sessions.FirstOrDefault(s => s.Id == item.CalendarSessionId);
            if (session?.IsLocked == true && item.WasManuallyModified)
                errors.Add($"Ítem {item.Order}: se modificó una sesión bloqueada.");

            if (item.ObjectiveLearningId is Guid oa && !unitObjectiveIds.Contains(oa))
                errors.Add($"Ítem {item.Order}: OA no pertenece a la unidad.");

            foreach (var ind in item.Indicators)
            {
                if (!indicatorToObjective.TryGetValue(ind.EvaluationIndicatorId, out var oaId))
                    errors.Add($"Ítem {item.Order}: indicador no publicado o inexistente.");
                else if (item.ObjectiveLearningId is Guid itemOa && oaId != itemOa)
                    errors.Add($"Ítem {item.Order}: indicador no pertenece al OA asignado.");
            }
        }

        if (proposal.Items.Count > sessions.Count(s => s.Status != PlanningSessionStatus.Cancelled))
            errors.Add("Hay más ítems que sesiones disponibles.");

        return new PlanningSequenceValidationDto
        {
            ProposalId = proposal.Id,
            IsValid = errors.Count == 0,
            CanConfirm = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }
}
