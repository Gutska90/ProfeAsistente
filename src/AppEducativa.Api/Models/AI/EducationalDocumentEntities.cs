using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Models.AI;

public class EducationalDocument
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public EducationalDocumentType DocumentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public EducationalDocumentStatus Status { get; set; } = EducationalDocumentStatus.Draft;
    public Guid? CurriculumSnapshotId { get; set; }
    public Guid? ClassStructureGenerationId { get; set; }
    public string BloomLevel { get; set; } = string.Empty;
    public ItemDifficulty Difficulty { get; set; } = ItemDifficulty.Intermediate;
    public int? EstimatedDurationMinutes { get; set; }
    public decimal? TotalPoints { get; set; }
    public string Provider { get; set; } = "Gemini";
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string CurriculumRelease { get; set; } = string.Empty;
    public Guid ObjectiveId { get; set; }
    public string ObjectiveCode { get; set; } = string.Empty;
    public string WarningsJson { get; set; } = "[]";
    public bool RequiresReview { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public bool IsCurrentVersion { get; set; }
    public bool IsOutdated { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();
    public string? ConfigurationFingerprint { get; set; }

    public ICollection<EducationalDocumentGeneration> Generations { get; set; } = [];
    public ICollection<EducationalItem> Items { get; set; } = [];
    public ICollection<AssessmentSpecification> Specifications { get; set; } = [];
    public ICollection<EducationalDocumentRevision> Revisions { get; set; } = [];
}

public class EducationalDocumentGeneration
{
    public Guid Id { get; set; }
    public Guid EducationalDocumentId { get; set; }
    public int GenerationNumber { get; set; }
    public AiGenerationStatus Status { get; set; } = AiGenerationStatus.Pending;
    public string? RequestJsonPath { get; set; }
    public string? ResponseJsonPath { get; set; }
    public int? InputTokenCount { get; set; }
    public int? OutputTokenCount { get; set; }
    public long DurationMilliseconds { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public EducationalDocument? Document { get; set; }
}

public class EducationalItem
{
    public Guid Id { get; set; }
    public Guid EducationalDocumentId { get; set; }
    public int Order { get; set; }
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
    public bool IsManuallyEdited { get; set; }
    public bool IsDeleted { get; set; }
    public Guid? SourceGenerationId { get; set; }

    public EducationalDocument? Document { get; set; }
    public ICollection<EducationalItemOption> Options { get; set; } = [];
    public ICollection<EducationalItemIndicator> Indicators { get; set; } = [];
}

public class EducationalItemOption
{
    public Guid Id { get; set; }
    public Guid EducationalItemId { get; set; }
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? Feedback { get; set; }

    public EducationalItem? Item { get; set; }
}

public class EducationalItemIndicator
{
    public Guid EducationalItemId { get; set; }
    public Guid EvaluationIndicatorId { get; set; }

    public EducationalItem? Item { get; set; }
}

public class AssessmentSpecification
{
    public Guid Id { get; set; }
    public Guid EducationalDocumentId { get; set; }
    public Guid EvaluationIndicatorId { get; set; }
    public string BloomLevel { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalPoints { get; set; }
    public decimal WeightPercentage { get; set; }

    public EducationalDocument? Document { get; set; }
}

public class EducationalDocumentRevision
{
    public Guid Id { get; set; }
    public Guid EducationalDocumentId { get; set; }
    public int RevisionNumber { get; set; }
    public string ContentJsonPath { get; set; } = string.Empty;
    public string? ChangeSummary { get; set; }
    public string? EditedBy { get; set; }
    public DateTime EditedAt { get; set; } = DateTime.UtcNow;
    public bool IsCurrent { get; set; }

    public EducationalDocument? Document { get; set; }
}
