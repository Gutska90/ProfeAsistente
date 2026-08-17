using ProfeAsistente.Api.Services.Coverage;
using ProfeAsistente.Api.Services.PlanningCalendar;
using ProfeAsistente.Api.Services.PlanningSequence;
using ProfeAsistente.Api.Services.PlanningSuggestions;
using ProfeAsistente.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProfeAsistente.Api.Controllers;

[ApiController]
[Authorize]
public class PlanningCalendarController : ControllerBase
{
    private readonly IPlanningCalendarService _calendar;
    private readonly IPlanningSequenceService _sequence;
    private readonly ICurriculumCoverageService _coverage;
    private readonly IPlanningSuggestionService _suggestions;

    public PlanningCalendarController(
        IPlanningCalendarService calendar,
        IPlanningSequenceService sequence,
        ICurriculumCoverageService coverage,
        IPlanningSuggestionService suggestions)
    {
        _calendar = calendar;
        _sequence = sequence;
        _coverage = coverage;
        _suggestions = suggestions;
    }

    [HttpGet("api/planificaciones/{planningId:guid}/calendario")]
    public async Task<ActionResult<PlanningCalendarDto>> GetCalendar(Guid planningId, CancellationToken ct)
    {
        var cal = await _calendar.GetCalendarAsync(planningId, ct);
        return cal is null ? NotFound() : Ok(cal);
    }

    [HttpPut("api/planificaciones/{planningId:guid}/calendario/configuracion")]
    public async Task<ActionResult<PlanningCalendarDto>> Configure(Guid planningId, [FromBody] ConfigurePlanningScheduleRequest request, CancellationToken ct)
    {
        try { return Ok(await _calendar.ConfigureAsync(planningId, request, ct)); }
        catch (PlanningCalendarException ex) { return Problem(ex); }
    }

    [HttpPost("api/planificaciones/{planningId:guid}/calendario/vista-previa")]
    public async Task<ActionResult<PlanningCalendarDto>> Preview(Guid planningId, CancellationToken ct)
    {
        try { return Ok(await _calendar.PreviewRegenerationAsync(planningId, ct)); }
        catch (PlanningCalendarException ex) { return Problem(ex); }
    }

    [HttpPost("api/planificaciones/{planningId:guid}/calendario/generar")]
    public async Task<ActionResult<PlanningCalendarDto>> Generate(Guid planningId, [FromBody] GenerateCalendarSessionsRequest? request, CancellationToken ct)
    {
        try { return Ok(await _calendar.GenerateSessionsAsync(planningId, request ?? new GenerateCalendarSessionsRequest(), ct)); }
        catch (PlanningCalendarException ex) { return Problem(ex); }
    }

    [HttpPost("api/planificaciones/{planningId:guid}/calendario/regenerar")]
    public async Task<ActionResult<PlanningCalendarDto>> Regenerate(Guid planningId, [FromBody] GenerateCalendarSessionsRequest? request, CancellationToken ct)
    {
        try
        {
            var req = request ?? new GenerateCalendarSessionsRequest();
            return Ok(await _calendar.GenerateSessionsAsync(planningId, new GenerateCalendarSessionsRequest
            {
                PreviewOnly = req.PreviewOnly,
                ConfirmDestructiveChanges = req.ConfirmDestructiveChanges,
                PreserveManualSessions = req.PreserveManualSessions,
                PreserveLockedSessions = req.PreserveLockedSessions
            }, ct));
        }
        catch (PlanningCalendarException ex) { return Problem(ex); }
    }

    [HttpPost("api/planificaciones/{planningId:guid}/calendario/sesiones")]
    public async Task<ActionResult<PlanningCalendarSessionDto>> AddSession(Guid planningId, [FromBody] CreateManualSessionRequest request, CancellationToken ct)
    {
        try { return Ok(await _calendar.AddManualSessionAsync(planningId, request, ct)); }
        catch (PlanningCalendarException ex) { return Problem(ex); }
    }

    [HttpPost("api/planificaciones/{planningId:guid}/calendario/exclusiones/importar")]
    public async Task<IActionResult> ImportExcluded(Guid planningId, [FromBody] ImportExcludedDatesRequest request, CancellationToken ct)
    {
        try
        {
            await _calendar.ImportExcludedDatesAsync(planningId, request, ct);
            return NoContent();
        }
        catch (PlanningCalendarException ex) { return Problem(ex); }
    }

    [HttpGet("api/planificaciones/{planningId:guid}/calendario/conflictos")]
    public async Task<ActionResult<IReadOnlyList<string>>> Conflicts(Guid planningId, CancellationToken ct)
        => Ok(await _calendar.GetConflictsAsync(planningId, ct));

    [HttpPut("api/calendario/sesiones/{sessionId:guid}")]
    public async Task<IActionResult> UpdateSession(Guid sessionId, [FromBody] CreateManualSessionRequest request, CancellationToken ct)
    {
        try
        {
            await _calendar.UpdateSessionAsync(sessionId, request, ct);
            return NoContent();
        }
        catch (PlanningCalendarException ex) { return Problem(ex); }
    }

