using System.Text.Json;
using AppEducativa.Api.Data;
using AppEducativa.Api.Models.Planning;
using AppEducativa.Api.Services.DateTimeServices;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api.Services.Coverage;

public interface ICurriculumCoverageService
{
    Task<PlanningCoverageDto> GetCoverageAsync(Guid planningId, string mode = "Planned", CancellationToken cancellationToken = default);
    Task<PlanningCoverageDto> RecalculateAsync(Guid planningId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlanningAlertDto>> GetAlertsAsync(Guid planningId, bool includeResolved = false, CancellationToken cancellationToken = default);
    Task CompleteClassAsync(Guid classId, CompleteClassRequest request, CancellationToken cancellationToken = default);
}

public sealed class CurriculumCoverageCalculator
{
    public PlanningCoverageDto Calculate(
        Guid planningId,
        string mode,
        IReadOnlyList<Models.Clase> classes,
        IReadOnlyList<PlanningCalendarSession> sessions,
        IReadOnlyDictionary<Guid, string> oaCodes,
        IReadOnlyDictionary<Guid, (string Code, Guid OaId)> indicators,
        IReadOnlyDictionary<Guid, int> minSessionsByOa,
        IReadOnlySet<Guid> classesWithStructure,
        IReadOnlySet<Guid> classesWithMaterials,
        IReadOnlyList<ClassLearningEvidence> evidences,
        int blockingAlerts)
    {
        var activeSessions = sessions.Where(s => s.Status != PlanningSessionStatus.Cancelled).ToList();
        var consideredClasses = mode.Equals("Executed", StringComparison.OrdinalIgnoreCase)
            ? classes.Where(c => c.Estado == EstadoClase.Realizada).ToList()
            : classes.ToList();

        var objectiveIds = consideredClasses.Select(c => c.ObjetivoAprendizajeId).Distinct().ToList();
        var objectiveDtos = new List<ObjectiveCoverageDto>();
        foreach (var oaId in oaCodes.Keys.OrderBy(x => oaCodes[x]))
        {
            var oaClasses = consideredClasses.Where(c => c.ObjetivoAprendizajeId == oaId).OrderBy(c => c.Numero).ToList();
            var min = minSessionsByOa.GetValueOrDefault(oaId, 1);
            var assigned = oaClasses.Count;
            var blooms = oaClasses.Select(c => c.NivelBloom).Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
            var hasAssessment = oaClasses.Any(c => c.ClassType is PlanningClassType.FormativeAssessment or PlanningClassType.SummativeAssessment);
            var worked = oaClasses.Any(c => c.Estado == EstadoClase.Realizada);
            var evidenced = evidences.Any(e => oaClasses.Any(c => c.Id == e.ClassId));
            var planned = oaClasses.Any(c => classesWithStructure.Contains(c.Id) || !string.IsNullOrWhiteSpace(c.Proposito));
            var status = assigned == 0 ? PlanningCoverageStatus.Missing :
                assigned < min ? PlanningCoverageStatus.Partial :
                PlanningCoverageStatus.Covered;

            objectiveDtos.Add(new ObjectiveCoverageDto
            {
                ObjectiveId = oaId,
                Code = oaCodes[oaId],
                AssignedSessions = assigned,
                MinimumSessions = min,
                CoveragePercent = min == 0 ? 0 : Math.Round(100m * assigned / min, 1),
                InitialBloom = blooms.FirstOrDefault(),
                MaxBloom = blooms.OrderByDescending(NivelBloomHelper.Orden).FirstOrDefault(),
                HasAssessment = hasAssessment,
                Status = status,
                Assigned = assigned > 0,
                Planned = planned,
                Worked = worked,
                Evidenced = evidenced,
                Evaluated = hasAssessment && (mode != "Executed" || worked)
            });
        }

        var indicatorDtos = new List<IndicatorCoverageDto>();
        foreach (var (indId, meta) in indicators)
        {
            var related = consideredClasses
                .Where(c => c.Indicadores.Any(i => i.IndicadorEvaluacionId == indId))
                .ToList();
            var usages = new List<IndicatorUsageType>();
            foreach (var c in related)
            {
                usages.Add(c.ClassType switch
                {
                    PlanningClassType.Introduction => IndicatorUsageType.Introduction,
                    PlanningClassType.Review => IndicatorUsageType.Review,
                    PlanningClassType.FormativeAssessment => IndicatorUsageType.FormativeAssessment,
                    PlanningClassType.SummativeAssessment => IndicatorUsageType.SummativeAssessment,
                    _ => IndicatorUsageType.Practice
                });
            }

            indicatorDtos.Add(new IndicatorCoverageDto
            {
                IndicatorId = indId,
                Code = meta.Code,
                ObjectiveId = meta.OaId,
                AssociatedClasses = related.Count,
                UsageTypes = usages.Distinct().ToList(),
                HasFormative = usages.Contains(IndicatorUsageType.FormativeAssessment),
                HasSummative = usages.Contains(IndicatorUsageType.SummativeAssessment),
                Status = related.Count == 0 ? PlanningCoverageStatus.Missing :
                    related.Count == 1 ? PlanningCoverageStatus.Partial : PlanningCoverageStatus.Covered
            });
        }

        var bloomDist = consideredClasses
            .GroupBy(c => c.NivelBloom)
            .Select(g => new BloomDistributionDto { BloomLevel = g.Key, Count = g.Count() })
            .OrderBy(b => NivelBloomHelper.Orden(b.BloomLevel))
            .ToList();

        var matrix = BuildMatrix(consideredClasses, oaCodes, indicators);

        return new PlanningCoverageDto
        {
            PlanningId = planningId,
            Mode = mode,
            AvailableSessions = activeSessions.Count,
            UsedSessions = activeSessions.Count(s => s.ClassId.HasValue),
            FreeSessions = activeSessions.Count(s => s.ClassId is null && s.Status == PlanningSessionStatus.Available),
            SelectedObjectives = objectiveDtos.Count,
            CoveredObjectives = objectiveDtos.Count(o => o.Status == PlanningCoverageStatus.Covered),
            SelectedIndicators = indicatorDtos.Count,
            CoveredIndicators = indicatorDtos.Count(i => i.Status != PlanningCoverageStatus.Missing),
            ClassesWithStructure = consideredClasses.Count(c => classesWithStructure.Contains(c.Id)),
            ClassesWithMaterials = consideredClasses.Count(c => classesWithMaterials.Contains(c.Id)),
            Assessments = consideredClasses.Count(c => c.ClassType is PlanningClassType.FormativeAssessment or PlanningClassType.SummativeAssessment),
            BlockingAlerts = blockingAlerts,
            Objectives = objectiveDtos,
            Indicators = indicatorDtos,
            BloomDistribution = bloomDist,
            Matrix = matrix
        };
    }

