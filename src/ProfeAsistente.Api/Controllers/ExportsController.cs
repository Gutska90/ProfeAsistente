using ProfeAsistente.Api.Services.Authorization;
using ProfeAsistente.Api.Services.Export;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using ProfeAsistente.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProfeAsistente.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.CanExportMaterials)]
public class ExportsController : ControllerBase
{
    private readonly IWordExportService _exports;
    private readonly IExportCleanupService _cleanup;
    private readonly IResourceAuthorizationService _authz;

    public ExportsController(IWordExportService exports, IExportCleanupService cleanup, IResourceAuthorizationService authz)
    {
        _exports = exports;
        _cleanup = cleanup;
        _authz = authz;
    }

    [HttpPost("api/exports")]
    [ProducesResponseType(typeof(ExportResultDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateExportRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _exports.ExportAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { exportId = result.ExportId }, result);
        }
        catch (WordExportException ex) { return ToProblem(ex); }
    }

    [HttpGet("api/exports")]
    public async Task<IActionResult> List([FromQuery] int take = 50, CancellationToken ct = default)
    {
        try { return Ok(await _exports.ListAsync(take, ct)); }
        catch (WordExportException ex) { return ToProblem(ex); }
    }

    [HttpGet("api/exports/{exportId:guid}")]
    public async Task<IActionResult> Get(Guid exportId, CancellationToken ct)
    {
        try
        {
            var result = await _exports.GetAsync(exportId, ct);
            return result is null ? NotFound(Problem(404, "ExportNotFound", "Exportación no encontrada.")) : Ok(result);
        }
        catch (WordExportException ex) { return ToProblem(ex); }
    }

    [HttpGet("api/exports/{exportId:guid}/download")]
    public async Task<IActionResult> Download(Guid exportId, CancellationToken ct)
    {
        try
        {
            await _authz.EnsureCanAccessExportAsync(exportId, ct);
            var (stream, fileName, contentType) = await _exports.OpenDownloadAsync(exportId, ct);
            return File(stream, contentType, fileName);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (WordExportException ex) { return ToProblem(ex); }
    }

    [HttpDelete("api/exports/{exportId:guid}")]
    public async Task<IActionResult> Delete(Guid exportId, CancellationToken ct)
    {
        try
        {
            await _exports.SoftDeleteAsync(exportId, ct);
            return NoContent();
        }
        catch (WordExportException ex) { return ToProblem(ex); }
    }

    [HttpPost("api/planificaciones/{planningId:guid}/export")]
    public async Task<IActionResult> ExportPlanning(Guid planningId, [FromBody] CreateExportRequest? request, CancellationToken ct)
    {
        try
        {
            request ??= new CreateExportRequest();
            request.DocumentType = ExportDocumentType.Planning;
            request.Audience = request.Audience == default ? ExportAudience.Administrative : request.Audience;
            var result = await _exports.ExportPlanningAsync(planningId, request, ct);
            return CreatedAtAction(nameof(Get), new { exportId = result.ExportId }, result);
        }
        catch (WordExportException ex) { return ToProblem(ex); }
    }

    [HttpPost("api/planificaciones/{planningId:guid}/export-package")]
    public async Task<IActionResult> ExportPackage(Guid planningId, [FromBody] CreateExportRequest? request, CancellationToken ct)
    {
        try
        {
            request ??= new CreateExportRequest();
            var result = await _exports.ExportPlanningPackageAsync(planningId, request, ct);
            return CreatedAtAction(nameof(Get), new { exportId = result.ExportId }, result);
        }
        catch (WordExportException ex) { return ToProblem(ex); }
    }

    [HttpPost("api/clases/{classId:guid}/export")]
    public async Task<IActionResult> ExportClass(Guid classId, [FromBody] CreateExportRequest? request, CancellationToken ct)
    {
        try
        {
            request ??= new CreateExportRequest();
            var result = await _exports.ExportClassAsync(classId, request, ct);
            return CreatedAtAction(nameof(Get), new { exportId = result.ExportId }, result);
        }
        catch (WordExportException ex) { return ToProblem(ex); }
    }

    [HttpPost("api/educational-documents/{documentId:guid}/export")]
    public async Task<IActionResult> ExportDocument(Guid documentId, [FromBody] CreateExportRequest? request, CancellationToken ct)
    {
        try
        {
            request ??= new CreateExportRequest { DocumentType = ExportDocumentType.Assessment, Audience = ExportAudience.Student };
            if (request.DocumentType is not (ExportDocumentType.LearningGuide or ExportDocumentType.Exercises or ExportDocumentType.Assessment))
            {
                // Infer from stored document later; default Assessment is fine if client set type
            }
            request.EducationalDocumentId = documentId;
            // Load type if generic
            var result = await _exports.ExportEducationalDocumentAsync(documentId, request, ct);
            return CreatedAtAction(nameof(Get), new { exportId = result.ExportId }, result);
        }
        catch (WordExportException ex) { return ToProblem(ex); }
    }

    [HttpPost("api/educational-documents/{documentId:guid}/export-answer-key")]
    public async Task<IActionResult> ExportAnswerKey(Guid documentId, [FromBody] CreateExportRequest? request, CancellationToken ct)
    {
        try
        {
            request ??= new CreateExportRequest();
            request.DocumentType = ExportDocumentType.AnswerKey;
            request.Audience = ExportAudience.Teacher;
            request.EducationalDocumentId = documentId;
            var result = await _exports.ExportAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { exportId = result.ExportId }, result);
        }
        catch (WordExportException ex) { return ToProblem(ex); }
    }

    [HttpPost("api/educational-documents/{documentId:guid}/export-specification-table")]
    public async Task<IActionResult> ExportSpec(Guid documentId, [FromBody] CreateExportRequest? request, CancellationToken ct)
    {
        try
        {
            request ??= new CreateExportRequest();
            request.DocumentType = ExportDocumentType.SpecificationTable;
            request.Audience = ExportAudience.Administrative;
            request.EducationalDocumentId = documentId;
            var result = await _exports.ExportAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { exportId = result.ExportId }, result);
        }
        catch (WordExportException ex) { return ToProblem(ex); }
    }

    [HttpPost("api/admin/exports/cleanup")]
    public async Task<IActionResult> Cleanup(CancellationToken ct)
    {
        try { return Ok(await _cleanup.CleanupAsync(ct)); }
        catch (WordExportException ex) { return ToProblem(ex); }
    }

    [HttpGet("api/admin/exports/storage-summary")]
    public async Task<IActionResult> StorageSummary(CancellationToken ct)
    {
        try { return Ok(await _cleanup.GetStorageSummaryAsync(ct)); }
        catch (WordExportException ex) { return ToProblem(ex); }
    }

    private IActionResult ToProblem(WordExportException ex) =>
        StatusCode(ex.StatusCode, Problem(ex.StatusCode, ex.ErrorCode, ex.Message));

    private object Problem(int status, string error, string message) => new
    {
        status,
        error,
        message,
        traceId = HttpContext.TraceIdentifier
    };
}
