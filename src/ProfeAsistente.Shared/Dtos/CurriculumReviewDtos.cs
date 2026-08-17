using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Shared.Dtos;

public class CurriculumReviewSessionDto
{
    public Guid Id { get; set; }
    public Guid ImportBatchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reviewer { get; set; }
    public int VersionRevision { get; set; }
    public string? RowVersion { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public string? ReviewContentHash { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? LastValidationAt { get; set; }
    public DateTime? LastDiffAt { get; set; }
}

public class CurriculumReviewPackageDto
{
    public Guid ImportBatchId { get; set; }
    public Guid ReviewSessionId { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
    public string ImportStatus { get; set; } = string.Empty;
    public string? RowVersion { get; set; }
    public string? DocumentTitle { get; set; }
    public string? LevelName { get; set; }
    public string? SubjectName { get; set; }
    public double ExtractionConfidence { get; set; }
    public List<ReviewUnitDto> Units { get; set; } = [];
    public List<ReviewObjectiveDto> Objectives { get; set; } = [];
    public List<ReviewIndicatorDto> Indicators { get; set; } = [];
    public List<ReviewSkillDto> Skills { get; set; } = [];
    public List<ReviewAttitudeDto> Attitudes { get; set; } = [];
}

public class ReviewUnitDto
{
    public string TemporaryId { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SuggestedHours { get; set; }
    public string Decision { get; set; } = "Pending";
    public bool WasManuallyModified { get; set; }
    public bool IsDeleted { get; set; }
    public int ObjectiveCount { get; set; }
    public int IssueCount { get; set; }
    public int? PageStart { get; set; }
}

public class ReviewObjectiveDto
{
    public string TemporaryId { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ExtractedCode { get; set; } = string.Empty;
    public string ExtractedDescription { get; set; } = string.Empty;
    public List<string> UnitTemporaryIds { get; set; } = [];
    public string? AxisTemporaryId { get; set; }
    public string Decision { get; set; } = "Pending";
    public bool WasManuallyModified { get; set; }
    public bool IsDeleted { get; set; }
    public decimal ExtractionConfidence { get; set; }
    public int IndicatorCount { get; set; }
    public int IssueCount { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
    public string? SourceFragment { get; set; }
    public List<ReviewFieldIssueDto> Issues { get; set; } = [];
}

public class ReviewIndicatorDto
{
    public string TemporaryId { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ExtractedDescription { get; set; } = string.Empty;
    public string ObjectiveTemporaryId { get; set; } = string.Empty;
    public string Decision { get; set; } = "Pending";
    public bool WasManuallyModified { get; set; }
    public bool IsDeleted { get; set; }
    public int Order { get; set; }
    public List<ReviewFieldIssueDto> Issues { get; set; } = [];
}

public class ReviewSkillDto
{
    public string TemporaryId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Decision { get; set; } = "Pending";
    public bool WasManuallyModified { get; set; }
    public bool IsDeleted { get; set; }
}

public class ReviewAttitudeDto
{
    public string TemporaryId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Decision { get; set; } = "Pending";
    public bool WasManuallyModified { get; set; }
    public bool IsDeleted { get; set; }
}

public class ReviewFieldIssueDto
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public string Message { get; set; } = string.Empty;
    public string? FieldName { get; set; }
}

public class CurriculumReviewSummaryDto
{
    public Guid ImportBatchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ImportStatus { get; set; } = string.Empty;
    public EntityDecisionCountsDto Units { get; set; } = new();
    public EntityDecisionCountsDto Objectives { get; set; } = new();
    public EntityDecisionCountsDto Indicators { get; set; } = new();
    public IssueCountsDto Issues { get; set; } = new();
    public int Changes { get; set; }
    public int UnresolvedComments { get; set; }
    public DateTime? LastValidationAt { get; set; }
    public DateTime? LastDiffAt { get; set; }
    public bool CanMarkReady { get; set; }
    public string? DocumentTitle { get; set; }
    public string? LevelName { get; set; }
    public string? SubjectName { get; set; }
    public double ExtractionConfidence { get; set; }
    public int Skills { get; set; }
    public int Attitudes { get; set; }
}

public class EntityDecisionCountsDto
{
    public int Total { get; set; }
    public int Accepted { get; set; }
    public int Corrected { get; set; }
    public int Pending { get; set; }
    public int Rejected { get; set; }
    public int Ignored { get; set; }
}

public class IssueCountsDto
{
    public int Blocking { get; set; }
    public int Errors { get; set; }
    public int Warnings { get; set; }
    public int Info { get; set; }
}

public class UpdateReviewUnitRequest
{
    public int? Number { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? SuggestedHours { get; set; }
    public int? Order { get; set; }
    public CurriculumRecordDecision? Decision { get; set; }
    public string? Reason { get; set; }
    public string? RowVersion { get; set; }
}

public class UpdateReviewObjectiveRequest
{
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? AxisTemporaryId { get; set; }
    public List<string>? UnitTemporaryIds { get; set; }
    public CurriculumRecordDecision? Decision { get; set; }
    public string? Reason { get; set; }
    public string? RowVersion { get; set; }
}

public class UpdateReviewIndicatorRequest
{
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? ObjectiveTemporaryId { get; set; }
    public int? Order { get; set; }
    public CurriculumRecordDecision? Decision { get; set; }
    public string? Reason { get; set; }
    public string? RowVersion { get; set; }
}

public class UpdateReviewSkillRequest
{
    public string? Description { get; set; }
    public CurriculumRecordDecision? Decision { get; set; }
    public string? Reason { get; set; }
    public string? RowVersion { get; set; }
}

public class UpdateReviewAttitudeRequest
{
    public string? Description { get; set; }
    public CurriculumRecordDecision? Decision { get; set; }
    public string? Reason { get; set; }
    public string? RowVersion { get; set; }
}

public class AddReviewObjectiveRequest
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? UnitTemporaryId { get; set; }
    public string? Reason { get; set; }
    public string? RowVersion { get; set; }
}

public class AddReviewIndicatorRequest
{
    public string Description { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Reason { get; set; }
    public string? RowVersion { get; set; }
}

public class SplitObjectiveRequest
{
    public SplitObjectivePart First { get; set; } = new();
    public SplitObjectivePart Second { get; set; } = new();
    public Dictionary<string, string> IndicatorAssignments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Reason { get; set; }
    public string? RowVersion { get; set; }
}

public class SplitObjectivePart
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class MergeReviewRequest
{
    public string EntityType { get; set; } = "LearningObjective";
    public List<string> TemporaryIds { get; set; } = [];
    public SplitObjectivePart Result { get; set; } = new();
    public string? Reason { get; set; }
    public string? RowVersion { get; set; }
}

public class DeleteReviewRecordRequest
{
    public string? Reason { get; set; }
    public string? RowVersion { get; set; }
}

public class ReviewCommentDto
{
    public Guid Id { get; set; }
    public string? EntityType { get; set; }
    public string? EntityTemporaryId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsResolved { get; set; }
}

public class AddReviewCommentRequest
{
    public string? EntityType { get; set; }
    public string? EntityTemporaryId { get; set; }
    public string Message { get; set; } = string.Empty;
    public CurriculumCommentSeverity Severity { get; set; } = CurriculumCommentSeverity.Info;
}

public class ReviewChangeDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityTemporaryId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
    public string? Reason { get; set; }
    public bool IsReverted { get; set; }
}

public class BulkDecisionRequest
{
    public List<string> TemporaryIds { get; set; } = [];
    public CurriculumRecordDecision Decision { get; set; } = CurriculumRecordDecision.Accepted;
    public string EntityType { get; set; } = "LearningObjective";
    public string? Reason { get; set; }
    public bool OnlyWithoutIssues { get; set; }
    public string? RowVersion { get; set; }
}

public class RejectReviewRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class CurriculumValidationResultDto
{
    public bool IsValid { get; set; }
    public bool CanMarkReady { get; set; }
    public List<ValidationIssueDto> Issues { get; set; } = [];
    public DateTime ValidatedAt { get; set; }
}

public class FieldDiffDto
{
    public string Field { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Significance { get; set; } = "None";
    public List<TextSegmentDiffDto> Difference { get; set; } = [];
}

public class TextSegmentDiffDto
{
    public string Type { get; set; } = "Unchanged";
    public string Text { get; set; } = string.Empty;
}

public class RichCurriculumDiffItemDto
{
    public string EntityType { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string TemporaryId { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public List<FieldDiffDto> Fields { get; set; } = [];
}

public class RichCurriculumDiffResultDto
{
    public Guid ImportBatchId { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<RichCurriculumDiffItemDto> Items { get; set; } = [];
}
