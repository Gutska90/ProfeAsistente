using ProfeAsistente.Api.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProfeAsistente.Api.Controllers;

[ApiController]
[Route("api/ai-usage")]
[Authorize]
public sealed class AiUsageController : ControllerBase
{
    private readonly IAiUsageTracker _tracker;

    public AiUsageController(IAiUsageTracker tracker) => _tracker = tracker;

    /// <summary>Resumen de uso/costo estimado de IA en un periodo (default últimos 30 días).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct)
        => Ok(await _tracker.GetSummaryAsync(fromUtc, toUtc, ct));

    /// <summary>Últimas generaciones registradas (tokens, latencia, costo estimado, prompt).</summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int take = 50, CancellationToken ct = default)
        => Ok(await _tracker.GetRecentAsync(take, ct));
}
