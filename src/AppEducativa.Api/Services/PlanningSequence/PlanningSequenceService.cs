using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AppEducativa.Api.Data;
using AppEducativa.Api.Models;
using AppEducativa.Api.Models.Planning;
using AppEducativa.Api.Services.Coverage;
using AppEducativa.Api.Services.DateTimeServices;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api.Services.PlanningSequence;

public interface IPlanningSequenceService
{
    Task<PlanningSequenceProposalDto> GenerateProposalAsync(Guid planningId, GeneratePlanningSequenceRequest request, CancellationToken cancellationToken = default);
    Task<PlanningSequenceProposalDto?> GetCurrentProposalAsync(Guid planningId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlanningSequenceProposalDto>> ListProposalsAsync(Guid planningId, CancellationToken cancellationToken = default);
    Task<PlanningSequenceProposalDto?> GetProposalAsync(Guid proposalId, CancellationToken cancellationToken = default);
    Task<PlanningSequenceProposalDto> UpdateProposalItemAsync(Guid proposalId, Guid itemId, UpdatePlanningSequenceItemRequest request, CancellationToken cancellationToken = default);
    Task<PlanningSequenceValidationDto> ValidateProposalAsync(Guid proposalId, CancellationToken cancellationToken = default);
    Task ConfirmProposalAsync(Guid proposalId, CancellationToken cancellationToken = default);
    Task RejectProposalAsync(Guid proposalId, string reason, CancellationToken cancellationToken = default);
    Task DeleteProposalAsync(Guid proposalId, CancellationToken cancellationToken = default);
}

public sealed class PlanningSequenceService : IPlanningSequenceService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly AppEducativaDbContext _db;
    private readonly PlanningSequenceGenerator _generator;
    private readonly PlanningSequenceValidator _validator;
    private readonly ICurriculumCoverageService _coverage;
    private readonly IApplicationClock _clock;
    private readonly ILogger<PlanningSequenceService> _logger;

    public PlanningSequenceService(
        AppEducativaDbContext db,
        PlanningSequenceGenerator generator,
        PlanningSequenceValidator validator,
        ICurriculumCoverageService coverage,
        IApplicationClock clock,
        ILogger<PlanningSequenceService> logger)
    {
        _db = db;
        _generator = generator;
        _validator = validator;
        _coverage = coverage;
        _clock = clock;
        _logger = logger;
    }

