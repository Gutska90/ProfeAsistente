using ProfeAsistente.Api.Services.AI.DocumentGeneration;
using ProfeAsistente.Api.Services.AI.Gemini;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using ProfeAsistente.Shared.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProfeAsistente.Api.Controllers;

[ApiController]
[Authorize]
public class EducationalDocumentController : ControllerBase
{
    private readonly IEducationalDocumentGenerationService _service;

    public EducationalDocumentController(IEducationalDocumentGenerationService service)
        => _service = service;

    [HttpPost("api/clases/{classId:guid}/educational-documents/generate")]
    [ProducesResponseType(typeof(EducationalDocumentGenerationResultDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Generate(Guid classId, [FromBody] GenerateEducationalDocumentRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _service.GenerateAsync(classId, request, ct);
            return CreatedAtAction(nameof(GetById), new { documentId = result.DocumentId }, result);
        }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        {
            return ToProblem(ex);
        }
    }

    [HttpGet("api/clases/{classId:guid}/educational-documents")]
    public async Task<IActionResult> List(Guid classId, CancellationToken ct)
    {
        try { return Ok(await _service.ListByClassAsync(classId, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    /// <summary>Biblioteca docente: materiales de las planificaciones del usuario.</summary>
    [HttpGet("api/biblioteca/materiales")]
    public async Task<IActionResult> Library(
        [FromQuery] Guid? courseId,
        [FromQuery] string? type,
        [FromQuery] string? q,
        [FromQuery] bool templatesOnly = false,
        CancellationToken ct = default)
    {
        try
        {
            EducationalDocumentType? parsed = null;
            if (!string.IsNullOrWhiteSpace(type))
            {
                if (Enum.TryParse<EducationalDocumentType>(type, true, out var t))
                    parsed = t;
                else
                    parsed = MaterialUiLabels.ParseTypeLabel(type);
            }

            return Ok(await _service.ListLibraryAsync(courseId, parsed, q, templatesOnly, ct));
        }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpGet("api/educational-documents/{documentId:guid}")]
    public async Task<IActionResult> GetById(Guid documentId, CancellationToken ct)
    {
        try
        {
            var doc = await _service.GetAsync(documentId, ct);
            return doc is null ? NotFound(Problem(404, "DocumentNotFound", "Documento no encontrado.")) : Ok(doc);
        }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpGet("api/educational-documents/{documentId:guid}/student-view")]
    public async Task<IActionResult> GetStudentView(Guid documentId, CancellationToken ct)
    {
        try
        {
            var doc = await _service.GetStudentViewAsync(documentId, ct);
            return doc is null ? NotFound(Problem(404, "DocumentNotFound", "Documento no encontrado.")) : Ok(doc);
        }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpGet("api/educational-documents/{documentId:guid}/items")]
    public async Task<IActionResult> GetItems(Guid documentId, CancellationToken ct)
    {
        try { return Ok(await _service.GetItemsAsync(documentId, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpGet("api/educational-documents/{documentId:guid}/answer-key")]
    public async Task<IActionResult> GetAnswerKey(Guid documentId, CancellationToken ct)
    {
        try { return Ok(await _service.GetAnswerKeyAsync(documentId, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpPost("api/educational-documents/{documentId:guid}/regenerate")]
    public async Task<IActionResult> Regenerate(Guid documentId, CancellationToken ct)
    {
        try
        {
            var result = await _service.RegenerateAsync(documentId, ct);
            return CreatedAtAction(nameof(GetById), new { documentId = result.DocumentId }, result);
        }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpPost("api/educational-documents/{documentId:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(
        Guid documentId, [FromBody] DuplicateEducationalDocumentRequest? request, CancellationToken ct)
    {
        try { return Ok(await _service.DuplicateAsync(documentId, request, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpPost("api/educational-documents/{documentId:guid}/reuse")]
    public async Task<IActionResult> Reuse(
        Guid documentId, [FromBody] ReuseEducationalDocumentRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.ReuseAsync(documentId, request, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpGet("api/educational-documents/{documentId:guid}/reuse-targets")]
    public async Task<IActionResult> ReuseTargets(Guid documentId, CancellationToken ct)
    {
        try { return Ok(await _service.ListReuseTargetsAsync(documentId, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpPost("api/educational-documents/{documentId:guid}/save-as-template")]
    public async Task<IActionResult> SaveAsTemplate(Guid documentId, CancellationToken ct)
    {
        try { return Ok(await _service.SaveAsTemplateAsync(documentId, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpPut("api/educational-documents/{documentId:guid}")]
    public async Task<IActionResult> Update(Guid documentId, [FromBody] UpdateEducationalDocumentRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.UpdateAsync(documentId, request, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpPut("api/educational-documents/{documentId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid documentId, [FromBody] UpdateEducationalDocumentStatusRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.UpdateStatusAsync(documentId, request, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpPost("api/educational-documents/{documentId:guid}/items")]
    public async Task<IActionResult> AddItem(Guid documentId, [FromBody] CreateEducationalItemRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.AddItemAsync(documentId, request, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpPut("api/educational-items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateEducationalItemRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.UpdateItemAsync(itemId, request, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpDelete("api/educational-items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid itemId, CancellationToken ct)
    {
        try
        {
            await _service.DeleteItemAsync(itemId, ct);
            return NoContent();
        }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpPost("api/educational-documents/{documentId:guid}/items/reorder")]
    public async Task<IActionResult> Reorder(Guid documentId, [FromBody] ReorderEducationalItemsRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.ReorderItemsAsync(documentId, request, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpPost("api/educational-items/{itemId:guid}/regenerate")]
    public async Task<IActionResult> RegenerateItem(Guid itemId, [FromBody] RegenerateEducationalItemRequest? request, CancellationToken ct)
    {
        try { return Ok(await _service.RegenerateItemAsync(itemId, request ?? new RegenerateEducationalItemRequest(), ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpPost("api/educational-documents/{documentId:guid}/set-current")]
    public async Task<IActionResult> SetCurrent(Guid documentId, CancellationToken ct)
    {
        try { return Ok(await _service.SetCurrentAsync(documentId, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpGet("api/educational-documents/{documentId:guid}/revisions")]
    public async Task<IActionResult> Revisions(Guid documentId, CancellationToken ct)
    {
        try { return Ok(await _service.GetRevisionsAsync(documentId, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpGet("api/educational-documents/{documentId:guid}/validation")]
    [HttpPost("api/educational-documents/{documentId:guid}/validate")]
    public async Task<IActionResult> Validate(Guid documentId, CancellationToken ct)
    {
        try { return Ok(await _service.ValidateAsync(documentId, ct)); }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpPost("api/educational-documents/{documentId:guid}/feedback")]
    [ProducesResponseType(typeof(MaterialFeedbackDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Feedback(Guid documentId, [FromBody] SubmitMaterialFeedbackRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _service.SubmitFeedbackAsync(documentId, request, ct);
            return CreatedAtAction(nameof(GetById), new { documentId }, result);
        }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    [HttpDelete("api/educational-documents/{documentId:guid}")]
    public async Task<IActionResult> Delete(Guid documentId, CancellationToken ct)
    {
        try
        {
            await _service.SoftDeleteAsync(documentId, ct);
            return NoContent();
        }
        catch (Exception ex) when (ex is EducationalDocumentGenerationException or GeminiApiException)
        { return ToProblem(ex); }
    }

    private IActionResult ToProblem(Exception ex)
    {
        return ex switch
        {
            EducationalDocumentGenerationException e => StatusCode(e.StatusCode, Problem(e.StatusCode, e.ErrorCode, e.Message)),
            GeminiApiException g => StatusCode(g.StatusCode ?? 503, Problem(g.StatusCode ?? 503, g.ErrorCode, g.Message)),
            _ => StatusCode(500, Problem(500, "AiProviderError", "Error inesperado."))
        };
    }

    private object Problem(int status, string error, string message) => new
    {
        status,
        error,
        message,
        traceId = HttpContext.TraceIdentifier
    };
}
