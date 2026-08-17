using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Shared.Dtos;

public sealed class ConfigurePlanningScheduleRequest
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public string TimeZoneId { get; init; } = "America/Santiago";
    public int DefaultClassDurationMinutes { get; init; } = 90;
    public bool UpdatePlanningDates { get; init; } = true;
    public IReadOnlyList<WeeklyScheduleRequest> WeeklySchedule { get; init; } = [];
    public IReadOnlyList<AddExcludedDateRequest> ExcludedDates { get; init; } = [];
}

public sealed class WeeklyScheduleRequest
{
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public int DurationMinutes { get; init; } = 90;
    public int SessionsPerDay { get; init; } = 1;
    public bool IsActive { get; init; } = true;
}

public sealed class AddExcludedDateRequest
{
    public DateOnly Date { get; init; }
    public string Reason { get; init; } = string.Empty;
    public PlanningExclusionType ExclusionType { get; init; } = PlanningExclusionType.Holiday;
    public bool IsRecurring { get; init; }
}

public sealed class GenerateCalendarSessionsRequest
{
    public bool PreviewOnly { get; init; }
    public bool ConfirmDestructiveChanges { get; init; }
    public bool PreserveManualSessions { get; init; } = true;
    public bool PreserveLockedSessions { get; init; } = true;
}

public sealed class CreateManualSessionRequest
{
    public DateOnly ScheduledDate { get; init; }
    public TimeOnly? StartTime { get; init; }
    public int DurationMinutes { get; init; } = 90;
    public string? Notes { get; init; }
}

public sealed class RescheduleSessionRequest
{
    public DateOnly NewDate { get; init; }
    public TimeOnly? NewStartTime { get; init; }
    public string? Reason { get; init; }
    public byte[]? RowVersion { get; init; }
}

public sealed class CancelPlanningSessionRequest
{
    public string Reason { get; init; } = string.Empty;
}

public sealed class LockSessionRequest
{
    public string? LockReason { get; init; }
}

public sealed class GeneratePlanningSequenceRequest
{
    public IReadOnlyList<ObjectiveCoverageRequest> Objectives { get; init; } = [];
    public BloomProgressionSettingsRequest BloomProgression { get; init; } = new();
    public bool IncludeDiagnosticClass { get; init; }
    public bool IncludeReviewClasses { get; init; } = true;
    public bool IncludeAssessmentClass { get; init; } = true;
    public int ReviewClassCount { get; init; } = 1;
    public int AssessmentClassCount { get; init; } = 1;
    public bool BalanceIndicators { get; init; } = true;
    public bool RespectExistingClasses { get; init; } = true;
    public bool PreserveLockedSessions { get; init; } = true;
    public string? TeacherInstructions { get; init; }
}

public sealed class ObjectiveCoverageRequest
{
    public Guid ObjectiveId { get; init; }
    public int MinimumSessions { get; init; } = 1;
    public int? MaximumSessions { get; init; }
    public int Priority { get; init; } = 1;
    public IReadOnlyList<Guid> IndicatorIds { get; init; } = [];
}

public sealed class BloomProgressionSettingsRequest
{
    public NivelBloom InitialLevel { get; init; } = NivelBloom.Recordar;
    public NivelBloom TargetLevel { get; init; } = NivelBloom.Aplicar;
    public bool AllowRegression { get; init; } = true;
    public int MaximumLevelJump { get; init; } = 1;
    public bool RequireFinalHigherOrderActivity { get; init; }
    public IReadOnlyList<NivelBloom>? AllowedLevels { get; init; }
}

public sealed class UpdatePlanningSequenceItemRequest
{
    public Guid? ObjectiveLearningId { get; init; }
    public string? BloomLevel { get; init; }
    public string? SuggestedTitle { get; init; }
    public string? SuggestedPurpose { get; init; }
    public PlanningClassType? ClassType { get; init; }
    public IReadOnlyList<Guid>? IndicatorIds { get; init; }
    public int? Order { get; init; }
}

public sealed class CompleteClassRequest
{
    public DateOnly? ActualDate { get; init; }
    public int? ActualDurationMinutes { get; init; }
    public string? Observation { get; init; }
    public IReadOnlyList<Guid> WorkedObjectiveIds { get; init; } = [];
    public IReadOnlyList<Guid> EvidencedIndicatorIds { get; init; } = [];
    public IReadOnlyList<RecordLearningEvidenceRequest> Evidences { get; init; } = [];
}

