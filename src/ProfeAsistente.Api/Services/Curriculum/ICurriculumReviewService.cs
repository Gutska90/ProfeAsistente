using ProfeAsistente.Shared.Dtos;

namespace ProfeAsistente.Api.Services.Curriculum;

public interface ICurriculumReviewService
{
    Task<CurriculumReviewSessionDto> StartReviewAsync(
        Guid importBatchId, string? reviewer, CancellationToken cancellationToken = default);

    Task<CurriculumReviewPackageDto?> GetReviewPackageAsync(
        Guid importBatchId, CancellationToken cancellationToken = default);

    Task<CurriculumReviewPackageDto> UpdateUnitAsync(
        Guid importBatchId, string unitTemporaryId, UpdateReviewUnitRequest request,
        CancellationToken cancellationToken = default);

    Task<CurriculumReviewPackageDto> UpdateObjectiveAsync(
        Guid importBatchId, string objectiveTemporaryId, UpdateReviewObjectiveRequest request,
        CancellationToken cancellationToken = default);

    Task<CurriculumReviewPackageDto> UpdateIndicatorAsync(
        Guid importBatchId, string indicatorTemporaryId, UpdateReviewIndicatorRequest request,
        CancellationToken cancellationToken = default);

    Task<CurriculumReviewPackageDto> UpdateSkillAsync(
        Guid importBatchId, string skillTemporaryId, UpdateReviewSkillRequest request,
        CancellationToken cancellationToken = default);

    Task<CurriculumReviewPackageDto> UpdateAttitudeAsync(
        Guid importBatchId, string attitudeTemporaryId, UpdateReviewAttitudeRequest request,
        CancellationToken cancellationToken = default);

    Task<CurriculumReviewPackageDto> AddObjectiveAsync(
        Guid importBatchId, AddReviewObjectiveRequest request,
        CancellationToken cancellationToken = default);

    Task<CurriculumReviewPackageDto> AddIndicatorAsync(
        Guid importBatchId, string objectiveTemporaryId, AddReviewIndicatorRequest request,
        CancellationToken cancellationToken = default);

    Task<CurriculumReviewPackageDto> DeleteRecordAsync(
        Guid importBatchId, string entityType, string temporaryId, DeleteReviewRecordRequest request,
        CancellationToken cancellationToken = default);

    Task<CurriculumReviewPackageDto> RestoreRecordAsync(
        Guid importBatchId, string entityType, string temporaryId, string? rowVersion,
        CancellationToken cancellationToken = default);

    Task RevertChangeAsync(
        Guid importBatchId, Guid changeId, string? rowVersion,
        CancellationToken cancellationToken = default);

    Task<CurriculumValidationResultDto> RevalidateAsync(
        Guid importBatchId, CancellationToken cancellationToken = default);

    Task MarkReadyForApprovalAsync(
        Guid importBatchId, string? user, CancellationToken cancellationToken = default);

    Task ApproveFromReviewAsync(
        Guid importBatchId, string? user, CancellationToken cancellationToken = default);

    Task RejectFromReviewAsync(
        Guid importBatchId, string reason, string? user, CancellationToken cancellationToken = default);

    Task PublishAsync(
        Guid importBatchId, string? user, CancellationToken cancellationToken = default);

    Task<CurriculumReviewSummaryDto> GetSummaryAsync(
        Guid importBatchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReviewChangeDto>> GetChangesAsync(
        Guid importBatchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReviewCommentDto>> GetCommentsAsync(
        Guid importBatchId, CancellationToken cancellationToken = default);

    Task<ReviewCommentDto> AddCommentAsync(
        Guid importBatchId, AddReviewCommentRequest request, string? user,
        CancellationToken cancellationToken = default);

    Task ResolveCommentAsync(
        Guid importBatchId, Guid commentId, string? user,
        CancellationToken cancellationToken = default);

    Task<CurriculumReviewPackageDto> SplitObjectiveAsync(
        Guid importBatchId, string objectiveTemporaryId, SplitObjectiveRequest request,
        CancellationToken cancellationToken = default);

    Task<CurriculumReviewPackageDto> MergeAsync(
        Guid importBatchId, MergeReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<CurriculumReviewPackageDto> BulkDecideAsync(
        Guid importBatchId, BulkDecisionRequest request,
        CancellationToken cancellationToken = default);

    Task<RichCurriculumDiffResultDto> GetRichDiffAsync(
        Guid importBatchId, CancellationToken cancellationToken = default);
}
