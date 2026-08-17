using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Shared.Dtos;

public sealed class CreateStudentRequest
{
    public Guid InstitutionId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? Notes { get; init; }
}

public sealed class StudentDto
{
    public Guid Id { get; init; }
    public Guid InstitutionId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool HasActiveSupportPlan { get; init; }
}

public sealed class EnrollStudentRequest
{
    public Guid StudentId { get; init; }
}

public sealed class CreateSupportPlanRequest
{
    public SupportPlanType PlanType { get; init; }
    public SpecialEducationalNeedType NeedType { get; init; } = SpecialEducationalNeedType.None;
    public required string Title { get; init; }
    public required string Strategies { get; init; }
    public string? AccessAdjustments { get; init; }
    public string? ObjectiveAdjustments { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
}

public sealed class SupportPlanDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public SupportPlanType PlanType { get; init; }
    public SpecialEducationalNeedType NeedType { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Strategies { get; init; } = string.Empty;
    public string? AccessAdjustments { get; init; }
    public string? ObjectiveAdjustments { get; init; }
    public bool IsActive { get; init; }
}

public sealed class AddClassDuaStrategyRequest
{
    public DuaPrinciple Principle { get; init; }
    public required string Strategy { get; init; }
    public string? Notes { get; init; }
}

public sealed class ClassDuaStrategyDto
{
    public Guid Id { get; init; }
    public DuaPrinciple Principle { get; init; }
    public string Strategy { get; init; } = string.Empty;
}

public sealed class AttendanceEntryRequest
{
    public Guid StudentId { get; init; }
    public AttendanceStatus Status { get; init; }
    public string? Justification { get; init; }
}

public sealed class SaveAttendanceRequest
{
    public IReadOnlyList<AttendanceEntryRequest> Entries { get; init; } = [];
}

public sealed class AttendanceRecordDto
{
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public AttendanceStatus Status { get; init; }
    public string? Justification { get; init; }
}

public sealed class CreateLearningAssessmentRequest
{
    public Guid InstitutionId { get; init; }
    public Guid? SchoolCourseId { get; init; }
    public Guid? ClassId { get; init; }
    public Guid? PlanningId { get; init; }
    public Guid? ObjectiveLearningId { get; init; }
    public Guid? EducationalDocumentId { get; init; }
    public EvaluationPurpose Purpose { get; init; } = EvaluationPurpose.Formative;
    public required string Name { get; init; }
    public DateOnly Date { get; init; }
    public string? Criteria { get; init; }
}

public sealed class LearningAssessmentDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public EvaluationPurpose Purpose { get; init; }
    public DateOnly Date { get; init; }
    public Guid? ClassId { get; init; }
    public Guid? SchoolCourseId { get; init; }
    public Guid? ObjectiveLearningId { get; init; }
    public Guid? EducationalDocumentId { get; init; }
    public string? ObjectiveCode { get; init; }
    public string? ObjectiveDescription { get; init; }
    public string? Criteria { get; init; }
}

public sealed class AssessmentScoreDto
{
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public decimal? Score { get; init; }
    public string? AchievementLevel { get; init; }
    public string? Feedback { get; init; }
}

public sealed class SaveAssessmentScoreRequest
{
    public Guid StudentId { get; init; }
    public decimal? Score { get; init; }
    public string? AchievementLevel { get; init; }
    public string? Feedback { get; init; }
}

/// <summary>Lectura de evidencia de una evaluación alineada al OA (P3).</summary>
public sealed class AssessmentEvidenceSummaryDto
{
    public Guid AssessmentId { get; init; }
    public string AssessmentName { get; init; } = string.Empty;
    public Guid? ClassId { get; init; }
    public Guid? ObjectiveId { get; init; }
    public string ObjectiveCode { get; init; } = string.Empty;
    public string ObjectiveDescription { get; init; } = string.Empty;
    public string PurposeLabel { get; init; } = string.Empty;
    public int StudentsTotal { get; init; }
    public int StudentsWithLevel { get; init; }
    public int CountPorLograr { get; init; }
    public int CountMedianamente { get; init; }
    public int CountLogrado { get; init; }
    public decimal? AverageScore { get; init; }
    public bool NeedsReinforcement { get; init; }
    public string ReadingSummary { get; init; } = string.Empty;
    public IReadOnlyList<string> Indicators { get; init; } = [];
    public IReadOnlyList<AssessmentSpecificationRowDto> SpecificationTable { get; init; } = [];
    public Guid? EducationalDocumentId { get; init; }
    public IReadOnlyList<string> StudentsNeedingSupport { get; init; } = [];
}

public sealed class TeacherDashboardDto
{
    public string TeacherName { get; init; } = string.Empty;
    public string Greeting { get; init; } = string.Empty;
    public string? InstitutionName { get; init; }
    public DateOnly Today { get; init; }
    public int ActivePlannings { get; init; }
    public int UpcomingClasses { get; init; }
    public int PendingClasses { get; init; }
    public int OpenCoverageAlerts { get; init; }
    public int StudentsWithSupportPlans { get; init; }
    /// <summary>Clases del día (Chile / reloj local del servidor).</summary>
    public IReadOnlyList<UpcomingClassDto> TodayClasses { get; init; } = [];
    /// <summary>Próximas clases (incluye hoy y siguientes).</summary>
    public IReadOnlyList<UpcomingClassDto> NextClasses { get; init; } = [];
    public IReadOnlyList<TeacherPendingItemDto> PendingItems { get; init; } = [];
    public IReadOnlyList<string> Reminders { get; init; } = [];
}

public sealed class TeacherPendingItemDto
{
    public string Kind { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
}

public sealed class UpcomingClassDto
{
    public Guid ClassId { get; init; }
    public Guid PlanningId { get; init; }
    public Guid? SchoolCourseId { get; init; }
    public string PlanningName { get; init; } = string.Empty;
    public string CourseDisplayName { get; init; } = string.Empty;
    public string SubjectName { get; init; } = string.Empty;
    public string UnitName { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public string ObjectiveCode { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public string TitleLine => string.IsNullOrWhiteSpace(CourseDisplayName)
        ? PlanningName
        : $"{CourseDisplayName} · {SubjectName}".Trim(' ', '·');
}

public sealed class CourseRosterDto
{
    public Guid CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public IReadOnlyList<RosterStudentDto> Students { get; init; } = [];
}

public sealed class RosterStudentDto
{
    public Guid StudentId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public EnrollmentStatus Status { get; init; }
    public bool HasActiveSupportPlan { get; init; }
}