public sealed class RecordLearningEvidenceRequest
{
    public LearningEvidenceType EvidenceType { get; init; }
    public string Description { get; init; } = string.Empty;
    public Guid? EvaluationIndicatorId { get; init; }
    public string Source { get; init; } = "Teacher";
    public string? Notes { get; init; }
}

public sealed class RejectSequenceProposalRequest
{
    public string Reason { get; init; } = string.Empty;
}

public sealed class ImportExcludedDatesRequest
{
    public IReadOnlyList<AddExcludedDateRequest> Dates { get; init; } = [];
}

public sealed class PlanningCalendarDto
{
    public Guid PlanningId { get; set; }
    public PlanningScheduleConfigurationDto? Configuration { get; set; }
    public IReadOnlyList<PlanningCalendarSessionDto> Sessions { get; set; } = [];
    public int AvailableSessionCount { get; set; }
    public int AssignedSessionCount { get; set; }
    public int CancelledSessionCount { get; set; }
    public IReadOnlyList<string> Warnings { get; set; } = [];
    public PlanningCalendarRegenerationPreviewDto? Preview { get; set; }
}

public sealed class PlanningScheduleConfigurationDto
{
    public Guid Id { get; init; }
    public Guid PlanningId { get; init; }
    public string TimeZoneId { get; init; } = "America/Santiago";
    public int DefaultClassDurationMinutes { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public IReadOnlyList<WeeklyScheduleDto> WeeklySchedule { get; init; } = [];
    public IReadOnlyList<ExcludedDateDto> ExcludedDates { get; init; } = [];
}

public sealed class WeeklyScheduleDto
{
    public Guid Id { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public int DurationMinutes { get; init; }
    public int SessionsPerDay { get; init; }
    public bool IsActive { get; init; }
}

public sealed class ExcludedDateDto
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public string Reason { get; init; } = string.Empty;
    public PlanningExclusionType ExclusionType { get; init; }
}

public sealed class PlanningCalendarSessionDto
{
    public Guid Id { get; init; }
    public Guid PlanningId { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public TimeOnly? StartTime { get; init; }
    public int DurationMinutes { get; init; }
    public int SessionNumber { get; init; }
    public PlanningSessionStatus Status { get; init; }
    public PlanningSessionSource Source { get; init; }
    public Guid? ClassId { get; init; }
    public bool IsLocked { get; init; }
    public string? LockReason { get; init; }
    public string? Title { get; init; }
    public string? ObjectiveCode { get; init; }
    public string? BloomLevel { get; init; }
    public PlanningClassType? ClassType { get; init; }
    public byte[]? RowVersion { get; init; }
    public IReadOnlyList<string> Alerts { get; init; } = [];
}

public sealed class PlanningCalendarRegenerationPreviewDto
{
    public int NewSessions { get; init; }
    public int UnchangedSessions { get; init; }
    public int MovedSessions { get; init; }
    public int RemovableSessions { get; init; }
    public int ConflictSessions { get; init; }
    public int ProtectedSessions { get; init; }
    public IReadOnlyList<string> Messages { get; init; } = [];
    public bool CanApplySafely { get; init; }
}

public sealed class PlanningSequenceProposalDto
{
    public Guid Id { get; init; }
    public Guid PlanningId { get; init; }
    public int ProposalNumber { get; init; }
    public PlanningSequenceProposalStatus Status { get; init; }
    public DateTime GeneratedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsOutdated { get; init; }
    public IReadOnlyList<PlanningSequenceProposalItemDto> Items { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public SequenceDeficitDto? Deficit { get; init; }
    public string? SummaryJson { get; init; }
}

public sealed class PlanningSequenceProposalItemDto
{
    public Guid Id { get; init; }
    public int Order { get; init; }
    public Guid CalendarSessionId { get; init; }
    public DateOnly? ScheduledDate { get; init; }
    public Guid? ObjectiveLearningId { get; init; }
    public string? ObjectiveCode { get; init; }
    public string BloomLevel { get; init; } = "Recordar";
    public string? SuggestedTitle { get; init; }
    public string? SuggestedPurpose { get; init; }
    public PlanningClassType ClassType { get; init; }
    public bool IsLocked { get; init; }
    public bool WasManuallyModified { get; init; }
    public IReadOnlyList<Guid> IndicatorIds { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class SequenceDeficitDto
{
    public int AvailableSessions { get; init; }
    public int RequiredMinimumSessions { get; init; }
    public int Deficit { get; init; }
    public IReadOnlyList<string> Alternatives { get; init; } = [];
}

public sealed class PlanningSequenceValidationDto
{
    public Guid ProposalId { get; init; }
    public bool IsValid { get; init; }
    public bool CanConfirm { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class PlanningCoverageDto
{
    public Guid PlanningId { get; init; }
    public string Mode { get; init; } = "Planned";
    public int AvailableSessions { get; init; }
    public int UsedSessions { get; init; }
    public int FreeSessions { get; init; }
    public int SelectedObjectives { get; init; }
    public int CoveredObjectives { get; init; }
    public int SelectedIndicators { get; init; }
    public int CoveredIndicators { get; init; }
    public int ClassesWithStructure { get; init; }
    public int ClassesWithMaterials { get; init; }
    public int Assessments { get; init; }
    public int BlockingAlerts { get; init; }
    public IReadOnlyList<ObjectiveCoverageDto> Objectives { get; init; } = [];
    public IReadOnlyList<IndicatorCoverageDto> Indicators { get; init; } = [];
    public IReadOnlyList<BloomDistributionDto> BloomDistribution { get; init; } = [];
    public CoverageMatrixDto? Matrix { get; init; }
}

public sealed class ObjectiveCoverageDto
{
    public Guid ObjectiveId { get; init; }
    public string Code { get; init; } = string.Empty;
    public int AssignedSessions { get; init; }
    public int MinimumSessions { get; init; }
    public int? MaximumSessions { get; init; }
    public decimal CoveragePercent { get; init; }
    public string? InitialBloom { get; init; }
    public string? MaxBloom { get; init; }
    public bool HasAssessment { get; init; }
    public PlanningCoverageStatus Status { get; init; }
    public bool Assigned { get; init; }
    public bool Planned { get; init; }
    public bool Worked { get; init; }
    public bool Evidenced { get; init; }
    public bool Evaluated { get; init; }
}

public sealed class IndicatorCoverageDto
{
    public Guid IndicatorId { get; init; }
    public string Code { get; init; } = string.Empty;
    public Guid ObjectiveId { get; init; }
    public int AssociatedClasses { get; init; }
    public IReadOnlyList<IndicatorUsageType> UsageTypes { get; init; } = [];
    public bool HasFormative { get; init; }
    public bool HasSummative { get; init; }
    public PlanningCoverageStatus Status { get; init; }
}

public sealed class BloomDistributionDto
{
    public string BloomLevel { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class CoverageMatrixDto
{
    public IReadOnlyList<string> ClassLabels { get; init; } = [];
    public IReadOnlyList<CoverageMatrixRowDto> Rows { get; init; } = [];
}

public sealed class CoverageMatrixRowDto
{
    public string Label { get; init; } = string.Empty;
    public string Kind { get; init; } = "OA";
    public Guid? EntityId { get; init; }
    public IReadOnlyList<string> Cells { get; init; } = [];
}

public sealed class PlanningAlertDto
{
    public Guid Id { get; init; }
    public Guid PlanningId { get; init; }
    public Guid? ClassId { get; init; }
    public Guid? ObjectiveId { get; init; }
    public Guid? IndicatorId { get; init; }
    public string AlertCode { get; init; } = string.Empty;
    public PlanningAlertSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsResolved { get; init; }
    public DateTime GeneratedAt { get; init; }
}

public sealed class PlanningSuggestionDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
    public PlanningAlertSeverity Severity { get; init; }
    public bool CanApplyAutomatically { get; init; }
    public string ProposedAction { get; init; } = string.Empty;
    public IReadOnlyList<Guid> AffectedClassIds { get; init; } = [];
    public string? Preview { get; init; }
}

public sealed class ApplySuggestionRequest
{
    public bool Confirm { get; init; } = true;
}
