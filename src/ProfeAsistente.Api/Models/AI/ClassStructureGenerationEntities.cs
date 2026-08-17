using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Models.AI;

public class ClassStructureGeneration
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public int GenerationNumber { get; set; }
    public AiGenerationStatus Status { get; set; } = AiGenerationStatus.Pending;
    public string Provider { get; set; } = "Gemini";
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = "class-structure-v1";
    public Guid? CurriculumSnapshotId { get; set; }
    public string? RequestJsonPath { get; set; }
    public string? ResponseJsonPath { get; set; }
    public string? GeneratedTitle { get; set; }
    public string? GeneratedPurpose { get; set; }
    public string? GeneratedStartJson { get; set; }
    public string? GeneratedDevelopmentJson { get; set; }
    public string? GeneratedClosureJson { get; set; }
    public string? FormativeAssessmentJson { get; set; }
    public string? DifferentiationJson { get; set; }
    public string? CurriculumReferenceJson { get; set; }
    public bool RequiresReview { get; set; }
    public string WarningsJson { get; set; } = "[]";
    public int? InputTokenCount { get; set; }
    public int? OutputTokenCount { get; set; }
    public long DurationMilliseconds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsCurrentVersion { get; set; }
    public bool IsOutdated { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    /// <summary>Hash of class config used for generation (OA+indicators+Bloom+duration).</summary>
    public string? ConfigurationFingerprint { get; set; }

    public ICollection<ClassStructureRevision> Revisions { get; set; } = [];
}

public class ClassStructureRevision
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public int RevisionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string StartJson { get; set; } = "{}";
    public string DevelopmentJson { get; set; } = "{}";
    public string ClosureJson { get; set; } = "{}";
    public string? FormativeAssessmentJson { get; set; }
    public string? DifferentiationJson { get; set; }
    public string? EditedBy { get; set; }
    public DateTime EditedAt { get; set; } = DateTime.UtcNow;
    public bool IsCurrent { get; set; }
    public string? ChangeSummary { get; set; }
    public bool WasManuallyModified { get; set; }

    public ClassStructureGeneration? Generation { get; set; }
}

public class AiUsageRecord
{
    public Guid Id { get; set; }
    public string OperationType { get; set; } = "ClassStructure";
    public Guid? ClassId { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? ItemId { get; set; }
    public string? DocumentType { get; set; }
    public string? GenerationType { get; set; }
    public string Provider { get; set; } = "Gemini";
    public string Model { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
}
