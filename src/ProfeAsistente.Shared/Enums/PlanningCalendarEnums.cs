namespace ProfeAsistente.Shared.Enums;

public enum PlanningSessionStatus
{
    Available = 0,
    Assigned = 1,
    Planned = 2,
    Completed = 3,
    Cancelled = 4,
    Rescheduled = 5,
    Excluded = 6
}

public enum PlanningSessionSource
{
    Automatic = 0,
    Manual = 1,
    Imported = 2
}

public enum PlanningExclusionType
{
    Holiday = 0,
    SchoolActivity = 1,
    TeacherAbsence = 2,
    Suspension = 3,
    Vacation = 4,
    Other = 5
}

public enum PlanningSequenceProposalStatus
{
    Draft = 0,
    Validated = 1,
    Confirmed = 2,
    Rejected = 3,
    Superseded = 4
}

public enum PlanningCoverageStatus
{
    NotStarted = 0,
    Partial = 1,
    Covered = 2,
    Overrepresented = 3,
    Missing = 4,
    Conflict = 5
}

public enum PlanningAlertSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2,
    Blocking = 3
}

public enum ObjectiveDependencyType
{
    Prerequisite = 0,
    RecommendedBefore = 1,
    Related = 2,
    Parallel = 3
}

public enum IndicatorUsageType
{
    Introduction = 0,
    Practice = 1,
    FormativeAssessment = 2,
    SummativeAssessment = 3,
    Review = 4
}

public enum PlanningClassType
{
    Regular = 0,
    Diagnostic = 1,
    Introduction = 2,
    Practice = 3,
    Review = 4,
    FormativeAssessment = 5,
    SummativeAssessment = 6,
    Project = 7,
    Remedial = 8,
    Other = 9
}

public enum LearningEvidenceType
{
    Observation = 0,
    StudentWork = 1,
    ExitTicket = 2,
    Exercise = 3,
    Guide = 4,
    FormativeAssessment = 5,
    SummativeAssessment = 6,
    Project = 7,
    Other = 8
}

public enum ObjectiveDependencySource
{
    Curriculum = 0,
    Teacher = 1,
    InferredSuggestion = 2
}
