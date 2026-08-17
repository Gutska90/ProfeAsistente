using AppEducativa.Shared.Enums;

namespace AppEducativa.Shared.Dtos;

public sealed class GenerateClassStructureRequest
{
    public int DurationMinutes { get; set; } = 90;
    public List<Guid> EvaluationIndicatorIds { get; set; } = [];
    public List<Guid> TransversalObjectiveIds { get; set; } = [];
    public string? PreviousKnowledge { get; set; }
    public string? AvailableResources { get; set; }
    public string? StudentContext { get; set; }
    public string? TeacherInstructions { get; set; }
    public bool IncludeFormativeAssessment { get; set; } = true;
    public bool IncludeDifferentiation { get; set; } = true;
}

public sealed class ClassStructureGenerationResultDto
{
    public Guid GenerationId { get; set; }
    public Guid ClassId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool RequiresReview { get; set; }
    public List<string> Warnings { get; set; } = [];
    public ClassStructureContentDto? Structure { get; set; }
    public ClassStructureCurriculumDto? Curriculum { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsOutdated { get; set; }
    public bool IsCurrentVersion { get; set; }
    public int GenerationNumber { get; set; }
}

public sealed class ClassStructureContentDto
{
    public string Title { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public int TotalDurationMinutes { get; set; }
    public ClassPhaseDto Start { get; set; } = new();
    public ClassPhaseDto Development { get; set; } = new();
    public ClassPhaseDto Closure { get; set; } = new();
    public FormativeAssessmentDto FormativeAssessment { get; set; } = new();
    public DifferentiationDto Differentiation { get; set; } = new();
}

public sealed class ClassPhaseDto
{
    public int DurationMinutes { get; set; }
    public string Objective { get; set; } = string.Empty;
    public List<string> TeacherActions { get; set; } = [];
    public List<string> StudentActions { get; set; } = [];
    public List<ClassActivityDto> Activities { get; set; } = [];
    public List<string> Resources { get; set; } = [];
    public List<string> Evidence { get; set; } = [];
}

public sealed class ClassActivityDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; }
}

public sealed class FormativeAssessmentDto
{
    public bool Included { get; set; }
    public string Strategy { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public string FeedbackMethod { get; set; } = string.Empty;
}

public sealed class DifferentiationDto
{
    public bool Included { get; set; }
    public List<string> SupportActions { get; set; } = [];
    public List<string> ExtensionActions { get; set; } = [];
    public List<string> AccessibilityConsiderations { get; set; } = [];
}

public sealed class ClassStructureCurriculumDto
{
    public Guid ObjectiveId { get; set; }
    public string ObjectiveCode { get; set; } = string.Empty;
    public List<Guid> IndicatorIds { get; set; } = [];
    public List<Guid> SkillIds { get; set; } = [];
    public List<Guid> AttitudeIds { get; set; } = [];
    public List<Guid> TransversalObjectiveIds { get; set; } = [];
    public string CurriculumRelease { get; set; } = string.Empty;
}

public sealed class UpdateClassStructureContentRequest
{
    public string? RowVersion { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public ClassPhaseDto Start { get; set; } = new();
    public ClassPhaseDto Development { get; set; } = new();
    public ClassPhaseDto Closure { get; set; } = new();
    public FormativeAssessmentDto? FormativeAssessment { get; set; }
    public DifferentiationDto? Differentiation { get; set; }
    public string? ChangeSummary { get; set; }
}

public sealed class ClassGenerationContextDto
{
    public string Level { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string ObjectiveCode { get; set; } = string.Empty;
    public string ObjectiveDescription { get; set; } = string.Empty;
    public List<string> Indicators { get; set; } = [];
    public List<string> Skills { get; set; } = [];
    public List<string> Attitudes { get; set; } = [];
    public List<string> TransversalObjectives { get; set; } = [];
    public string BloomLevel { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string CurriculumRelease { get; set; } = string.Empty;
    public Guid? SnapshotId { get; set; }
}

public sealed class ClassStructureGenerationSummaryDto
{
    public Guid Id { get; set; }
    public int GenerationNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsCurrentVersion { get; set; }
    public bool IsOutdated { get; set; }
    public bool RequiresReview { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
}
