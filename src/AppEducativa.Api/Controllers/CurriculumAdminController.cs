using System.Security.Claims;
using AppEducativa.Api.Data;
using AppEducativa.Api.Models.Curriculum;
using AppEducativa.Api.Services.Curriculum;
using AppEducativa.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api.Controllers;

[ApiController]
[Route("api/admin/curriculum")]
[Authorize(Policy = "CurriculumAdmin")]
public class CurriculumAdminController : ControllerBase
{
    private readonly AppEducativaDbContext _db;
    private readonly OfficialCurriculumImportOrchestrator _orchestrator;
    private readonly ICurriculumReviewService _review;

    public CurriculumAdminController(
        AppEducativaDbContext db,
        OfficialCurriculumImportOrchestrator orchestrator,
        ICurriculumReviewService review) =>
        (_db, _orchestrator, _review) = (db, orchestrator, review);

    [HttpGet("sources")]
    public async Task<ActionResult<IEnumerable<CurriculumAdminSourceDto>>> Sources(CancellationToken ct) =>
        Ok(await _db.CurriculumSources.AsNoTracking().OrderBy(s => s.Nombre).Select(s => new CurriculumAdminSourceDto
        {
            Id = s.Id,
            ExternalId = s.ExternalId,
            Nombre = s.Nombre,
            Url = s.Url,
            Dominio = s.Dominio,
            TipoFuente = s.TipoFuente.ToString(),
            Formato = s.Formato.ToString(),
            NivelEsperado = s.NivelEsperado,
            AsignaturaEsperada = s.AsignaturaEsperada,
            Activo = s.Activo
        }).ToListAsync(ct));

    [HttpPost("sources/reload")]
    public async Task<IActionResult> ReloadSources(CancellationToken ct)
    {
        await _orchestrator.ReloadSourcesAsync(ct);
        return NoContent();
    }

    [HttpPost("imports")]
    public async Task<ActionResult<ImportSummaryDto>> CreateImport([FromBody] CreateImportRequest request, CancellationToken ct)
    {
        CurriculumImportBatch batch;
        var key = request.SourceExternalId?.Trim();
        if (string.IsNullOrWhiteSpace(key))
            key = request.SourceId?.Trim();

        if (string.IsNullOrWhiteSpace(key))
            return BadRequest(new { error = "Indique sourceId (Guid o id externo, ej. matematica-4-basico-programa) o sourceExternalId." });

        if (Guid.TryParse(key, out var sourceGuid) && sourceGuid != Guid.Empty)
            batch = await _orchestrator.CreateImportAsync(sourceGuid, ct);
        else
            batch = await _orchestrator.CreateImportByExternalIdAsync(key, ct);

        return CreatedAtAction(nameof(GetImport), new { id = batch.Id }, Summary(batch));
    }

    [HttpGet("imports")]
    public async Task<ActionResult<IEnumerable<CurriculumAdminBatchDto>>> Imports(CancellationToken ct) =>
        Ok(await _db.CurriculumImportBatches.AsNoTracking().OrderByDescending(b => b.FechaInicio).Take(100)
            .Select(b => new CurriculumAdminBatchDto
            {
                Id = b.Id,
                FechaInicio = b.FechaInicio,
                FechaTermino = b.FechaTermino,
                Estado = b.Status.ToString(),
                CantidadUnidades = b.CantidadUnidades,
                CantidadOA = b.CantidadOA,
                CantidadIndicadores = b.CantidadIndicadores,
                CantidadAdvertencias = b.CantidadAdvertencias,
                CantidadErrores = b.CantidadErrores,
                SourceExternalId = b.SourceExternalId,
                Mensaje = $"{b.CantidadUnidades} u · {b.CantidadOA} OA · {b.CantidadErrores} err"
            }).ToListAsync(ct));

    [HttpGet("imports/{id:guid}")]
    public async Task<ActionResult<ImportSummaryDto>> GetImport(Guid id, CancellationToken ct)
    {
        var batch = await _db.CurriculumImportBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
        return batch is null ? NotFound() : Ok(Summary(batch));
    }

