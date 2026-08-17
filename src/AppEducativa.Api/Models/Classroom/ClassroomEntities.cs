using AppEducativa.Api.Models.Identity;
using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Models.Classroom;

public class Student
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CourseEnrollment
{
    public Guid Id { get; set; }
    public Guid SchoolCourseId { get; set; }
    public Guid StudentId { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public DateOnly EnrolledOn { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EndedOn { get; set; }
    public bool IsDeleted { get; set; }
}

public class StudentSupportPlan
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid InstitutionId { get; set; }
    public SupportPlanType PlanType { get; set; }
    public SpecialEducationalNeedType NeedType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Strategies { get; set; } = string.Empty;
    public string? AccessAdjustments { get; set; }
    public string? ObjectiveAdjustments { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ClassDuaStrategy
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public DuaPrinciple Principle { get; set; }
    public string Strategy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class AttendanceRecord
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid StudentId { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Justification { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

public class LearningAssessment
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid? SchoolCourseId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? PlanningId { get; set; }
    public Guid? ObjectiveLearningId { get; set; }
    public Guid? EducationalDocumentId { get; set; }
    public EvaluationPurpose Purpose { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string? Criteria { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AssessmentScore
{
    public Guid Id { get; set; }
    public Guid LearningAssessmentId { get; set; }
    public Guid StudentId { get; set; }
    public decimal? Score { get; set; }
    public string? AchievementLevel { get; set; }
    public string? Feedback { get; set; }
}

public class ClassFeedbackNote
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid? StudentId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
