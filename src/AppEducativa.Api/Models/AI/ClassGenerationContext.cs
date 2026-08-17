namespace AppEducativa.Api.Models.AI;

/// <summary>Internal curriculum + classroom context used to build Gemini prompts.</summary>
public sealed class ClassGenerationContext
{
    public Guid ClassId { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string? Axis { get; init; }
    public CurriculumObjectiveRef Objective { get; init; } = new();
    public List<CurriculumIndicatorRef> Indicators { get; init; } = [];
    public List<CurriculumSkillRef> Skills { get; init; } = [];
    public List<CurriculumAttitudeRef> Attitudes { get; init; } = [];
    public List<CurriculumTransversalRef> TransversalObjectives { get; init; } = [];
    public string BloomLevel { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public string? PreviousKnowledge { get; init; }
    public string? AvailableResources { get; init; }
    public string? StudentContext { get; init; }
    public string? TeacherInstructions { get; init; }
    public string CurriculumRelease { get; init; } = string.Empty;
    public Guid? SnapshotId { get; init; }
    public string ConfigurationFingerprint { get; init; } = string.Empty;
    public bool IncludeFormativeAssessment { get; init; } = true;
    public bool IncludeDifferentiation { get; init; } = true;
    public List<string> Warnings { get; init; } = [];
}

public sealed class CurriculumObjectiveRef
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public sealed class CurriculumIndicatorRef
{
    public Guid Id { get; init; }
    public string? Code { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed class CurriculumSkillRef
{
    public Guid Id { get; init; }
    public string? Code { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed class CurriculumAttitudeRef
{
    public Guid Id { get; init; }
    public string? Code { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed class CurriculumTransversalRef
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
