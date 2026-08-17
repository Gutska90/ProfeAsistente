using ProfeAsistente.Api.Services.Pilot;
using ProfeAsistente.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProfeAsistente.Api.Controllers;

[ApiController]
[Route("api/pilot")]
[Authorize]
public sealed class PilotController : ControllerBase
{
    private readonly IPilotMetricsService _pilot;

    public PilotController(IPilotMetricsService pilot) => _pilot = pilot;

    /// <summary>Resumen de métricas del piloto (export %, feedback, evidencia, IA, autoreporte).</summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<PilotMetricsDto>> Metrics(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct)
        => Ok(await _pilot.GetMetricsAsync(fromUtc, toUtc, ct));

    [HttpPost("session-reports")]
    [ProducesResponseType(typeof(PilotSessionReportDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitReport(
        [FromBody] SubmitPilotSessionReportRequest request, CancellationToken ct)
    {
        try
        {
            var row = await _pilot.SubmitSessionReportAsync(request, ct);
            return CreatedAtAction(nameof(ListReports), new { take = 1 }, row);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { errorCode = "InvalidMinutes", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { errorCode = "Unauthorized", message = ex.Message });
        }
    }

    [HttpGet("session-reports")]
    public async Task<ActionResult<IReadOnlyList<PilotSessionReportDto>>> ListReports(
        [FromQuery] int take = 50, CancellationToken ct = default)
        => Ok(await _pilot.ListSessionReportsAsync(take, ct));
}