    private static CoverageMatrixDto BuildMatrix(
        IReadOnlyList<Models.Clase> classes,
        IReadOnlyDictionary<Guid, string> oaCodes,
        IReadOnlyDictionary<Guid, (string Code, Guid OaId)> indicators)
    {
        var ordered = classes.OrderBy(c => c.Numero).ToList();
        var labels = ordered.Select(c => $"Clase {c.Numero}").ToList();
        var rows = new List<CoverageMatrixRowDto>();

        foreach (var oa in oaCodes.OrderBy(x => x.Value))
        {
            rows.Add(new CoverageMatrixRowDto
            {
                Label = oa.Value,
                Kind = "OA",
                EntityId = oa.Key,
                Cells = ordered.Select(c => c.ObjetivoAprendizajeId == oa.Key ? "X" : "").ToList()
            });
        }

        foreach (var ind in indicators.OrderBy(x => x.Value.Code))
        {
            rows.Add(new CoverageMatrixRowDto
            {
                Label = ind.Value.Code,
                Kind = "Indicador",
                EntityId = ind.Key,
                Cells = ordered.Select(c =>
                {
                    if (!c.Indicadores.Any(i => i.IndicadorEvaluacionId == ind.Key)) return "";
                    return c.ClassType switch
                    {
                        PlanningClassType.Introduction => "I",
                        PlanningClassType.Review => "R",
                        PlanningClassType.FormativeAssessment => "F",
                        PlanningClassType.SummativeAssessment => "E",
                        _ => "P"
                    };
                }).ToList()
            });
        }

        return new CoverageMatrixDto { ClassLabels = labels, Rows = rows };
    }
}

public sealed class CurriculumCoverageValidator
{
    public IReadOnlyList<PlanningAlert> BuildAlerts(
        Guid planningId,
        PlanningCoverageDto coverage,
        IReadOnlyList<Models.Clase> classes,
        IReadOnlyList<PlanningCalendarSession> sessions,
        GeneratePlanningSequenceRequest? sequenceConfig)
    {
        var alerts = new List<PlanningAlert>();
        void Add(string code, PlanningAlertSeverity severity, string message, Guid? classId = null, Guid? oa = null, Guid? ind = null)
        {
            alerts.Add(new PlanningAlert
            {
                Id = Guid.NewGuid(),
                PlanningId = planningId,
                ClassId = classId,
                ObjectiveId = oa,
                IndicatorId = ind,
                AlertCode = code,
                Severity = severity,
                Message = message,
                GeneratedAt = DateTime.UtcNow
            });
        }

        if (sessions.Count(s => s.Status != PlanningSessionStatus.Cancelled) == 0)
            Add("PLAN_NO_SESSIONS", PlanningAlertSeverity.Blocking, "La planificación no tiene sesiones de calendario.");

        foreach (var oa in coverage.Objectives.Where(o => o.Status == PlanningCoverageStatus.Missing))
            Add("PLAN_OBJECTIVE_WITHOUT_SESSION", PlanningAlertSeverity.Error, $"El OA {oa.Code} no tiene clases asignadas.", oa: oa.ObjectiveId);

        foreach (var oa in coverage.Objectives.Where(o => o.Status == PlanningCoverageStatus.Partial))
            Add("PLAN_OBJECTIVE_BELOW_MINIMUM", PlanningAlertSeverity.Warning, $"El OA {oa.Code} tiene menos sesiones que el mínimo ({oa.AssignedSessions}/{oa.MinimumSessions}).", oa: oa.ObjectiveId);

        foreach (var ind in coverage.Indicators.Where(i => i.Status == PlanningCoverageStatus.Missing))
            Add("PLAN_INDICATOR_NOT_COVERED", PlanningAlertSeverity.Warning, $"El indicador {ind.Code} no está cubierto.", ind: ind.IndicatorId);

        foreach (var c in classes.Where(c => c.ObjetivoAprendizajeId == Guid.Empty))
            Add("PLAN_CLASS_WITHOUT_OBJECTIVE", PlanningAlertSeverity.Error, $"La clase {c.Numero} no tiene OA.", classId: c.Id);

        var planStart = sessions.Select(s => s.ScheduledDate).DefaultIfEmpty().Min();
        var planEnd = sessions.Select(s => s.ScheduledDate).DefaultIfEmpty().Max();
        foreach (var c in classes)
        {
            if (sessions.Count > 0 && (c.Fecha < planStart || c.Fecha > planEnd))
                Add("PLAN_CLASS_OUTSIDE_RANGE", PlanningAlertSeverity.Warning, $"La clase {c.Numero} está fuera del rango del calendario.", classId: c.Id);
        }

        if (!classes.Any(c => c.ClassType is PlanningClassType.FormativeAssessment or PlanningClassType.SummativeAssessment))
            Add("PLAN_NO_ASSESSMENT", PlanningAlertSeverity.Information, "La unidad no tiene clases de evaluación.");

        var assessmentStreak = 0;
        foreach (var c in classes.OrderBy(x => x.Numero))
        {
            if (c.ClassType is PlanningClassType.FormativeAssessment or PlanningClassType.SummativeAssessment)
            {
                assessmentStreak++;
                if (assessmentStreak >= 3)
                    Add("PLAN_EXCESSIVE_ASSESSMENTS", PlanningAlertSeverity.Warning, "Hay demasiadas evaluaciones consecutivas.");
            }
            else assessmentStreak = 0;
        }

        // Bloom jump across consecutive classes
        Models.Clase? prev = null;
        foreach (var c in classes.OrderBy(x => x.Numero))
        {
            if (prev is not null)
            {
                var jump = NivelBloomHelper.Orden(c.NivelBloom) - NivelBloomHelper.Orden(prev.NivelBloom);
                if (jump > 2)
                    Add("PLAN_BLOOM_JUMP", PlanningAlertSeverity.Warning, $"Posible salto de Bloom entre clase {prev.Numero} y {c.Numero}.", classId: c.Id);
            }
            prev = c;
        }

        // Indicator evaluated before practiced
        foreach (var ind in coverage.Indicators)
        {
            var related = classes.Where(c => c.Indicadores.Any(i => i.IndicadorEvaluacionId == ind.IndicatorId))
                .OrderBy(c => c.Numero).ToList();
            var firstEval = related.FirstOrDefault(c => c.ClassType is PlanningClassType.FormativeAssessment or PlanningClassType.SummativeAssessment);
            var firstPractice = related.FirstOrDefault(c => c.ClassType is PlanningClassType.Introduction or PlanningClassType.Practice or PlanningClassType.Regular);
            if (firstEval is not null && (firstPractice is null || firstEval.Numero < firstPractice.Numero))
                Add("PLAN_INDICATOR_EVALUATED_TOO_EARLY", PlanningAlertSeverity.Warning,
                    $"El indicador {ind.Code} se evalúa antes de ser trabajado.", ind: ind.IndicatorId);
        }

        _ = sequenceConfig;
        return alerts;
    }
}

public sealed class CurriculumCoverageService : ICurriculumCoverageService
{
    private readonly AppEducativaDbContext _db;
    private readonly CurriculumCoverageCalculator _calculator;
    private readonly CurriculumCoverageValidator _validator;
    private readonly IApplicationClock _clock;
    private readonly ILogger<CurriculumCoverageService> _logger;

