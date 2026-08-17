using AppEducativa.Shared.Enums;

namespace AppEducativa.Shared.Dtos;

public sealed class LoginRequest
{
    public required string UserNameOrEmail { get; init; }
    public required string Password { get; init; }
    public bool RememberMe { get; init; }
    public Guid? InstitutionId { get; init; }
}

public sealed class RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}

public sealed class ChangePasswordRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}

public sealed class ForgotPasswordRequest
{
    public required string UserNameOrEmail { get; init; }
}

public sealed class ResetPasswordRequest
{
    public required string UserNameOrEmail { get; init; }
    public required string ResetToken { get; init; }
    public required string NewPassword { get; init; }
}

public sealed class CreateUserRequest
{
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public bool MustChangePassword { get; init; } = true;
}

public sealed class UpdateUserRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? PreferredTimeZone { get; init; }
    public string? PreferredLanguage { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class AssignRolesRequest
{
    public IReadOnlyList<string> Roles { get; init; } = [];
}

public sealed class AuthenticationResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset AccessTokenExpiresAt { get; init; }
    public required UserSessionDto User { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = [];
    public bool MustChangePassword { get; init; }
    /// <summary>Solo en desarrollo: token de reset visible. Nunca en producción.</summary>
    public string? DevelopmentResetToken { get; init; }
}

public sealed class UserSessionDto
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string PreferredTimeZone { get; init; } = "America/Santiago";
    public string PreferredLanguage { get; init; } = "es-CL";
    public Guid? ActiveInstitutionId { get; init; }
    public string? ActiveInstitutionName { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = [];
    public IReadOnlyList<InstitutionMembershipDto> Memberships { get; init; } = [];
}

public sealed class UserSummaryDto
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public DateTime? LastLoginAt { get; init; }
}

public sealed class ForgotPasswordResponse
{
    public string Message { get; set; } = "Si la cuenta existe, se generó una solicitud de recuperación.";
    public string? DevelopmentResetToken { get; set; }
}

public sealed class AuthSessionDto
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? RevokedAt { get; init; }
    public string? UserAgent { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed class CreateInstitutionRequest
{
    public required string Name { get; init; }
    public string? ShortName { get; init; }
    public string? Rbd { get; init; }
    public EducationalInstitutionType InstitutionType { get; init; } = EducationalInstitutionType.Other;
    public string? Address { get; init; }
    public string? Commune { get; init; }
    public string? Region { get; init; }
    public string Country { get; init; } = "Chile";
    public string TimeZoneId { get; init; } = "America/Santiago";
}

public sealed class UpdateInstitutionRequest
{
    public string? Name { get; init; }
    public string? ShortName { get; init; }
    public string? Rbd { get; init; }
    public EducationalInstitutionType? InstitutionType { get; init; }
    public string? Address { get; init; }
    public string? Commune { get; init; }
    public string? Region { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class InstitutionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ShortName { get; init; }
    public string? Rbd { get; init; }
    public EducationalInstitutionType InstitutionType { get; init; }
    public string Country { get; init; } = "Chile";
    public string TimeZoneId { get; init; } = "America/Santiago";
    public bool IsActive { get; init; }
}

public sealed class AddMembershipRequest
{
    public Guid UserId { get; init; }
    public ApplicationRole Role { get; init; } = ApplicationRole.Teacher;
    public string? Notes { get; init; }
}

public sealed class InstitutionMembershipDto
{
    public Guid Id { get; init; }
    public Guid InstitutionId { get; init; }
    public string InstitutionName { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string? UserDisplayName { get; init; }
    public ApplicationRole Role { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CreateAcademicPeriodRequest
{
    public required string Name { get; init; }
    public int Year { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed class AcademicPeriodDto
{
    public Guid Id { get; init; }
    public Guid InstitutionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Year { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public AcademicPeriodStatus Status { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed class CreateSchoolCourseRequest
{
    public Guid AcademicPeriodId { get; init; }
    public Guid LevelId { get; init; }
    public required string Name { get; init; }
    public string? Section { get; init; }
    public int? Capacity { get; init; }
    public int DefaultClassDurationMinutes { get; init; } = 90;
}

public sealed class SchoolCourseDto
{
    public Guid Id { get; init; }
    public Guid InstitutionId { get; init; }
    public Guid AcademicPeriodId { get; init; }
    public Guid LevelId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Section { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? LevelName { get; init; }
    public string? PrimarySubjectName { get; init; }
    public bool IsActive { get; init; }
    public string Subtitle => string.IsNullOrWhiteSpace(PrimarySubjectName)
        ? (LevelName ?? Name)
        : PrimarySubjectName!;
}

public sealed class CreateCourseSubjectRequest
{
    public Guid SubjectId { get; init; }
    public decimal? WeeklyHours { get; init; }
    public string? Notes { get; init; }
}

public sealed class CourseSubjectDto
{
    public Guid Id { get; init; }
    public Guid SchoolCourseId { get; init; }
    public Guid SubjectId { get; init; }
    public string? SubjectName { get; init; }
    public decimal? WeeklyHours { get; init; }
    public bool IsActive { get; init; }
}

public sealed class AssignTeacherRequest
{
    public Guid UserId { get; init; }
    public TeacherAssignmentType AssignmentType { get; init; } = TeacherAssignmentType.PrimaryTeacher;
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsPrimary { get; init; } = true;
}

public sealed class CourseTeacherAssignmentDto
{
    public Guid Id { get; init; }
    public Guid CourseSubjectId { get; init; }
    public Guid UserId { get; init; }
    public string? UserDisplayName { get; init; }
    public TeacherAssignmentType AssignmentType { get; init; }
    public bool IsPrimary { get; init; }
    public bool IsActive { get; init; }
}

public sealed class SelectInstitutionRequest
{
    public Guid InstitutionId { get; init; }
}

public sealed class AuditEventDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public Guid? InstitutionId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public bool Success { get; init; }
    public DateTime Timestamp { get; init; }
    public string? FailureReason { get; init; }
}

public sealed class TeacherProfileDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? ProfessionalTitle { get; init; }
    public string? Specialization { get; init; }
    public string? Biography { get; init; }
    public int? DefaultClassDurationMinutes { get; init; }
}

public sealed class UpdateTeacherProfileRequest
{
    public string? ProfessionalTitle { get; init; }
    public string? Specialization { get; init; }
    public string? Biography { get; init; }
    public int? DefaultClassDurationMinutes { get; init; }
    public string? PreferredPlanningFormat { get; init; }
}
