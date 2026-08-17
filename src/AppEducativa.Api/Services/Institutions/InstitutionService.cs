using AppEducativa.Api.Data;
using AppEducativa.Api.Models.Identity;
using AppEducativa.Api.Models.Institutions;
using AppEducativa.Api.Services.Auth;
using AppEducativa.Api.Services.Authorization;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using AppEducativa.Shared.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api.Services.Institutions;

public interface IInstitutionService
{
    Task<InstitutionDto> CreateAsync(CreateInstitutionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstitutionDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<InstitutionDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InstitutionDto> UpdateAsync(Guid id, UpdateInstitutionRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default);
    Task<InstitutionMembershipDto> AddMemberAsync(Guid institutionId, AddMembershipRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstitutionMembershipDto>> ListMembersAsync(Guid institutionId, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid institutionId, Guid membershipId, CancellationToken cancellationToken = default);
    Task<AcademicPeriodDto> CreatePeriodAsync(Guid institutionId, CreateAcademicPeriodRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcademicPeriodDto>> ListPeriodsAsync(Guid institutionId, CancellationToken cancellationToken = default);
    Task ActivatePeriodAsync(Guid periodId, CancellationToken cancellationToken = default);
    Task ClosePeriodAsync(Guid periodId, CancellationToken cancellationToken = default);
    Task<SchoolCourseDto> CreateCourseAsync(Guid institutionId, CreateSchoolCourseRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SchoolCourseDto>> ListCoursesAsync(Guid institutionId, CancellationToken cancellationToken = default);
    Task<SchoolCourseDto?> GetCourseAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<CourseSubjectDto> AddSubjectAsync(Guid courseId, CreateCourseSubjectRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseSubjectDto>> ListSubjectsAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<CourseTeacherAssignmentDto> AssignTeacherAsync(Guid courseSubjectId, AssignTeacherRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseTeacherAssignmentDto>> ListTeachersAsync(Guid courseSubjectId, CancellationToken cancellationToken = default);
    Task RemoveTeacherAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default);
}

public interface IUserAdminService
{
    Task<UserSummaryDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSummaryDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<UserSummaryDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserSummaryDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default);
    Task AssignRolesAsync(Guid id, AssignRolesRequest request, CancellationToken cancellationToken = default);
    Task ForcePasswordChangeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string> AdminResetPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default);
}

public sealed class InstitutionService : IInstitutionService
{
    private readonly AppEducativaDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditService _audit;

    public InstitutionService(AppEducativaDbContext db, ICurrentUserService current, IAuditService audit)
    {
        _db = db;
        _current = current;
        _audit = audit;
    }

    public async Task<InstitutionDto> CreateAsync(CreateInstitutionRequest request, CancellationToken cancellationToken = default)
    {
        EnsurePermission(AppPermissions.InstitutionsCreate);
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Nombre obligatorio.");
        var entity = new EducationalInstitution
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ShortName = request.ShortName?.Trim(),
            Rbd = request.Rbd?.Trim(),
            InstitutionType = request.InstitutionType,
            Address = request.Address,
            Commune = request.Commune,
            Region = request.Region,
            Country = request.Country,
            TimeZoneId = request.TimeZoneId
        };
        _db.EducationalInstitutions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("InstitutionCreated", true, _current.UserId, entity.Id, "EducationalInstitution", entity.Id.ToString(), cancellationToken: cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<InstitutionDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var q = _db.EducationalInstitutions.AsNoTracking().Where(i => !i.IsDeleted);
        if (!_current.IsInRole(nameof(ApplicationRole.SystemAdministrator)))
        {
            var allowed = _current.InstitutionIds.ToList();
            q = q.Where(i => allowed.Contains(i.Id));
        }
        var list = await q.OrderBy(i => i.Name).ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<InstitutionDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.EducationalInstitutions.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);
        if (entity is null) return null;
        EnsureInstitutionAccess(id);
        return Map(entity);
    }

    public async Task<InstitutionDto> UpdateAsync(Guid id, UpdateInstitutionRequest request, CancellationToken cancellationToken = default)
    {
        EnsurePermission(AppPermissions.InstitutionsUpdate);
        EnsureInstitutionAccess(id);
        var entity = await _db.EducationalInstitutions.FirstAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);
        if (request.Name is not null) entity.Name = request.Name.Trim();
        if (request.ShortName is not null) entity.ShortName = request.ShortName;
        if (request.Rbd is not null) entity.Rbd = request.Rbd;
        if (request.InstitutionType is not null) entity.InstitutionType = request.InstitutionType.Value;
        if (request.Address is not null) entity.Address = request.Address;
        if (request.Commune is not null) entity.Commune = request.Commune;
        if (request.Region is not null) entity.Region = request.Region;
        if (request.IsActive is not null) entity.IsActive = request.IsActive.Value;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        EnsurePermission(AppPermissions.InstitutionsUpdate);
        var entity = await _db.EducationalInstitutions.FirstAsync(i => i.Id == id, cancellationToken);
        entity.IsActive = active;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<InstitutionMembershipDto> AddMemberAsync(Guid institutionId, AddMembershipRequest request, CancellationToken cancellationToken = default)
    {
        EnsurePermission(AppPermissions.InstitutionsUpdate);
        EnsureInstitutionAccess(institutionId);
        var membership = new InstitutionMembership
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            UserId = request.UserId,
            Role = request.Role,
            Notes = request.Notes,
            CreatedByUserId = _current.UserId
        };
        _db.InstitutionMemberships.Add(membership);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("MembershipCreated", true, _current.UserId, institutionId, "InstitutionMembership", membership.Id.ToString(), cancellationToken: cancellationToken);
        var inst = await _db.EducationalInstitutions.AsNoTracking().FirstAsync(i => i.Id == institutionId, cancellationToken);
        return new InstitutionMembershipDto
        {
            Id = membership.Id,
            InstitutionId = institutionId,
            InstitutionName = inst.Name,
            UserId = request.UserId,
            Role = request.Role,
            IsActive = true
        };
    }

    public async Task<IReadOnlyList<InstitutionMembershipDto>> ListMembersAsync(Guid institutionId, CancellationToken cancellationToken = default)
    {
        EnsureInstitutionAccess(institutionId);
        return await _db.InstitutionMemberships.AsNoTracking()
            .Where(m => m.InstitutionId == institutionId && !m.IsDeleted)
            .Join(_db.Users, m => m.UserId, u => u.Id, (m, u) => new InstitutionMembershipDto
            {
                Id = m.Id,
                InstitutionId = m.InstitutionId,
                InstitutionName = "",
                UserId = m.UserId,
                UserDisplayName = u.DisplayName,
                Role = m.Role,
                IsActive = m.IsActive
            }).ToListAsync(cancellationToken);
    }

    public async Task RemoveMemberAsync(Guid institutionId, Guid membershipId, CancellationToken cancellationToken = default)
    {
        EnsurePermission(AppPermissions.InstitutionsUpdate);
        var m = await _db.InstitutionMemberships.FirstAsync(x => x.Id == membershipId && x.InstitutionId == institutionId, cancellationToken);
        m.IsDeleted = true;
        m.IsActive = false;
        m.EndedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AcademicPeriodDto> CreatePeriodAsync(Guid institutionId, CreateAcademicPeriodRequest request, CancellationToken cancellationToken = default)
    {
        EnsurePermission(AppPermissions.CoursesCreate);
        EnsureInstitutionAccess(institutionId);
        if (request.EndDate < request.StartDate) throw new ArgumentException("Fechas de período inválidas.");
        if (request.IsCurrent)
        {
            var currents = await _db.AcademicPeriods.Where(p => p.InstitutionId == institutionId && p.IsCurrent).ToListAsync(cancellationToken);
            foreach (var c in currents) c.IsCurrent = false;
        }

        var period = new AcademicPeriod
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            Name = request.Name.Trim(),
            Year = request.Year,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent,
            Status = request.IsCurrent ? AcademicPeriodStatus.Active : AcademicPeriodStatus.Draft
        };
        _db.AcademicPeriods.Add(period);
        await _db.SaveChangesAsync(cancellationToken);
        return MapPeriod(period);
    }

    public async Task<IReadOnlyList<AcademicPeriodDto>> ListPeriodsAsync(Guid institutionId, CancellationToken cancellationToken = default)
    {
        EnsureInstitutionAccess(institutionId);
        return await _db.AcademicPeriods.AsNoTracking()
            .Where(p => p.InstitutionId == institutionId)
            .OrderByDescending(p => p.Year)
            .Select(p => MapPeriod(p))
            .ToListAsync(cancellationToken);
    }

    public async Task ActivatePeriodAsync(Guid periodId, CancellationToken cancellationToken = default)
    {
        var period = await _db.AcademicPeriods.FirstAsync(p => p.Id == periodId, cancellationToken);
        EnsureInstitutionAccess(period.InstitutionId);
        var currents = await _db.AcademicPeriods.Where(p => p.InstitutionId == period.InstitutionId && p.IsCurrent).ToListAsync(cancellationToken);
        foreach (var c in currents) { c.IsCurrent = false; }
        period.IsCurrent = true;
        period.Status = AcademicPeriodStatus.Active;
        period.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ClosePeriodAsync(Guid periodId, CancellationToken cancellationToken = default)
    {
        var period = await _db.AcademicPeriods.FirstAsync(p => p.Id == periodId, cancellationToken);
        EnsureInstitutionAccess(period.InstitutionId);
        period.Status = AcademicPeriodStatus.Closed;
        period.IsCurrent = false;
        period.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SchoolCourseDto> CreateCourseAsync(Guid institutionId, CreateSchoolCourseRequest request, CancellationToken cancellationToken = default)
    {
        EnsurePermission(AppPermissions.CoursesCreate);
        EnsureInstitutionAccess(institutionId);
        var period = await _db.AcademicPeriods.FirstAsync(p => p.Id == request.AcademicPeriodId && p.InstitutionId == institutionId, cancellationToken);
        if (period.Status == AcademicPeriodStatus.Closed)
            throw new InvalidOperationException("No se pueden crear cursos en un período cerrado.");
        var level = await _db.Niveles.FirstAsync(n => n.Id == request.LevelId, cancellationToken);
        var display = string.IsNullOrWhiteSpace(request.Section) ? request.Name.Trim() : $"{request.Name.Trim()} {request.Section.Trim()}";
        var course = new SchoolCourse
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            AcademicPeriodId = request.AcademicPeriodId,
            LevelId = level.Id,
            Name = request.Name.Trim(),
            Section = request.Section?.Trim(),
            DisplayName = display,
            Capacity = request.Capacity,
            DefaultClassDurationMinutes = request.DefaultClassDurationMinutes
        };
        _db.SchoolCourses.Add(course);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("CourseCreated", true, _current.UserId, institutionId, "SchoolCourse", course.Id.ToString(), cancellationToken: cancellationToken);
        return MapCourse(course);
    }

    public async Task<IReadOnlyList<SchoolCourseDto>> ListCoursesAsync(Guid institutionId, CancellationToken cancellationToken = default)
    {
        EnsureInstitutionAccess(institutionId);
        var list = await _db.SchoolCourses.AsNoTracking()
            .Include(c => c.Level)
            .Include(c => c.Subjects).ThenInclude(s => s.Subject)
            .Where(c => c.InstitutionId == institutionId && !c.IsDeleted)
            .OrderBy(c => c.DisplayName)
            .ToListAsync(cancellationToken);
        return list.Select(MapCourse).ToList();
    }

    public async Task<SchoolCourseDto?> GetCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var course = await _db.SchoolCourses.AsNoTracking()
            .Include(c => c.Level)
            .Include(c => c.Subjects).ThenInclude(s => s.Subject)
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted, cancellationToken);
        if (course is null) return null;
        EnsureInstitutionAccess(course.InstitutionId);
        return MapCourse(course);
    }

