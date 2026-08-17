using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Models.Planning;

public class PlanningScheduleConfiguration
{
    public Guid Id { get; set; }
    public Guid PlanningId { get; set; }
    public string TimeZoneId { get; set; } = "America/Santiago";
    public int DefaultClassDurationMinutes { get; set; } = 90;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IncludeStartDate { get; set; } = true;
    public bool IncludeEndDate { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    public Planificacion? Planning { get; set; }
    public ICollection<WeeklyClassSchedule> WeeklySchedules { get; set; } = [];
    public ICollection<PlanningExcludedDate> ExcludedDates { get; set; } = [];
}

public class WeeklyClassSchedule
{
    public Guid Id { get; set; }
    public Guid PlanningScheduleConfigurationId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public int SessionsPerDay { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public int Order { get; set; }

    public PlanningScheduleConfiguration? Configuration { get; set; }
}

public class PlanningExcludedDate
{
    public Guid Id { get; set; }
    public Guid PlanningScheduleConfigurationId { get; set; }
    public DateOnly Date { get; set; }
    public string Reason { get; set; } = string.Empty;
    public PlanningExclusionType ExclusionType { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PlanningScheduleConfiguration? Configuration { get; set; }
}

public class PlanningCalendarSession
{
    public Guid Id { get; set; }
    public Guid PlanningId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public int DurationMinutes { get; set; } = 90;
    public int SessionNumber { get; set; }
    public PlanningSessionStatus Status { get; set; } = PlanningSessionStatus.Available;
    public PlanningSessionSource Source { get; set; } = PlanningSessionSource.Automatic;
    public Guid? ClassId { get; set; }
    public bool IsLocked { get; set; }
    public string? LockReason { get; set; }
    public string? CancelReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    public Planificacion? Planning { get; set; }
    public Clase? Class { get; set; }
    public ICollection<PlanningSessionHistory> History { get; set; } = [];
}

public class PlanningSessionHistory
{
    public Guid Id { get; set; }
    public Guid PlanningCalendarSessionId { get; set; }
    public DateOnly PreviousDate { get; set; }
    public DateOnly NewDate { get; set; }
    public TimeOnly? PreviousStartTime { get; set; }
    public TimeOnly? NewStartTime { get; set; }
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? ChangedBy { get; set; }

    public PlanningCalendarSession? Session { get; set; }
}

public class PlanningSequenceProposal
{
    public Guid Id { get; set; }
    public Guid PlanningId { get; set; }
    public int ProposalNumber { get; set; }
    public PlanningSequenceProposalStatus Status { get; set; } = PlanningSequenceProposalStatus.Draft;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public string ConfigurationJson { get; set; } = "{}";
    public string SummaryJson { get; set; } = "{}";
    public string WarningJson { get; set; } = "[]";
    public bool IsCurrent { get; set; } = true;
    public string PlanningVersionHash { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    public Planificacion? Planning { get; set; }
    public ICollection<PlanningSequenceProposalItem> Items { get; set; } = [];
}

public class PlanningSequenceProposalItem
{
    public Guid Id { get; set; }
    public Guid PlanningSequenceProposalId { get; set; }
    public int Order { get; set; }
    public Guid CalendarSessionId { get; set; }
    public Guid? ObjectiveLearningId { get; set; }
    public string BloomLevel { get; set; } = "Recordar";
    public string? SuggestedTitle { get; set; }
    public string? SuggestedPurpose { get; set; }
    public string SuggestedIndicatorIdsJson { get; set; } = "[]";
    public string SuggestedSkillIdsJson { get; set; } = "[]";
    public string SuggestedAttitudeIdsJson { get; set; } = "[]";
    public string SuggestedTransversalObjectiveIdsJson { get; set; } = "[]";
    public PlanningClassType ClassType { get; set; } = PlanningClassType.Regular;
    public bool IsLocked { get; set; }
    public bool WasManuallyModified { get; set; }
    public string WarningJson { get; set; } = "[]";

    public PlanningSequenceProposal? Proposal { get; set; }
    public PlanningCalendarSession? CalendarSession { get; set; }
    public ICollection<PlanningSequenceItemIndicator> Indicators { get; set; } = [];
}

public class PlanningSequenceItemIndicator
{
    public Guid PlanningSequenceProposalItemId { get; set; }
    public Guid EvaluationIndicatorId { get; set; }
    public IndicatorUsageType UsageType { get; set; }
    public decimal Weight { get; set; } = 1;
    public bool IsPrimary { get; set; }

    public PlanningSequenceProposalItem? Item { get; set; }
}

public class PlanningAlert
{
    public Guid Id { get; set; }
    public Guid PlanningId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? ObjectiveId { get; set; }
    public Guid? IndicatorId { get; set; }
    public string AlertCode { get; set; } = string.Empty;
    public PlanningAlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string? ContextJson { get; set; }
}

public class LearningObjectiveDependency
{
    public Guid Id { get; set; }
    public Guid? PlanningId { get; set; }
    public Guid PrerequisiteObjectiveId { get; set; }
    public Guid DependentObjectiveId { get; set; }
    public ObjectiveDependencyType DependencyType { get; set; }
    public bool IsMandatory { get; set; }
    public ObjectiveDependencySource Source { get; set; }
    public string? Notes { get; set; }
}

public class ClassLearningEvidence
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid? EvaluationIndicatorId { get; set; }
    public LearningEvidenceType EvidenceType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = "Teacher";
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public Clase? Class { get; set; }
}

public class PlanningSuggestionState
{
    public Guid Id { get; set; }
    public Guid PlanningId { get; set; }
    public string SuggestionCode { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public bool IsIgnored { get; set; }
    public bool IsApplied { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AppliedAt { get; set; }
}