    public CurriculumCoverageService(
        AppEducativaDbContext db,
        CurriculumCoverageCalculator calculator,
        CurriculumCoverageValidator validator,
        IApplicationClock clock,
        ILogger<CurriculumCoverageService> logger)
    {
        _db = db;
        _calculator = calculator;
        _validator = validator;
        _clock = clock;
        _logger = logger;
    }

    public async Task<PlanningCoverageDto> GetCoverageAsync(Guid planningId, string mode = "Planned", CancellationToken cancellationToken = default)
    {
        var plan = await _db.Planificaciones.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planningId, cancellationToken)
            ?? throw new InvalidOperationException("Planificación no encontrada.");

        var classes = await _db.Clases.AsNoTracking()
            .Include(c => c.Indicadores)
            .Where(c => c.PlanificacionId == planningId)
            .ToListAsync(cancellationToken);
        var sessions = await _db.PlanningCalendarSessions.AsNoTracking()
            .Where(s => s.PlanningId == planningId).ToListAsync(cancellationToken);

        var unitOas = await _db.UnidadObjetivos.AsNoTracking()
            .Where(u => u.UnidadId == plan.UnidadId)
            .Select(u => u.ObjetivoAprendizajeId)
            .ToListAsync(cancellationToken);
        var oaCodes = await _db.ObjetivosAprendizaje.AsNoTracking()
            .Where(o => unitOas.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Codigo, cancellationToken);
        var indicators = await _db.IndicadoresEvaluacion.AsNoTracking()
            .Where(i => unitOas.Contains(i.ObjetivoAprendizajeId))
            .ToDictionaryAsync(i => i.Id, i => (i.Codigo ?? i.Id.ToString("N")[..8], i.ObjetivoAprendizajeId), cancellationToken);

