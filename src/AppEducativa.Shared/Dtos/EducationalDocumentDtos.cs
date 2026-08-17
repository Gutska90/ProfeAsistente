using AppEducativa.Shared.Enums;

namespace AppEducativa.Shared.Dtos;

public sealed class GenerateEducationalDocumentRequest
{
    public EducationalDocumentType DocumentType { get; set; }
    public int ItemCount { get; set; } = 10;
    public List<Guid> EvaluationIndicatorIds { get; set; } = [];
    public ItemDifficulty Difficulty { get; set; } = ItemDifficulty.Intermediate;
    public int? EstimatedDurationMinutes { get; set; }
    public bool IncludeAnswerKey { get; set; } = true;
    public bool IncludeFeedback { get; set; } = true;
    public bool IncludeScoring { get; set; } = true;
    public bool IncludeDifferentiation { get; set; }
    public List<EducationalItemType> AllowedItemTypes { get; set; } = [];
    public string? TeacherInstructions { get; set; }
    public string? StudentInstructions { get; set; }
}

public sealed class RegenerateEducationalItemRequest
{
    public string? Reason { get; set; }
    public bool KeepItemType { get; set; } = true;
    public bool KeepIndicator { get; set; } = true;
    public ItemDifficulty? TargetDifficulty { get; set; }
}

public sealed class UpdateEducationalDocumentRequest
{
    public string? RowVersion { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public int? EstimatedDurationMinutes { get; set; }
    public decimal? TotalPoints { get; set; }
    public string? ChangeSummary { get; set; }
}

public sealed class UpdateEducationalDocumentStatusRequest
{
    public EducationalDocumentStatus Status { get; set; }
    public string? Note { get; set; }
}

public class UpdateEducationalItemRequest
{
    public EducationalItemType ItemType { get; set; }
    public string Statement { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public ItemDifficulty Difficulty { get; set; }
    public string BloomLevel { get; set; } = string.Empty;
    public decimal Points { get; set; }
    public string? ExpectedAnswer { get; set; }
    public string? Explanation { get; set; }
    public string? TeacherNotes { get; set; }
    public bool IsRequired { get; set; } = true;
    public List<Guid> EvaluationIndicatorIds { get; set; } = [];
    public List<EducationalItemOptionDto> Options { get; set; } = [];
}

public sealed class CreateEducationalItemRequest : UpdateEducationalItemRequest
{
    public int? Order { get; set; }
}

public sealed class ReorderEducationalItemsRequest
{
    public List<ReorderEducationalItemDto> Items { get; set; } = [];
}

public sealed class ReorderEducationalItemDto
{
    public Guid ItemId { get; set; }
    public int Order { get; set; }
}

public sealed class EducationalDocumentSummaryDto
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BloomLevel { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal? TotalPoints { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool IsCurrentVersion { get; set; }
    public bool IsOutdated { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> Warnings { get; set; } = [];
}

public sealed class EducationalDocumentGenerationResultDto
{
    public Guid DocumentId { get; set; }
    public Guid GenerationId { get; set; }
    public Guid ClassId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DocumentStatus { get; set; } = string.Empty;
    public bool RequiresReview { get; set; }
    public List<string> Warnings { get; set; } = [];
    public EducationalDocumentDetailDto? Document { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class EducationalDocumentDetailDto
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BloomLevel { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int? EstimatedDurationMinutes { get; set; }
    public decimal? TotalPoints { get; set; }
    public string CurriculumRelease { get; set; } = string.Empty;
    public string ObjectiveCode { get; set; } = string.Empty;
    public Guid ObjectiveId { get; set; }
    public List<Guid> IndicatorIds { get; set; } = [];
    public Guid? ClassStructureGenerationId { get; set; }
    public Guid? CurriculumSnapshotId { get; set; }
    public bool IsCurrentVersion { get; set; }
    public bool IsOutdated { get; set; }
    public bool RequiresReview { get; set; }
    public List<string> Warnings { get; set; } = [];
    public string? RowVersion { get; set; }
    public List<EducationalItemDto> Items { get; set; } = [];
    public List<AssessmentSpecificationRowDto> SpecificationTable { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Vista docente: incluye respuestas, explicaciones y notas.</summary>
public sealed class EducationalItemDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public string BloomLevel { get; set; } = string.Empty;
    public decimal Points { get; set; }
    public string? ExpectedAnswer { get; set; }
    public string? Explanation { get; set; }
    public string? TeacherNotes { get; set; }
    public bool IsRequired { get; set; }
    public bool IsManuallyEdited { get; set; }
    public List<Guid> EvaluationIndicatorIds { get; set; } = [];
    public List<EducationalItemOptionDto> Options { get; set; } = [];
}

/// <summary>Vista estudiante: sin clave ni notas docentes.</summary>
public sealed class EducationalItemStudentDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public string BloomLevel { get; set; } = string.Empty;
    public decimal Points { get; set; }
    public bool IsRequired { get; set; }
    public List<EducationalItemOptionStudentDto> Options { get; set; } = [];
}

public sealed class EducationalItemOptionDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? Feedback { get; set; }
}

public sealed class EducationalItemOptionStudentDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
}

public sealed class EducationalDocumentStudentViewDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public int? EstimatedDurationMinutes { get; set; }
    public decimal? TotalPoints { get; set; }
    public string ObjectiveCode { get; set; } = string.Empty;
    public List<EducationalItemStudentDto> Items { get; set; } = [];
}

public sealed class AssessmentSpecificationRowDto
{
    public Guid Id { get; set; }
    public Guid EvaluationIndicatorId { get; set; }
    public string IndicatorDescription { get; set; } = string.Empty;
    public string BloomLevel { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalPoints { get; set; }
    public decimal WeightPercentage { get; set; }
}

public sealed class AnswerKeyDto
{
    public Guid DocumentId { get; set; }
    public List<AnswerKeyEntryDto> Entries { get; set; } = [];
}

public sealed class AnswerKeyEntryDto
{
    public Guid ItemId { get; set; }
    public int Order { get; set; }
    public string StatementPreview { get; set; } = string.Empty;
    public string? ExpectedAnswer { get; set; }
    public List<string> CorrectOptions { get; set; } = [];
    public string? Explanation { get; set; }
    public decimal Points { get; set; }
}

public sealed class EducationalDocumentRevisionSummaryDto
{
    public Guid Id { get; set; }
    public int RevisionNumber { get; set; }
    public string? ChangeSummary { get; set; }
    public DateTime EditedAt { get; set; }
    public bool IsCurrent { get; set; }
}

public sealed class EducationalDocumentValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