    [HttpPost("imports/{id:guid}/download")]
    public Task<ActionResult<ImportSummaryDto>> Download(Guid id, CancellationToken ct) => Execute(() => _orchestrator.DownloadAsync(id, ct));
    [HttpPost("imports/{id:guid}/extract")]
    public Task<ActionResult<ImportSummaryDto>> Extract(Guid id, CancellationToken ct) => Execute(() => _orchestrator.ExtractAsync(id, ct));
    [HttpPost("imports/{id:guid}/validate")]
    public Task<ActionResult<ImportSummaryDto>> Validate(Guid id, CancellationToken ct) => Execute(() => _orchestrator.ValidateAsync(id, ct));
    [HttpPost("imports/{id:guid}/process")]
    public Task<ActionResult<ImportSummaryDto>> Process(Guid id, CancellationToken ct) => Execute(() => _orchestrator.ProcessAsync(id, ct));

    [HttpGet("imports/{id:guid}/preview")]
    public async Task<ActionResult<CurriculumImportPreviewDto>> Preview(Guid id, CancellationToken ct) =>
        await Execute(() => _orchestrator.GetPreviewAsync(id, ct));

    [HttpPut("imports/{id:guid}/preview")]
    public async Task<ActionResult<CurriculumImportPreviewDto>> UpdatePreview(Guid id, CurriculumImportPreviewDto preview, CancellationToken ct) =>
        await Execute(() => _orchestrator.UpdatePreviewAsync(id, preview, UserName(), ct));

    [HttpGet("imports/{id:guid}/issues")]
    public async Task<ActionResult<IReadOnlyList<ValidationIssueDto>>> Issues(Guid id, CancellationToken ct) =>
        await Execute(() => _orchestrator.GetIssuesAsync(id, ct));

    [HttpGet("imports/{id:guid}/diff")]
    public async Task<IActionResult> Diff(Guid id, CancellationToken ct)
    {
        var diff = await _db.CurriculumImportBatches.AsNoTracking().Where(b => b.Id == id).Select(b => b.DiffJson).FirstOrDefaultAsync(ct);
        return diff is null ? NotFound() : Content(diff, "application/json");
    }

    [HttpPost("imports/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        try
        {
            await _review.ApproveFromReviewAsync(id, UserName(), ct);
            return NoContent();
        }
        catch (CurriculumReviewException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost("imports/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectReviewRequest? request, CancellationToken ct)
    {
        try
        {
            var reason = request?.Reason;
            if (string.IsNullOrWhiteSpace(reason))
                return BadRequest(new { error = "Debe indicar un motivo de rechazo." });
            await _review.RejectFromReviewAsync(id, reason, UserName(), ct);
            return NoContent();
        }
        catch (CurriculumReviewException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("imports/{id:guid}/import")]
    public async Task<ActionResult> Import(Guid id, CancellationToken ct)
    {
        try { return Ok(await _orchestrator.ImportAsync(id, ct)); }
        catch (KeyNotFoundException) { return NotFound(); } catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost("imports/{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        try
        {
            await _review.PublishAsync(id, UserName(), ct);
            return NoContent();
        }
        catch (CurriculumReviewException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    private async Task<ActionResult<ImportSummaryDto>> Execute(Func<Task<Models.Curriculum.CurriculumImportBatch>> action)
    {
        try { return Ok(Summary(await action())); }
        catch (KeyNotFoundException) { return NotFound(); } catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }
    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (KeyNotFoundException) { return NotFound(); } catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }
    private string? UserName() => User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
    private static ImportSummaryDto Summary(Models.Curriculum.CurriculumImportBatch b) => new()
    {
        BatchId = b.Id, Status = b.Status.ToString(), Units = b.CantidadUnidades, Objectives = b.CantidadOA,
        Indicators = b.CantidadIndicadores, Skills = b.CantidadHabilidades, Attitudes = b.CantidadActitudes,
        Warnings = b.CantidadAdvertencias, Errors = b.CantidadErrores
    };

    public sealed class CreateImportRequest
    {
        /// <summary>Guid de CurriculumSource o id externo (ej. matematica-4-basico-programa).</summary>
        public string? SourceId { get; set; }
        public string? SourceExternalId { get; set; }
    }
}