        var classIds = classes.Select(c => c.Id).ToList();
        var withStructure = await _db.ClassStructureGenerations.AsNoTracking()
            .Where(g => classIds.Contains(g.ClassId))
            .Select(g => g.ClassId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var structureSet = classes
            .Where(c => !string.IsNullOrWhiteSpace(c.DescripcionInicio) || !string.IsNullOrWhiteSpace(c.DescripcionDesarrollo) || !string.IsNullOrWhiteSpace(c.DescripcionCierre))
            .Select(c => c.Id)
            .Concat(withStructure)
            .ToHashSet();
        var withMaterials = await _db.EducationalDocuments.AsNoTracking()
            .Where(d => classIds.Contains(d.ClassId))
            .Select(d => d.ClassId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var evidences = await _db.ClassLearningEvidences.AsNoTracking()
            .Where(e => classIds.Contains(e.ClassId))
            .ToListAsync(cancellationToken);

        var mins = new Dictionary<Guid, int>();
        var currentProposal = await _db.PlanningSequenceProposals.AsNoTracking()
            .Where(p => p.PlanningId == planningId && p.IsCurrent)
            .Select(p => p.ConfigurationJson)
            .FirstOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(currentProposal))
        {
            try
            {
                var cfg = JsonSerializer.Deserialize<GeneratePlanningSequenceRequest>(currentProposal, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (cfg is not null)
                {
                    foreach (var o in cfg.Objectives)
                        mins[o.ObjectiveId] = o.MinimumSessions;
                }
            }
            catch { /* ignore */ }
        }

        var blocking = await _db.PlanningAlerts.AsNoTracking()
            .CountAsync(a => a.PlanningId == planningId && !a.IsResolved && a.Severity == PlanningAlertSeverity.Blocking, cancellationToken);

        return _calculator.Calculate(
            planningId, mode, classes, sessions, oaCodes, indicators, mins,
            structureSet, withMaterials.ToHashSet(), evidences, blocking);
    }

    public async Task<PlanningCoverageDto> RecalculateAsync(Guid planningId, CancellationToken cancellationToken = default)
    {
        var coverage = await GetCoverageAsync(planningId, "Planned", cancellationToken);
        var classes = await _db.Clases.Include(c => c.Indicadores).Where(c => c.PlanificacionId == planningId).ToListAsync(cancellationToken);
        var sessions = await _db.PlanningCalendarSessions.Where(s => s.PlanningId == planningId).ToListAsync(cancellationToken);

        var existing = await _db.PlanningAlerts.Where(a => a.PlanningId == planningId && !a.IsResolved).ToListAsync(cancellationToken);
        _db.PlanningAlerts.RemoveRange(existing);

        var alerts = _validator.BuildAlerts(planningId, coverage, classes, sessions, null);
        _db.PlanningAlerts.AddRange(alerts);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("CoverageRecalculated planning={PlanningId} alerts={Count}", planningId, alerts.Count);

        coverage = await GetCoverageAsync(planningId, "Planned", cancellationToken);
        return coverage;
    }

    public async Task<IReadOnlyList<PlanningAlertDto>> GetAlertsAsync(Guid planningId, bool includeResolved = false, CancellationToken cancellationToken = default)
    {
        var q = _db.PlanningAlerts.AsNoTracking().Where(a => a.PlanningId == planningId);
        if (!includeResolved) q = q.Where(a => !a.IsResolved);
        return await q.OrderByDescending(a => a.Severity).ThenByDescending(a => a.GeneratedAt)
            .Select(a => new PlanningAlertDto
            {
                Id = a.Id,
                PlanningId = a.PlanningId,
                ClassId = a.ClassId,
                ObjectiveId = a.ObjectiveId,
                IndicatorId = a.IndicatorId,
                AlertCode = a.AlertCode,
                Severity = a.Severity,
                Message = a.Message,
                IsResolved = a.IsResolved,
                GeneratedAt = a.GeneratedAt
            }).ToListAsync(cancellationToken);
    }

    public async Task CompleteClassAsync(Guid classId, CompleteClassRequest request, CancellationToken cancellationToken = default)
    {
        var clase = await _db.Clases.FirstOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new InvalidOperationException("Clase no encontrada.");

        clase.Estado = EstadoClase.Realizada;
        clase.ActualDate = request.ActualDate ?? clase.Fecha;
        clase.ActualDurationMinutes = request.ActualDurationMinutes;
        clase.CompletionNotes = request.Observation;
        clase.CompletedAt = _clock.UtcNow;

        foreach (var ev in request.Evidences)
        {
            _db.ClassLearningEvidences.Add(new ClassLearningEvidence
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                EvaluationIndicatorId = ev.EvaluationIndicatorId,
                EvidenceType = ev.EvidenceType,
                Description = ev.Description,
                Source = ev.Source,
                Notes = ev.Notes,
                RecordedAt = _clock.UtcNow
            });
        }

        foreach (var indId in request.EvidencedIndicatorIds.Distinct())
        {
            if (request.Evidences.Any(e => e.EvaluationIndicatorId == indId)) continue;
            _db.ClassLearningEvidences.Add(new ClassLearningEvidence
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                EvaluationIndicatorId = indId,
                EvidenceType = LearningEvidenceType.Observation,
                Description = "Indicador evidenciado al completar la clase.",
                Source = "Teacher",
                RecordedAt = _clock.UtcNow
            });
        }

        var session = await _db.PlanningCalendarSessions.FirstOrDefaultAsync(s => s.ClassId == classId, cancellationToken);
        if (session is not null)
        {
            session.Status = PlanningSessionStatus.Completed;
            session.UpdatedAt = _clock.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await RecalculateAsync(clase.PlanificacionId, cancellationToken);
        _logger.LogInformation("ClassCompleted class={ClassId}", classId);
    }
}