    [HttpPost("api/calendario/sesiones/{sessionId:guid}/reprogramar")]
    public async Task<ActionResult<PlanningCalendarSessionDto>> Reschedule(Guid sessionId, [FromBody] RescheduleSessionRequest request, CancellationToken ct)
    {
        try { return Ok(await _calendar.RescheduleSessionAsync(sessionId, request, ct)); }
        catch (PlanningCalendarException ex) { return Problem(ex); }
    }

    [HttpPost("api/calendario/sesiones/{sessionId:guid}/cancelar")]
    public async Task<IActionResult> Cancel(Guid sessionId, [FromBody] CancelPlanningSessionRequest request, CancellationToken ct)
    {
        try
        {
            await _calendar.CancelSessionAsync(sessionId, request, ct);
            return NoContent();
        }
        catch (PlanningCalendarException ex) { return Problem(ex); }
    }

    [HttpPost("api/calendario/sesiones/{sessionId:guid}/restaurar")]
    public async Task<IActionResult> Restore(Guid sessionId, CancellationToken ct)
    {
        try
        {
            await _calendar.RestoreSessionAsync(sessionId, ct);
            return NoContent();
        }
        catch (PlanningCalendarException ex) { return Problem(ex); }
    }

    [HttpPost("api/calendario/sesiones/{sessionId:guid}/bloquear")]
    public async Task<IActionResult> Lock(Guid sessionId, [FromBody] LockSessionRequest? request, CancellationToken ct)
    {
        try
        {
            await _calendar.LockSessionAsync(sessionId, request ?? new LockSessionRequest(), ct);
            return NoContent();
        }
        catch (PlanningCalendarException ex) { return Problem(ex); }
    }

    [HttpPost("api/calendario/sesiones/{sessionId:guid}/desbloquear")]
    public async Task<IActionResult> Unlock(Guid sessionId, CancellationToken ct)
    {
        try
        {
            await _calendar.UnlockSessionAsync(sessionId, ct);
            return NoContent();
        }
        catch (PlanningCalendarException ex) { return Problem(ex); }
    }

    // Sequence
    [HttpPost("api/planificaciones/{planningId:guid}/secuencia/propuestas")]
    public async Task<ActionResult<PlanningSequenceProposalDto>> GenerateProposal(Guid planningId, [FromBody] GeneratePlanningSequenceRequest request, CancellationToken ct)
    {
        try { return Ok(await _sequence.GenerateProposalAsync(planningId, request, ct)); }
        catch (PlanningSequenceException ex) { return Problem(ex.Message, statusCode: 400, title: ex.ErrorCode); }
    }

    [HttpGet("api/planificaciones/{planningId:guid}/secuencia/propuestas")]
    public async Task<ActionResult<IReadOnlyList<PlanningSequenceProposalDto>>> ListProposals(Guid planningId, CancellationToken ct)
        => Ok(await _sequence.ListProposalsAsync(planningId, ct));

