using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Models.AI;

public sealed class EducationalDocumentGenerationContext
{
    public Guid ClassId { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public CurriculumObjectiveRef Objective { get; init; } = new();
    public List<CurriculumIndicatorRef> Indicators { get; init; } = [];
    public List<CurriculumSkillRef> Skills { get; init; } = [];
    public List<CurriculumAttitudeRef> Attitudes { get; init; } = [];
    public List<CurriculumTransversalRef> TransversalObjectives { get; init; } = [];
    public string BloomLevel { get; init; } = string.Empty;
    public string CurriculumRelease { get; init; } = string.Empty;
    public Guid? SnapshotId { get; init; }
    public Guid? ClassStructureGenerationId { get; init; }
    public ClassStructureSummaryForDocuments? ClassStructure { get; init; }
    public int? ClassDurationMinutes { get; init; }
    public EducationalDocumentType DocumentType { get; init; }
    public int ItemCount { get; init; }
    public ItemDifficulty Difficulty { get; init; }
    public List<EducationalItemType> AllowedItemTypes { get; init; } = [];
    public int? EstimatedDurationMinutes { get; init; }
    public bool IncludeAnswerKey { get; init; } = true;
    public bool IncludeFeedback { get; init; } = true;
    public bool IncludeScoring { get; init; } = true;
    public bool IncludeDifferentiation { get; init; }
    public string? TeacherInstructions { get; init; }
    public string? StudentInstructions { get; init; }
    public string? AvailableResources { get; init; }
    public string ConfigurationFingerprint { get; init; } = string.Empty;
    public List<string> Warnings { get; init; } = [];
    public string PromptVersion { get; init; } = string.Empty;
}

public sealed class ClassStructureSummaryForDocuments
{
    public Guid GenerationId { get; init; }
    public string? Title { get; init; }
    public string? Purpose { get; init; }
    public string? StartSummary { get; init; }
    public string? DevelopmentSummary { get; init; }
    public string? ClosureSummary { get; init; }
}
