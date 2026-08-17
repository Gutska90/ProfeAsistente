using AppEducativa.Api.Models.Curriculum;
using AppEducativa.Api.Models.Identity;
using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Models.Institutions;

public class EducationalInstitution
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? Rbd { get; set; }
    public EducationalInstitutionType InstitutionType { get; set; }
    public string? Address { get; set; }
    public string? Commune { get; set; }
    public string? Region { get; set; }
    public string Country { get; set; } = "Chile";
    public string TimeZoneId { get; set; } = "America/Santiago";
    public string? PeiVision { get; set; }
    public string? PeiSeals { get; set; }
    public string? EvaluationRegulationNotes { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();
}

public class InstitutionMembership
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid UserId { get; set; }
    public ApplicationRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }

    public EducationalInstitution? Institution { get; set; }
    public ApplicationUser? User { get; set; }
}

public class AcademicPeriod
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public AcademicPeriodStatus Status { get; set; } = AcademicPeriodStatus.Draft;
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public EducationalInstitution? Institution { get; set; }
}

public class SchoolCourse
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public Guid LevelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Section { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public int DefaultClassDurationMinutes { get; set; } = 90;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    public EducationalInstitution? Institution { get; set; }
    public AcademicPeriod? AcademicPeriod { get; set; }
    public Nivel? Level { get; set; }
    public ICollection<CourseSubject> Subjects { get; set; } = [];
}

public class CourseSubject
{
    public Guid Id { get; set; }
    public Guid SchoolCourseId { get; set; }
    public Guid SubjectId { get; set; }
    public decimal? WeeklyHours { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public SchoolCourse? SchoolCourse { get; set; }
    public Asignatura? Subject { get; set; }
    public ICollection<CourseTeacherAssignment> Teachers { get; set; } = [];
}

public class CourseTeacherAssignment
{
    public Guid Id { get; set; }
    public Guid CourseSubjectId { get; set; }
    public Guid UserId { get; set; }
    public TeacherAssignmentType AssignmentType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CourseSubject? CourseSubject { get; set; }
    public ApplicationUser? User { get; set; }
}

public class TeacherProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? ProfessionalTitle { get; set; }
    public string? Specialization { get; set; }
    public string? Biography { get; set; }
    public string? PreferredPlanningFormat { get; set; }
    public int? DefaultClassDurationMinutes { get; set; }
    public string? DefaultBloomPreferencesJson { get; set; }
    public string? DefaultExportSettingsJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}

public class UserPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string ValueJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class AuditEvent
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? InstitutionId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? TraceId { get; set; }
    public string? DetailsJson { get; set; }
    public string? FailureReason { get; set; }
}
