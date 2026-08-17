using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.Classroom;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services.Classroom;

public interface ICourseRosterService
{
    Task<StudentDto> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentDto>> ListInstitutionStudentsAsync(Guid institutionId, CancellationToken cancellationToken = default);
    Task<CourseRosterDto> GetRosterAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<CourseRosterDto> GetRosterForClassAsync(Guid classId, CancellationToken cancellationToken = default);
    Task EnrollAsync(Guid courseId, EnrollStudentRequest request, CancellationToken cancellationToken = default);
}

public sealed class CourseRosterService : ICourseRosterService
{
    private readonly ProfeAsistenteDbContext _db;
    private readonly ClassroomAccess _access;

    public CourseRosterService(ProfeAsistenteDbContext db, ClassroomAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task<StudentDto> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomManageStudents);
        _access.EnsureInstitution(request.InstitutionId);
        var student = new Student
        {
            Id = Guid.NewGuid(),
            InstitutionId = request.InstitutionId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DisplayName = $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            BirthDate = request.BirthDate,
            Notes = request.Notes
        };
        _db.Students.Add(student);
        await _db.SaveChangesAsync(cancellationToken);
        return MapStudent(student, false);
    }

    public async Task<IReadOnlyList<StudentDto>> ListInstitutionStudentsAsync(Guid institutionId, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomView);
        _access.EnsureInstitution(institutionId);
        var list = await _db.Students.AsNoTracking()
            .Where(s => s.InstitutionId == institutionId && !s.IsDeleted)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);
        var support = await _db.StudentSupportPlans.AsNoTracking()
            .Where(p => p.InstitutionId == institutionId && p.IsActive)
            .Select(p => p.StudentId)
            .ToListAsync(cancellationToken);
        var set = support.ToHashSet();
        return list.Select(s => MapStudent(s, set.Contains(s.Id))).ToList();
    }

    public async Task<CourseRosterDto> GetRosterAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomView);
        var course = await _db.SchoolCourses.AsNoTracking().FirstAsync(c => c.Id == courseId, cancellationToken);
        _access.EnsureInstitution(course.InstitutionId);
        var rows = await _db.CourseEnrollments.AsNoTracking()
            .Where(e => e.SchoolCourseId == courseId && !e.IsDeleted)
            .Join(_db.Students, e => e.StudentId, s => s.Id, (e, s) => new { e, s })
            .ToListAsync(cancellationToken);
        var ids = rows.Select(r => r.s.Id).ToList();
        var support = await _db.StudentSupportPlans.AsNoTracking()
            .Where(p => p.IsActive && ids.Contains(p.StudentId))
            .Select(p => p.StudentId)
            .ToListAsync(cancellationToken);
        var set = support.ToHashSet();
        return new CourseRosterDto
        {
            CourseId = course.Id,
            CourseName = course.DisplayName,
            Students = rows.Select(r => new RosterStudentDto
            {
                StudentId = r.s.Id,
                DisplayName = r.s.DisplayName,
                Status = r.e.Status,
                HasActiveSupportPlan = set.Contains(r.s.Id)
            }).OrderBy(s => s.DisplayName).ToList()
        };
    }

    public async Task<CourseRosterDto> GetRosterForClassAsync(Guid classId, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomView);
        var planId = await _db.Clases.AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => c.PlanificacionId)
            .FirstAsync(cancellationToken);
        var courseId = await _db.Planificaciones.AsNoTracking()
            .Where(p => p.Id == planId)
            .Select(p => p.SchoolCourseId)
            .FirstAsync(cancellationToken);
        if (courseId is not Guid cid)
            return new CourseRosterDto { CourseId = Guid.Empty, CourseName = "Esta planificación no tiene curso asignado.", Students = [] };
        return await GetRosterAsync(cid, cancellationToken);
    }

    public async Task EnrollAsync(Guid courseId, EnrollStudentRequest request, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomManageStudents);
        var course = await _db.SchoolCourses.FirstAsync(c => c.Id == courseId, cancellationToken);
        _access.EnsureInstitution(course.InstitutionId);
        var exists = await _db.CourseEnrollments.AnyAsync(
            e => e.SchoolCourseId == courseId && e.StudentId == request.StudentId && !e.IsDeleted, cancellationToken);
        if (exists) return;
        _db.CourseEnrollments.Add(new CourseEnrollment
        {
            Id = Guid.NewGuid(),
            SchoolCourseId = courseId,
            StudentId = request.StudentId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static StudentDto MapStudent(Student s, bool support) => new()
    {
        Id = s.Id,
        InstitutionId = s.InstitutionId,
        FirstName = s.FirstName,
        LastName = s.LastName,
        DisplayName = s.DisplayName,
        IsActive = s.IsActive,
        HasActiveSupportPlan = support
    };
}
