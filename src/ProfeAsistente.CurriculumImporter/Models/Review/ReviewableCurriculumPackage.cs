using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.CurriculumImporter.Models.Review;

public sealed class ReviewableCurriculumPackage
{
    public string SourceId { get; set; } = string.Empty;
    public string? LevelCode { get; set; }
    public string? LevelName { get; set; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public string? DocumentTitle { get; set; }
    public string? DocumentUrl { get; set; }
    public string? DocumentHash { get; set; }
    public double ExtractionConfidence { get; set; }
    public List<ReviewableUnit> Units { get; set; } = [];
    public List<ReviewableLearningObjective> Objectives { get; set; } = [];
    public List<ReviewableEvaluationIndicator> Indicators { get; set; } = [];
    public List<ReviewableSkill> Skills { get; set; } = [];
    public List<ReviewableAttitude> Attitudes { get; set; } = [];
    public List<ReviewableAxis> Axes { get; set; } = [];
    public int NextUnitSeq { get; set; } = 1;
    public int NextOaSeq { get; set; } = 1;
    public int NextIndicatorSeq { get; set; } = 1;
    public int NextSkillSeq { get; set; } = 1;
    public int NextAttitudeSeq { get; set; } = 1;
    public int NextAxisSeq { get; set; } = 1;
}

public sealed class ReviewableUnit
{
    public required string TemporaryId { get; init; }
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SuggestedHours { get; set; }
    public int Order { get; set; }
    public string ExtractedName { get; set; } = string.Empty;
    public string? ExtractedDescription { get; set; }
    public CurriculumRecordDecision Decision { get; set; } = CurriculumRecordDecision.Pending;
    public bool WasManuallyModified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public string? DeletionReason { get; set; }
    public List<string> LearningObjectiveTemporaryIds { get; set; } = [];
    public List<ReviewFieldIssue> Issues { get; set; } = [];
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
    public string? SourceFragment { get; set; }
}

public sealed class ReviewableLearningObjective
{
    public required string TemporaryId { get; init; }
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ExtractedCode { get; set; } = string.Empty;
    public string ExtractedDescription { get; set; } = string.Empty;
    public string? AxisTemporaryId { get; set; }
    public List<string> UnitTemporaryIds { get; set; } = [];
    public decimal ExtractionConfidence { get; set; } = 0.7m;
    public CurriculumRecordDecision Decision { get; set; } = CurriculumRecordDecision.Pending;
    public bool WasManuallyModified { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsMerged { get; set; }
    public string? MergedIntoTemporaryId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public string? DeletionReason { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
    public string? SourceFragment { get; set; }
    public List<ReviewFieldIssue> Issues { get; set; } = [];
}

public sealed class ReviewableEvaluationIndicator
{
    public required string TemporaryId { get; init; }
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ExtractedDescription { get; set; } = string.Empty;
    public string ObjectiveTemporaryId { get; set; } = string.Empty;
    public string? UnitTemporaryId { get; set; }
    public int Order { get; set; }
    public bool IsSuggested { get; set; } = true;
    public CurriculumRecordDecision Decision { get; set; } = CurriculumRecordDecision.Pending;
    public bool WasManuallyModified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public string? DeletionReason { get; set; }
    public int? PageStart { get; set; }
    public string? SourceFragment { get; set; }
    public List<ReviewFieldIssue> Issues { get; set; } = [];
}

public sealed class ReviewableSkill
{
    public required string TemporaryId { get; init; }
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ExtractedDescription { get; set; } = string.Empty;
    public string? AxisTemporaryId { get; set; }
    public CurriculumRecordDecision Decision { get; set; } = CurriculumRecordDecision.Pending;
    public bool WasManuallyModified { get; set; }
    public bool IsDeleted { get; set; }
    public List<ReviewFieldIssue> Issues { get; set; } = [];
}

public sealed class ReviewableAttitude
{
    public required string TemporaryId { get; init; }
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ExtractedDescription { get; set; } = string.Empty;
    public CurriculumRecordDecision Decision { get; set; } = CurriculumRecordDecision.Pending;
    public bool WasManuallyModified { get; set; }
    public bool IsDeleted { get; set; }
    public List<ReviewFieldIssue> Issues { get; set; } = [];
}

public sealed class ReviewableAxis
{
    public required string TemporaryId { get; init; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CurriculumRecordDecision Decision { get; set; } = CurriculumRecordDecision.Pending;
    public bool WasManuallyModified { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class ReviewFieldIssue
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public string Message { get; set; } = string.Empty;
    public string? FieldName { get; set; }
}