    public async Task<PlanningSequenceProposalDto> GenerateProposalAsync(
        Guid planningId,
        GeneratePlanningSequenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = await _db.Planificaciones.FirstOrDefaultAsync(p => p.Id == planningId, cancellationToken)
            ?? throw new PlanningSequenceException("NOT_FOUND", "Planificación no encontrada.");

        var sessions = await _db.PlanningCalendarSessions
            .Where(s => s.PlanningId == planningId)
            .OrderBy(s => s.SessionNumber)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
            throw new PlanningSequenceException("NO_SESSIONS", "Genere el calendario antes de crear una secuencia.");

        var unitOas = await _db.UnidadObjetivos.AsNoTracking()
            .Where(u => u.UnidadId == plan.UnidadId)
            .Select(u => u.ObjetivoAprendizajeId)
            .ToListAsync(cancellationToken);

        foreach (var oa in request.Objectives)
        {
            if (!unitOas.Contains(oa.ObjectiveId))
                throw new PlanningSequenceException("OA_INVALID", $"El OA {oa.ObjectiveId} no pertenece a la unidad.");
        }

        var codes = await _db.ObjetivosAprendizaje.AsNoTracking()
            .Where(o => unitOas.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Codigo, cancellationToken);

        var indicators = await _db.IndicadoresEvaluacion.AsNoTracking()
            .Where(i => unitOas.Contains(i.ObjetivoAprendizajeId))
            .GroupBy(i => i.ObjetivoAprendizajeId)
            .ToDictionaryAsync(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(x => x.Id).ToList(), cancellationToken);

        var available = sessions.Where(s => s.Status != PlanningSessionStatus.Cancelled).ToList();
        if (request.RespectExistingClasses)
        {
            // Prefer unassigned sessions for new proposal; keep locked sessions
        }

        var result = _generator.Generate(available, request, codes, indicators);
        if (result.Deficit is not null)
        {
            return new PlanningSequenceProposalDto
            {
                PlanningId = planningId,
                Status = PlanningSequenceProposalStatus.Draft,
                Warnings = result.Warnings,
                Deficit = result.Deficit,
                Items = []
            };
        }

        foreach (var prev in await _db.PlanningSequenceProposals.Where(p => p.PlanningId == planningId && p.IsCurrent).ToListAsync(cancellationToken))
        {
            prev.IsCurrent = false;
            if (prev.Status is PlanningSequenceProposalStatus.Draft or PlanningSequenceProposalStatus.Validated)
                prev.Status = PlanningSequenceProposalStatus.Superseded;
        }

        var number = await _db.PlanningSequenceProposals.CountAsync(p => p.PlanningId == planningId, cancellationToken) + 1;
        var proposal = new PlanningSequenceProposal
        {
            Id = Guid.NewGuid(),
            PlanningId = planningId,
            ProposalNumber = number,
            Status = PlanningSequenceProposalStatus.Draft,
            GeneratedAt = _clock.UtcNow,
            IsCurrent = true,
            ConfigurationJson = JsonSerializer.Serialize(request, JsonOpts),
            SummaryJson = JsonSerializer.Serialize(new { itemCount = result.Items.Count }, JsonOpts),
            WarningJson = JsonSerializer.Serialize(result.Warnings, JsonOpts),
            PlanningVersionHash = await ComputePlanningHashAsync(planningId, cancellationToken)
        };
        _db.PlanningSequenceProposals.Add(proposal);

        foreach (var draft in result.Items)
        {
            var item = new PlanningSequenceProposalItem
            {
                Id = Guid.NewGuid(),
                PlanningSequenceProposalId = proposal.Id,
                Order = draft.Order,
                CalendarSessionId = draft.CalendarSessionId,
                ObjectiveLearningId = draft.ObjectiveId,
                BloomLevel = draft.BloomLevel,
                SuggestedTitle = draft.Title,
                SuggestedPurpose = draft.Purpose,
                SuggestedIndicatorIdsJson = JsonSerializer.Serialize(draft.IndicatorIds),
                ClassType = draft.ClassType,
                IsLocked = draft.IsLocked,
                WarningJson = JsonSerializer.Serialize(draft.Warnings)
            };
            _db.PlanningSequenceProposalItems.Add(item);
            var primary = true;
            foreach (var (indId, usage) in draft.IndicatorUsages)
            {
                _db.PlanningSequenceItemIndicators.Add(new PlanningSequenceItemIndicator
                {
                    PlanningSequenceProposalItemId = item.Id,
                    EvaluationIndicatorId = indId,
                    UsageType = usage,
                    IsPrimary = primary,
                    Weight = 1
                });
                primary = false;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("SequenceProposalGenerated planning={PlanningId} proposal={ProposalId}", planningId, proposal.Id);
        return (await GetProposalAsync(proposal.Id, cancellationToken))!;
    }

    public async Task<PlanningSequenceProposalDto?> GetCurrentProposalAsync(Guid planningId, CancellationToken cancellationToken = default)
    {
        var id = await _db.PlanningSequenceProposals.AsNoTracking()
            .Where(p => p.PlanningId == planningId && p.IsCurrent && !p.IsDeleted)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return id is null ? null : await GetProposalAsync(id.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<PlanningSequenceProposalDto>> ListProposalsAsync(Guid planningId, CancellationToken cancellationToken = default)
    {
        var ids = await _db.PlanningSequenceProposals.AsNoTracking()
            .Where(p => p.PlanningId == planningId && !p.IsDeleted)
            .OrderByDescending(p => p.ProposalNumber)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        var list = new List<PlanningSequenceProposalDto>();
        foreach (var id in ids)
        {
            var dto = await GetProposalAsync(id, cancellationToken);
            if (dto is not null) list.Add(dto);
        }
        return list;
    }

    public async Task<PlanningSequenceProposalDto?> GetProposalAsync(Guid proposalId, CancellationToken cancellationToken = default)
    {
        var proposal = await _db.PlanningSequenceProposals.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == proposalId && !p.IsDeleted, cancellationToken);
        if (proposal is null) return null;

        var items = await _db.PlanningSequenceProposalItems.AsNoTracking()
            .Where(i => i.PlanningSequenceProposalId == proposalId)
            .OrderBy(i => i.Order)
            .ToListAsync(cancellationToken);
        var itemIds = items.Select(i => i.Id).ToList();
        var indicators = await _db.PlanningSequenceItemIndicators.AsNoTracking()
            .Where(i => itemIds.Contains(i.PlanningSequenceProposalItemId))
            .ToListAsync(cancellationToken);
        var sessionIds = items.Select(i => i.CalendarSessionId).ToList();
        var sessions = await _db.PlanningCalendarSessions.AsNoTracking()
            .Where(s => sessionIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);
        var oaIds = items.Where(i => i.ObjectiveLearningId.HasValue).Select(i => i.ObjectiveLearningId!.Value).Distinct().ToList();
        var codes = await _db.ObjetivosAprendizaje.AsNoTracking()
            .Where(o => oaIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Codigo, cancellationToken);

        var currentHash = await ComputePlanningHashAsync(proposal.PlanningId, cancellationToken);
        var warnings = JsonSerializer.Deserialize<List<string>>(proposal.WarningJson) ?? [];

        return new PlanningSequenceProposalDto
        {
            Id = proposal.Id,
            PlanningId = proposal.PlanningId,
            ProposalNumber = proposal.ProposalNumber,
            Status = proposal.Status,
            GeneratedAt = proposal.GeneratedAt,
            ConfirmedAt = proposal.ConfirmedAt,
            IsCurrent = proposal.IsCurrent,
            IsOutdated = !string.Equals(proposal.PlanningVersionHash, currentHash, StringComparison.Ordinal),
            SummaryJson = proposal.SummaryJson,
            Warnings = warnings,
            Items = items.Select(i =>
            {
                sessions.TryGetValue(i.CalendarSessionId, out var session);
                var inds = indicators.Where(x => x.PlanningSequenceProposalItemId == i.Id).Select(x => x.EvaluationIndicatorId).ToList();
                var itemWarnings = JsonSerializer.Deserialize<List<string>>(i.WarningJson) ?? [];
                return new PlanningSequenceProposalItemDto
                {
                    Id = i.Id,
                    Order = i.Order,
                    CalendarSessionId = i.CalendarSessionId,
                    ScheduledDate = session?.ScheduledDate,
                    ObjectiveLearningId = i.ObjectiveLearningId,
                    ObjectiveCode = i.ObjectiveLearningId is Guid oid ? codes.GetValueOrDefault(oid) : null,
                    BloomLevel = i.BloomLevel,
                    SuggestedTitle = i.SuggestedTitle,
                    SuggestedPurpose = i.SuggestedPurpose,
                    ClassType = i.ClassType,
                    IsLocked = i.IsLocked,
                    WasManuallyModified = i.WasManuallyModified,
                    IndicatorIds = inds,
                    Warnings = itemWarnings
                };
            }).ToList()
        };
    }

    public async Task<PlanningSequenceProposalDto> UpdateProposalItemAsync(
        Guid proposalId,
        Guid itemId,
        UpdatePlanningSequenceItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var proposal = await _db.PlanningSequenceProposals.FirstOrDefaultAsync(p => p.Id == proposalId && !p.IsDeleted, cancellationToken)
            ?? throw new PlanningSequenceException("NOT_FOUND", "Propuesta no encontrada.");
        if (proposal.Status is PlanningSequenceProposalStatus.Confirmed or PlanningSequenceProposalStatus.Rejected)
            throw new PlanningSequenceException("READONLY", "La propuesta no se puede modificar.");

        var item = await _db.PlanningSequenceProposalItems
            .Include(i => i.Indicators)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.PlanningSequenceProposalId == proposalId, cancellationToken)
            ?? throw new PlanningSequenceException("NOT_FOUND", "Ítem no encontrado.");

        if (item.IsLocked)
            throw new PlanningSequenceException("LOCKED", "El ítem está bloqueado.");

        if (request.ObjectiveLearningId.HasValue) item.ObjectiveLearningId = request.ObjectiveLearningId;
        if (request.BloomLevel is not null) item.BloomLevel = NivelBloomHelper.Normalizar(request.BloomLevel) ?? item.BloomLevel;
        if (request.SuggestedTitle is not null) item.SuggestedTitle = request.SuggestedTitle;
        if (request.SuggestedPurpose is not null) item.SuggestedPurpose = request.SuggestedPurpose;
        if (request.ClassType.HasValue) item.ClassType = request.ClassType.Value;
        if (request.Order.HasValue) item.Order = request.Order.Value;
        if (request.IndicatorIds is not null)
        {
            _db.PlanningSequenceItemIndicators.RemoveRange(item.Indicators);
            var primary = true;
            foreach (var ind in request.IndicatorIds)
            {
                _db.PlanningSequenceItemIndicators.Add(new PlanningSequenceItemIndicator
                {
                    PlanningSequenceProposalItemId = item.Id,
                    EvaluationIndicatorId = ind,
                    UsageType = IndicatorUsageType.Practice,
                    IsPrimary = primary
                });
                primary = false;
            }
            item.SuggestedIndicatorIdsJson = JsonSerializer.Serialize(request.IndicatorIds);
        }

        item.WasManuallyModified = true;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("SequenceProposalModified proposal={ProposalId} item={ItemId}", proposalId, itemId);
        return (await GetProposalAsync(proposalId, cancellationToken))!;
    }

    public async Task<PlanningSequenceValidationDto> ValidateProposalAsync(Guid proposalId, CancellationToken cancellationToken = default)
    {
        var proposal = await _db.PlanningSequenceProposals
            .Include(p => p.Items).ThenInclude(i => i.Indicators)
            .FirstOrDefaultAsync(p => p.Id == proposalId && !p.IsDeleted, cancellationToken)
            ?? throw new PlanningSequenceException("NOT_FOUND", "Propuesta no encontrada.");

        var plan = await _db.Planificaciones.FirstAsync(p => p.Id == proposal.PlanningId, cancellationToken);
        var sessions = await _db.PlanningCalendarSessions.Where(s => s.PlanningId == proposal.PlanningId).ToListAsync(cancellationToken);
        var unitOas = await _db.UnidadObjetivos.AsNoTracking()
            .Where(u => u.UnidadId == plan.UnidadId).Select(u => u.ObjetivoAprendizajeId).ToListAsync(cancellationToken);
        var indicatorMap = await _db.IndicadoresEvaluacion.AsNoTracking()
            .ToDictionaryAsync(i => i.Id, i => i.ObjetivoAprendizajeId, cancellationToken);

        var currentHash = await ComputePlanningHashAsync(proposal.PlanningId, cancellationToken);
        var validation = _validator.Validate(proposal, sessions, unitOas.ToHashSet(), indicatorMap);
        if (!string.Equals(proposal.PlanningVersionHash, currentHash, StringComparison.Ordinal))
        {
            var errors = validation.Errors.ToList();
            errors.Add("La planificación cambió después de generar la propuesta. Regenere o reconcilie.");
            validation = new PlanningSequenceValidationDto
            {
                ProposalId = proposalId,
                IsValid = false,
                CanConfirm = false,
                Errors = errors,
                Warnings = validation.Warnings
            };
        }

        if (validation.IsValid && proposal.Status == PlanningSequenceProposalStatus.Draft)
        {
            proposal.Status = PlanningSequenceProposalStatus.Validated;
            await _db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("SequenceProposalValidated proposal={ProposalId} valid={Valid}", proposalId, validation.IsValid);
        return validation;
    }

    public async Task ConfirmProposalAsync(Guid proposalId, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateProposalAsync(proposalId, cancellationToken);
        if (!validation.CanConfirm)
            throw new PlanningSequenceException("INVALID", string.Join(" ", validation.Errors));

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var proposal = await _db.PlanningSequenceProposals
            .Include(p => p.Items).ThenInclude(i => i.Indicators)
            .FirstAsync(p => p.Id == proposalId, cancellationToken);

        var sessions = await _db.PlanningCalendarSessions
            .Where(s => s.PlanningId == proposal.PlanningId)
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var plan = await _db.Planificaciones.Include(p => p.Clases).FirstAsync(p => p.Id == proposal.PlanningId, cancellationToken);
        var nextNumero = plan.Clases.Count == 0 ? 1 : plan.Clases.Max(c => c.Numero) + 1;

        foreach (var item in proposal.Items.OrderBy(i => i.Order))
        {
            if (!sessions.TryGetValue(item.CalendarSessionId, out var session))
                continue;
            if (session.Status == PlanningSessionStatus.Cancelled)
                continue;
            if (session.IsLocked && session.ClassId is not null)
                continue;

            Clase clase;
            if (session.ClassId is Guid existingId)
            {
                clase = await _db.Clases.Include(c => c.Indicadores).FirstAsync(c => c.Id == existingId, cancellationToken);
                if (clase.Estado == EstadoClase.Realizada)
                    continue;
                _db.ClaseIndicadores.RemoveRange(clase.Indicadores);
            }
            else
            {
                if (item.ObjectiveLearningId is null)
                    throw new PlanningSequenceException("MISSING_OA", $"Ítem {item.Order} sin OA.");

                clase = new Clase
                {
                    Id = Guid.NewGuid(),
                    PlanificacionId = proposal.PlanningId,
                    Numero = nextNumero++,
                    Fecha = session.ScheduledDate,
                    ObjetivoAprendizajeId = item.ObjectiveLearningId.Value,
                    Estado = EstadoClase.Planificada
                };
                _db.Clases.Add(clase);
                session.ClassId = clase.Id;
            }

            if (item.ObjectiveLearningId.HasValue)
                clase.ObjetivoAprendizajeId = item.ObjectiveLearningId.Value;
            clase.NivelBloom = item.BloomLevel;
            clase.Titulo = item.SuggestedTitle;
            clase.Proposito = item.SuggestedPurpose;
            clase.ClassType = item.ClassType;
            clase.StartTime = session.StartTime;
            clase.DurationMinutes = session.DurationMinutes;
            clase.Fecha = session.ScheduledDate;

            foreach (var ind in item.Indicators)
            {
                _db.ClaseIndicadores.Add(new ClaseIndicadorEvaluacion
                {
                    ClaseId = clase.Id,
                    IndicadorEvaluacionId = ind.EvaluationIndicatorId
                });
            }

            session.Status = PlanningSessionStatus.Planned;
            session.UpdatedAt = _clock.UtcNow;
        }

        foreach (var prev in await _db.PlanningSequenceProposals
                     .Where(p => p.PlanningId == proposal.PlanningId && p.Id != proposal.Id && p.Status == PlanningSequenceProposalStatus.Confirmed)
                     .ToListAsync(cancellationToken))
        {
            prev.Status = PlanningSequenceProposalStatus.Superseded;
            prev.IsCurrent = false;
        }

        proposal.Status = PlanningSequenceProposalStatus.Confirmed;
        proposal.ConfirmedAt = _clock.UtcNow;
        proposal.IsCurrent = true;

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await _coverage.RecalculateAsync(proposal.PlanningId, cancellationToken);
        _logger.LogInformation("SequenceProposalConfirmed proposal={ProposalId}", proposalId);
    }

    public async Task RejectProposalAsync(Guid proposalId, string reason, CancellationToken cancellationToken = default)
    {
        var proposal = await _db.PlanningSequenceProposals.FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken)
            ?? throw new PlanningSequenceException("NOT_FOUND", "Propuesta no encontrada.");
        proposal.Status = PlanningSequenceProposalStatus.Rejected;
        proposal.IsCurrent = false;
        proposal.WarningJson = JsonSerializer.Serialize(new[] { reason }, JsonOpts);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteProposalAsync(Guid proposalId, CancellationToken cancellationToken = default)
    {
        var proposal = await _db.PlanningSequenceProposals.FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken)
            ?? throw new PlanningSequenceException("NOT_FOUND", "Propuesta no encontrada.");
        proposal.IsDeleted = true;
        proposal.IsCurrent = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> ComputePlanningHashAsync(Guid planningId, CancellationToken cancellationToken)
    {
        var plan = await _db.Planificaciones.AsNoTracking().FirstAsync(p => p.Id == planningId, cancellationToken);
        var sessionSig = await _db.PlanningCalendarSessions.AsNoTracking()
            .Where(s => s.PlanningId == planningId)
            .OrderBy(s => s.SessionNumber)
            .Select(s => $"{s.Id}:{s.ScheduledDate}:{s.Status}:{s.IsLocked}")
            .ToListAsync(cancellationToken);
        var raw = $"{plan.Id}|{plan.FechaInicio}|{plan.FechaFin}|{plan.UnidadId}|{string.Join(';', sessionSig)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }
}