    public async Task<CourseSubjectDto> AddSubjectAsync(Guid courseId, CreateCourseSubjectRequest request, CancellationToken cancellationToken = default)
    {
        var course = await _db.SchoolCourses.FirstAsync(c => c.Id == courseId && !c.IsDeleted, cancellationToken);
        EnsurePermission(AppPermissions.CoursesUpdate);
        EnsureInstitutionAccess(course.InstitutionId);
        var subject = await _db.Asignaturas.FirstAsync(a => a.Id == request.SubjectId, cancellationToken);
        var entity = new CourseSubject
        {
            Id = Guid.NewGuid(),
            SchoolCourseId = courseId,
            SubjectId = subject.Id,
            WeeklyHours = request.WeeklyHours,
            Notes = request.Notes
        };
        _db.CourseSubjects.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new CourseSubjectDto
        {
            Id = entity.Id,
            SchoolCourseId = courseId,
            SubjectId = subject.Id,
            SubjectName = subject.Nombre,
            WeeklyHours = entity.WeeklyHours,
            IsActive = true
        };
    }

    public async Task<IReadOnlyList<CourseSubjectDto>> ListSubjectsAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var course = await _db.SchoolCourses.AsNoTracking().FirstAsync(c => c.Id == courseId, cancellationToken);
        EnsureInstitutionAccess(course.InstitutionId);
        return await _db.CourseSubjects.AsNoTracking()
            .Where(s => s.SchoolCourseId == courseId)
            .Join(_db.Asignaturas, s => s.SubjectId, a => a.Id, (s, a) => new CourseSubjectDto
            {
                Id = s.Id,
                SchoolCourseId = s.SchoolCourseId,
                SubjectId = s.SubjectId,
                SubjectName = a.Nombre,
                WeeklyHours = s.WeeklyHours,
                IsActive = s.IsActive
            }).ToListAsync(cancellationToken);
    }

    public async Task<CourseTeacherAssignmentDto> AssignTeacherAsync(Guid courseSubjectId, AssignTeacherRequest request, CancellationToken cancellationToken = default)
    {
        EnsurePermission(AppPermissions.CoursesAssignTeachers);
        var cs = await _db.CourseSubjects.Include(s => s.SchoolCourse)
            .FirstAsync(s => s.Id == courseSubjectId, cancellationToken);
        EnsureInstitutionAccess(cs.SchoolCourse!.InstitutionId);

        if (request.IsPrimary || request.AssignmentType == TeacherAssignmentType.PrimaryTeacher)
        {
            var existingPrimary = await _db.CourseTeacherAssignments
                .AnyAsync(a => a.CourseSubjectId == courseSubjectId && a.IsPrimary && a.IsActive && !a.IsDeleted, cancellationToken);
            if (existingPrimary)
                throw new InvalidOperationException("Ya existe un profesor principal activo para esta asignatura.");
        }

        var assignment = new CourseTeacherAssignment
        {
            Id = Guid.NewGuid(),
            CourseSubjectId = courseSubjectId,
            UserId = request.UserId,
            AssignmentType = request.AssignmentType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsPrimary = request.IsPrimary || request.AssignmentType == TeacherAssignmentType.PrimaryTeacher
        };
        _db.CourseTeacherAssignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("TeacherAssigned", true, _current.UserId, cs.SchoolCourse.InstitutionId, "CourseTeacherAssignment", assignment.Id.ToString(), cancellationToken: cancellationToken);
        return new CourseTeacherAssignmentDto
        {
            Id = assignment.Id,
            CourseSubjectId = courseSubjectId,
            UserId = request.UserId,
            AssignmentType = assignment.AssignmentType,
            IsPrimary = assignment.IsPrimary,
            IsActive = true
        };
    }

    public async Task<IReadOnlyList<CourseTeacherAssignmentDto>> ListTeachersAsync(Guid courseSubjectId, CancellationToken cancellationToken = default)
    {
        var cs = await _db.CourseSubjects.Include(s => s.SchoolCourse).AsNoTracking()
            .FirstAsync(s => s.Id == courseSubjectId, cancellationToken);
        EnsureInstitutionAccess(cs.SchoolCourse!.InstitutionId);
        return await _db.CourseTeacherAssignments.AsNoTracking()
            .Where(a => a.CourseSubjectId == courseSubjectId && !a.IsDeleted)
            .Join(_db.Users, a => a.UserId, u => u.Id, (a, u) => new CourseTeacherAssignmentDto
            {
                Id = a.Id,
                CourseSubjectId = a.CourseSubjectId,
                UserId = a.UserId,
                UserDisplayName = u.DisplayName,
                AssignmentType = a.AssignmentType,
                IsPrimary = a.IsPrimary,
                IsActive = a.IsActive
            }).ToListAsync(cancellationToken);
    }

    public async Task RemoveTeacherAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        EnsurePermission(AppPermissions.CoursesAssignTeachers);
        var a = await _db.CourseTeacherAssignments.FirstAsync(x => x.Id == assignmentId, cancellationToken);
        a.IsDeleted = true;
        a.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private void EnsurePermission(string permission)
    {
        if (!_current.HasPermission(permission) && !_current.IsInRole(nameof(ApplicationRole.SystemAdministrator)))
            throw new UnauthorizedAccessException("Permiso denegado.");
    }

    private void EnsureInstitutionAccess(Guid institutionId)
    {
        if (_current.IsInRole(nameof(ApplicationRole.SystemAdministrator))) return;
        if (!_current.InstitutionIds.Contains(institutionId))
            throw new UnauthorizedAccessException("No tiene acceso a este establecimiento.");
    }

    private static InstitutionDto Map(EducationalInstitution i) => new()
    {
        Id = i.Id,
        Name = i.Name,
        ShortName = i.ShortName,
        Rbd = i.Rbd,
        InstitutionType = i.InstitutionType,
        Country = i.Country,
        TimeZoneId = i.TimeZoneId,
        IsActive = i.IsActive
    };

    private static AcademicPeriodDto MapPeriod(AcademicPeriod p) => new()
    {
        Id = p.Id,
        InstitutionId = p.InstitutionId,
        Name = p.Name,
        Year = p.Year,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        Status = p.Status,
        IsCurrent = p.IsCurrent
    };

    private static SchoolCourseDto MapCourse(SchoolCourse c) => new()
    {
        Id = c.Id,
        InstitutionId = c.InstitutionId,
        AcademicPeriodId = c.AcademicPeriodId,
        LevelId = c.LevelId,
        Name = c.Name,
        Section = c.Section,
        DisplayName = c.DisplayName,
        LevelName = c.Level?.Nombre,
        PrimarySubjectName = c.Subjects?
            .Where(s => s.IsActive)
            .Select(s => s.Subject?.Nombre)
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
        IsActive = c.IsActive
    };
}