    [HttpGet("api/secuencia/propuestas/{proposalId:guid}")]
    public async Task<ActionResult<PlanningSequenceProposalDto>> GetProposal(Guid proposalId, CancellationToken ct)
    {
        var p = await _sequence.GetProposalAsync(proposalId, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPut("api/secuencia/propuestas/{proposalId:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<PlanningSequenceProposalDto>> UpdateItem(Guid proposalId, Guid itemId, [FromBody] UpdatePlanningSequenceItemRequest request, CancellationToken ct)
    {
        try { return Ok(await _sequence.UpdateProposalItemAsync(proposalId, itemId, request, ct)); }
        catch (PlanningSequenceException ex) { return Problem(ex.Message, statusCode: 400, title: ex.ErrorCode); }
    }

    [HttpPost("api/secuencia/propuestas/{proposalId:guid}/validar")]
    public async Task<ActionResult<PlanningSequenceValidationDto>> Validate(Guid proposalId, CancellationToken ct)
    {
        try { return Ok(await _sequence.ValidateProposalAsync(proposalId, ct)); }
        catch (PlanningSequenceException ex) { return Problem(ex.Message, statusCode: 400, title: ex.ErrorCode); }
    }

    [HttpPost("api/secuencia/propuestas/{proposalId:guid}/confirmar")]
    public async Task<IActionResult> Confirm(Guid proposalId, CancellationToken ct)
    {
        try
        {
            await _sequence.ConfirmProposalAsync(proposalId, ct);
            return NoContent();
        }
        catch (PlanningSequenceException ex) { return Problem(ex.Message, statusCode: 400, title: ex.ErrorCode); }
    }

    [HttpPost("api/secuencia/propuestas/{proposalId:guid}/rechazar")]
    public async Task<IActionResult> Reject(Guid proposalId, [FromBody] RejectSequenceProposalRequest request, CancellationToken ct)
    {
        try
        {
            await _sequence.RejectProposalAsync(proposalId, request.Reason, ct);
            return NoContent();
        }
        catch (PlanningSequenceException ex) { return Problem(ex.Message, statusCode: 400, title: ex.ErrorCode); }
    }

    [HttpDelete("api/secuencia/propuestas/{proposalId:guid}")]
    public async Task<IActionResult> DeleteProposal(Guid proposalId, CancellationToken ct)
    {
        try
        {
            await _sequence.DeleteProposalAsync(proposalId, ct);
            return NoContent();
        }
        catch (PlanningSequenceException ex) { return Problem(ex.Message, statusCode: 400, title: ex.ErrorCode); }
    }

    // Coverage
    [HttpGet("api/planificaciones/{planningId:guid}/cobertura")]
    public async Task<ActionResult<PlanningCoverageDto>> Coverage(Guid planningId, [FromQuery] string mode = "Planned", CancellationToken ct = default)
        => Ok(await _coverage.GetCoverageAsync(planningId, mode, ct));

    [HttpGet("api/planificaciones/{planningId:guid}/cobertura/objetivos")]
    public async Task<ActionResult<IReadOnlyList<ObjectiveCoverageDto>>> CoverageObjectives(Guid planningId, CancellationToken ct)
        => Ok((await _coverage.GetCoverageAsync(planningId, "Planned", ct)).Objectives);

    [HttpGet("api/planificaciones/{planningId:guid}/cobertura/indicadores")]
    public async Task<ActionResult<IReadOnlyList<IndicatorCoverageDto>>> CoverageIndicators(Guid planningId, CancellationToken ct)
        => Ok((await _coverage.GetCoverageAsync(planningId, "Planned", ct)).Indicators);

    [HttpGet("api/planificaciones/{planningId:guid}/cobertura/bloom")]
    public async Task<ActionResult<IReadOnlyList<BloomDistributionDto>>> CoverageBloom(Guid planningId, CancellationToken ct)
        => Ok((await _coverage.GetCoverageAsync(planningId, "Planned", ct)).BloomDistribution);

    [HttpGet("api/planificaciones/{planningId:guid}/cobertura/matriz")]
    public async Task<ActionResult<CoverageMatrixDto?>> CoverageMatrix(Guid planningId, CancellationToken ct)
        => Ok((await _coverage.GetCoverageAsync(planningId, "Planned", ct)).Matrix);

    [HttpGet("api/planificaciones/{planningId:guid}/cobertura/planificada")]
    public async Task<ActionResult<PlanningCoverageDto>> CoveragePlanned(Guid planningId, CancellationToken ct)
        => Ok(await _coverage.GetCoverageAsync(planningId, "Planned", ct));

    [HttpGet("api/planificaciones/{planningId:guid}/cobertura/ejecutada")]
    public async Task<ActionResult<PlanningCoverageDto>> CoverageExecuted(Guid planningId, CancellationToken ct)
        => Ok(await _coverage.GetCoverageAsync(planningId, "Executed", ct));

    [HttpPost("api/planificaciones/{planningId:guid}/cobertura/recalcular")]
    public async Task<ActionResult<PlanningCoverageDto>> Recalculate(Guid planningId, CancellationToken ct)
        => Ok(await _coverage.RecalculateAsync(planningId, ct));

    [HttpGet("api/planificaciones/{planningId:guid}/alertas")]
    public async Task<ActionResult<IReadOnlyList<PlanningAlertDto>>> Alerts(Guid planningId, [FromQuery] bool includeResolved = false, CancellationToken ct = default)
        => Ok(await _coverage.GetAlertsAsync(planningId, includeResolved, ct));

    [HttpGet("api/planificaciones/{planningId:guid}/sugerencias")]
    public async Task<ActionResult<IReadOnlyList<PlanningSuggestionDto>>> Suggestions(Guid planningId, CancellationToken ct)
        => Ok(await _suggestions.GetSuggestionsAsync(planningId, ct));

    [HttpPost("api/planificaciones/{planningId:guid}/sugerencias/{suggestionId:guid}/aplicar")]
    public async Task<IActionResult> ApplySuggestion(Guid planningId, Guid suggestionId, CancellationToken ct)
    {
        await _suggestions.ApplyAsync(planningId, suggestionId, ct);
        return NoContent();
    }

    [HttpPost("api/planificaciones/{planningId:guid}/sugerencias/{suggestionId:guid}/ignorar")]
    public async Task<IActionResult> IgnoreSuggestion(Guid planningId, Guid suggestionId, CancellationToken ct)
    {
        await _suggestions.IgnoreAsync(planningId, suggestionId, ct);
        return NoContent();
    }

    [HttpPost("api/clases/{classId:guid}/completar")]
    public async Task<IActionResult> CompleteClass(Guid classId, [FromBody] CompleteClassRequest request, CancellationToken ct)
    {
        await _coverage.CompleteClassAsync(classId, request, ct);
        return NoContent();
    }

    private ObjectResult Problem(PlanningCalendarException ex) =>
        Problem(detail: ex.Message, statusCode: MapStatus(ex.ErrorCode), title: ex.ErrorCode);

    private static int MapStatus(string code) => code switch
    {
        "NOT_FOUND" => 404,
        "LOCKED" or "CONFLICT" => 409,
        _ => 400
    };
}
