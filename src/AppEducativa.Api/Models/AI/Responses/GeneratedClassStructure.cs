namespace AppEducativa.Api.Models.AI.Responses;

public sealed class GeneratedClassStructure
{
    public bool RequiresReview { get; set; }
    public List<string> Warnings { get; set; } = [];
    public GeneratedCurriculumReference Curriculum { get; set; } = new();
    public GeneratedClassBody Class { get; set; } = new();
}

public sealed class GeneratedCurriculumReference
{
    public Guid ObjectiveId { get; set; }
    public string ObjectiveCode { get; set; } = string.Empty;
    public List<Guid> IndicatorIds { get; set; } = [];
    public List<Guid> SkillIds { get; set; } = [];
    public List<Guid> AttitudeIds { get; set; } = [];
    public List<Guid> TransversalObjectiveIds { get; set; } = [];
    public string CurriculumRelease { get; set; } = string.Empty;
}

public sealed class GeneratedClassBody
{
    public string Title { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public int TotalDurationMinutes { get; set; }
    public GeneratedClassPhase Start { get; set; } = new();
    public GeneratedClassPhase Development { get; set; } = new();
    public GeneratedClassPhase Closure { get; set; } = new();
    public GeneratedFormativeAssessment FormativeAssessment { get; set; } = new();
    public GeneratedDifferentiation Differentiation { get; set; } = new();
}

public sealed class GeneratedClassPhase
{
    public int DurationMinutes { get; set; }
    public string Objective { get; set; } = string.Empty;
    public List<string> TeacherActions { get; set; } = [];
    public List<string> StudentActions { get; set; } = [];
    public List<GeneratedActivity> Activities { get; set; } = [];
    public List<string> Resources { get; set; } = [];
    public List<string> Evidence { get; set; } = [];
}

public sealed class GeneratedActivity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; }
}

public sealed class GeneratedFormativeAssessment
{
    public bool Included { get; set; }
    public string Strategy { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public string FeedbackMethod { get; set; } = string.Empty;
}

public sealed class GeneratedDifferentiation
{
    public bool Included { get; set; }
    public List<string> SupportActions { get; set; } = [];
    public List<string> ExtensionActions { get; set; } = [];
    public List<string> AccessibilityConsiderations { get; set; } = [];
}
