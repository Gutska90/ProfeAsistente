using System.Security.Claims;
using AppEducativa.Api.Services.Curriculum;
using AppEducativa.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppEducativa.Api.Controllers;

[ApiController]
[Route("api/admin/curriculum/imports/{id:guid}/review")]
[Authorize(Policy = "CurriculumAdmin")]
public class CurriculumReviewController : ControllerBase
{
    private readonly ICurriculumReviewService _review;

    public CurriculumReviewController(ICurriculumReviewService review) => _review = review;

    [HttpPost("start")]
    public Task<ActionResult<CurriculumReviewSessionDto>> Start(Guid id, CancellationToken ct) =>
        Execute(() => _review.StartReviewAsync(id, UserName(), ct));

    [HttpGet]
    public async Task<ActionResult<CurriculumReviewPackageDto>> Get(Guid id, CancellationToken ct)
    {
        try
        {
            var package = await _review.GetReviewPackageAsync(id, ct);
            return package is null ? NotFound(new { error = "No hay sesión de revisión activa." }) : Ok(package);
        }
        catch (CurriculumReviewException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    [HttpGet("summary")]
    public Task<ActionResult<CurriculumReviewSummaryDto>> Summary(Guid id, CancellationToken ct) =>
        Execute(() => _review.GetSummaryAsync(id, ct));

    [HttpPut("units/{temporaryId}")]
    public Task<ActionResult<CurriculumReviewPackageDto>> UpdateUnit(
        Guid id, string temporaryId, [FromBody] UpdateReviewUnitRequest request, CancellationToken ct) =>
        Execute(() => _review.UpdateUnitAsync(id, temporaryId, request, ct));

    [HttpPut("objectives/{temporaryId}")]
    public Task<ActionResult<CurriculumReviewPackageDto>> UpdateObjective(
        Guid id, string temporaryId, [FromBody] UpdateReviewObjectiveRequest request, CancellationToken ct) =>
        Execute(() => _review.UpdateObjectiveAsync(id, temporaryId, request, ct));

    [HttpPut("indicators/{temporaryId}")]
    public Task<ActionResult<CurriculumReviewPackageDto>> UpdateIndicator(
        Guid id, string temporaryId, [FromBody] UpdateReviewIndicatorRequest request, CancellationToken ct) =>
        Execute(() => _review.UpdateIndicatorAsync(id, temporaryId, request, ct));

    [HttpPut("skills/{temporaryId}")]
    public Task<ActionResult<CurriculumReviewPackageDto>> UpdateSkill(
        Guid id, string temporaryId, [FromBody] UpdateReviewSkillRequest request, CancellationToken ct) =>
        Execute(() => _review.UpdateSkillAsync(id, temporaryId, request, ct));

    [HttpPut("attitudes/{temporaryId}")]
    public Task<ActionResult<CurriculumReviewPackageDto>> UpdateAttitude(
        Guid id, string temporaryId, [FromBody] UpdateReviewAttitudeRequest request, CancellationToken ct) =>
        Execute(() => _review.UpdateAttitudeAsync(id, temporaryId, request, ct));

    [HttpPost("objectives")]
    public Task<ActionResult<CurriculumReviewPackageDto>> AddObjective(
        Guid id, [FromBody] AddReviewObjectiveRequest request, CancellationToken ct) =>
        Execute(() => _review.AddObjectiveAsync(id, request, ct));

    [HttpPost("objectives/{temporaryId}/indicators")]
    public Task<ActionResult<CurriculumReviewPackageDto>> AddIndicator(
        Guid id, string temporaryId, [FromBody] AddReviewIndicatorRequest request, CancellationToken ct) =>
        Execute(() => _review.AddIndicatorAsync(id, temporaryId, request, ct));

    [HttpPost("objectives/{temporaryId}/split")]
    public Task<ActionResult<CurriculumReviewPackageDto>> Split(
        Guid id, string temporaryId, [FromBody] SplitObjectiveRequest request, CancellationToken ct) =>
        Execute(() => _review.SplitObjectiveAsync(id, temporaryId, request, ct));

    [HttpPost("merge")]
    public Task<ActionResult<CurriculumReviewPackageDto>> Merge(
        Guid id, [FromBody] MergeReviewRequest request, CancellationToken ct) =>
        Execute(() => _review.MergeAsync(id, request, ct));

    [HttpPost("bulk-decide")]
    public Task<ActionResult<CurriculumReviewPackageDto>> BulkDecide(
        Guid id, [FromBody] BulkDecisionRequest request, CancellationToken ct) =>
        Execute(() => _review.BulkDecideAsync(id, request, ct));

    [HttpDelete("{entityType}/{temporaryId}")]
    public Task<ActionResult<CurriculumReviewPackageDto>> Delete(
        Guid id, string entityType, string temporaryId, [FromBody] DeleteReviewRecordRequest? request, CancellationToken ct) =>
        Execute(() => _review.DeleteRecordAsync(id, entityType, temporaryId, request ?? new DeleteReviewRecordRequest(), ct));

    [HttpPost("{entityType}/{temporaryId}/restore")]
    public Task<ActionResult<CurriculumReviewPackageDto>> Restore(
        Guid id, string entityType, string temporaryId, [FromQuery] string? rowVersion, CancellationToken ct) =>
        Execute(() => _review.RestoreRecordAsync(id, entityType, temporaryId, rowVersion, ct));

    [HttpGet("changes")]
    public Task<ActionResult<IReadOnlyList<ReviewChangeDto>>> Changes(Guid id, CancellationToken ct) =>
        Execute(() => _review.GetChangesAsync(id, ct));

    [HttpPost("changes/{changeId:guid}/revert")]
    public async Task<IActionResult> Revert(Guid id, Guid changeId, [FromQuery] string? rowVersion, CancellationToken ct)
    {
        try
        {
            await _review.RevertChangeAsync(id, changeId, rowVersion, ct);
            return NoContent();
        }
        catch (CurriculumReviewException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    [HttpGet("comments")]
    public Task<ActionResult<IReadOnlyList<ReviewCommentDto>>> Comments(Guid id, CancellationToken ct) =>
        Execute(() => _review.GetCommentsAsync(id, ct));

    [HttpPost("comments")]
    public Task<ActionResult<ReviewCommentDto>> AddComment(
        Guid id, [FromBody] AddReviewCommentRequest request, CancellationToken ct) =>
        Execute(() => _review.AddCommentAsync(id, request, UserName(), ct));

    [HttpPut("comments/{commentId:guid}/resolve")]
    public async Task<IActionResult> ResolveComment(Guid id, Guid commentId, CancellationToken ct)
    {
        try
        {
            await _review.ResolveCommentAsync(id, commentId, UserName(), ct);
            return NoContent();
        }
        catch (CurriculumReviewException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    [HttpPost("revalidate")]
    public Task<ActionResult<CurriculumValidationResultDto>> Revalidate(Guid id, CancellationToken ct) =>
        Execute(() => _review.RevalidateAsync(id, ct));

    [HttpGet("diff")]
    public Task<ActionResult<RichCurriculumDiffResultDto>> Diff(Guid id, CancellationToken ct) =>
        Execute(() => _review.GetRichDiffAsync(id, ct));

    [HttpPost("ready")]
    public async Task<IActionResult> Ready(Guid id, CancellationToken ct)
    {
        try
        {
            await _review.MarkReadyForApprovalAsync(id, UserName(), ct);
            return NoContent();
        }
        catch (CurriculumReviewException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (CurriculumReviewException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    private string? UserName() => User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
}