public sealed class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRoleEntity> _roles;
    private readonly AppEducativaDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditService _audit;
    private readonly IAuthenticationService _auth;

    public UserAdminService(
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRoleEntity> roles,
        AppEducativaDbContext db,
        ICurrentUserService current,
        IAuditService audit,
        IAuthenticationService auth)
    {
        _users = users;
        _roles = roles;
        _db = db;
        _current = current;
        _audit = audit;
        _auth = auth;
    }

    public async Task<UserSummaryDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DisplayName = $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            MustChangePassword = request.MustChangePassword,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new AuthException(string.Join(" ", result.Errors.Select(e => e.Description)));

        foreach (var role in request.Roles)
        {
            if (!await _roles.RoleExistsAsync(role))
                await _roles.CreateAsync(new ApplicationRoleEntity(role));
            await _users.AddToRoleAsync(user, role);
        }

        _db.TeacherProfiles.Add(new TeacherProfile { Id = Guid.NewGuid(), UserId = user.Id });
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("UserCreated", true, _current.UserId, entityType: "ApplicationUser", entityId: user.Id.ToString(), cancellationToken: cancellationToken);
        return await MapAsync(user);
    }

    public async Task<IReadOnlyList<UserSummaryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var users = await _users.Users.AsNoTracking().Where(u => !u.IsDeleted).OrderBy(u => u.UserName).ToListAsync(cancellationToken);
        var list = new List<UserSummaryDto>();
        foreach (var u in users) list.Add(await MapAsync(u));
        return list;
    }

    public async Task<UserSummaryDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var user = await _users.FindByIdAsync(id.ToString());
        return user is null || user.IsDeleted ? null : await MapAsync(user);
    }

    public async Task<UserSummaryDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var user = await _users.FindByIdAsync(id.ToString()) ?? throw new KeyNotFoundException("Usuario no encontrado.");
        if (request.FirstName is not null) user.FirstName = request.FirstName.Trim();
        if (request.LastName is not null) user.LastName = request.LastName.Trim();
        user.DisplayName = $"{user.FirstName} {user.LastName}".Trim();
        if (request.Email is not null) user.Email = request.Email.Trim();
        if (request.PhoneNumber is not null) user.PhoneNumber = request.PhoneNumber;
        if (request.PreferredTimeZone is not null) user.PreferredTimeZone = request.PreferredTimeZone;
        if (request.PreferredLanguage is not null) user.PreferredLanguage = request.PreferredLanguage;
        if (request.IsActive is not null) user.IsActive = request.IsActive.Value;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        return await MapAsync(user);
    }

    public async Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var user = await _users.FindByIdAsync(id.ToString()) ?? throw new KeyNotFoundException("Usuario no encontrado.");
        user.IsActive = active;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        if (!active)
            await _auth.LogoutAllAsync(id, cancellationToken);
        await _audit.WriteAsync(active ? "UserActivated" : "UserDeactivated", true, _current.UserId, entityType: "ApplicationUser", entityId: id.ToString(), cancellationToken: cancellationToken);
    }

    public async Task AssignRolesAsync(Guid id, AssignRolesRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var user = await _users.FindByIdAsync(id.ToString()) ?? throw new KeyNotFoundException("Usuario no encontrado.");
        var current = await _users.GetRolesAsync(user);
        await _users.RemoveFromRolesAsync(user, current);
        foreach (var role in request.Roles)
        {
            if (!await _roles.RoleExistsAsync(role))
                await _roles.CreateAsync(new ApplicationRoleEntity(role));
            await _users.AddToRoleAsync(user, role);
        }
        await _audit.WriteAsync("RoleAssigned", true, _current.UserId, entityType: "ApplicationUser", entityId: id.ToString(), cancellationToken: cancellationToken);
    }

    public async Task ForcePasswordChangeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var user = await _users.FindByIdAsync(id.ToString()) ?? throw new KeyNotFoundException("Usuario no encontrado.");
        user.MustChangePassword = true;
        await _users.UpdateAsync(user);
        await _auth.LogoutAllAsync(id, cancellationToken);
    }

    public async Task<string> AdminResetPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var user = await _users.FindByIdAsync(id.ToString()) ?? throw new KeyNotFoundException("Usuario no encontrado.");
        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var result = await _users.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded) throw new AuthException(string.Join(" ", result.Errors.Select(e => e.Description)));
        user.MustChangePassword = true;
        await _users.UpdateAsync(user);
        await _auth.LogoutAllAsync(id, cancellationToken);
        return token;
    }

    private void EnsureAdmin()
    {
        if (!_current.HasPermission(AppPermissions.UsersView)
            && !_current.IsInRole(nameof(ApplicationRole.SystemAdministrator)))
            throw new UnauthorizedAccessException("Permiso denegado.");
    }

    private async Task<UserSummaryDto> MapAsync(ApplicationUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        return new UserSummaryDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            Roles = roles.ToList(),
            LastLoginAt = user.LastLoginAt
        };
    }
}
