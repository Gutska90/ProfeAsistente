using AppEducativa.Api.Data;
using AppEducativa.Api.Models.Classroom;
using AppEducativa.Api.Services.Authorization;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using AppEducativa.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api.Services.Classroom;

public interface IClassroomService
{
    Task<TeacherDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<StudentDto> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentDto>> ListInstitutionStudentsAsync(Guid institutionId, CancellationToken cancellationToken = default);
    Task<CourseRosterDto> GetRosterAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<CourseRosterDto> GetRosterForClassAsync(Guid classId, CancellationToken cancellationToken = default);
    Task EnrollAsync(Guid courseId, EnrollStudentRequest request, CancellationToken cancellationToken = default);
    Task<SupportPlanDto> AddSupportPlanAsync(Guid studentId, CreateSupportPlanRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportPlanDto>> ListSupportPlansAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<ClassDuaStrategyDto> AddDuaStrategyAsync(Guid classId, AddClassDuaStrategyRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassDuaStrategyDto>> ListDuaAsync(Guid classId, CancellationToken cancellationToken = default);
    Task SaveAttendanceAsync(Guid classId, SaveAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(Guid classId, CancellationToken cancellationToken = default);
    Task<LearningAssessmentDto> CreateAssessmentAsync(CreateLearningAssessmentRequest request, CancellationToken cancellationToken = default);
    Task SaveScoresAsync(Guid assessmentId, IReadOnlyList<SaveAssessmentScoreRequest> scores, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssessmentScoreDto>> GetScoresAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LearningAssessmentDto>> ListAssessmentsAsync(Guid? courseId, Guid? classId = null, CancellationToken cancellationToken = default);
}

public sealed class ClassroomService : IClassroomService
{
    private readonly AppEducativaDbContext _db;
    private readonly ICurrentUserService _current;

    public ClassroomService(AppEducativaDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<TeacherDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomView, AppPermissions.PlanningViewOwn);
        var userId = _current.UserId;
        var inst = _current.ActiveInstitutionId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var plans = _db.Planificaciones.AsNoTracking().Where(p => !p.IsDeleted);
        if (userId is Guid uid && !_current.IsInRole(nameof(ApplicationRole.SystemAdministrator)))
            plans = plans.Where(p => p.OwnerUserId == uid || (inst != null && p.InstitutionId == inst));

        var planList = await plans.ToListAsync(cancellationToken);
        var planIds = planList.Select(p => p.Id).ToList();
        var classes = await _db.Clases.AsNoTracking()
            .Where(c => planIds.Contains(c.PlanificacionId))
            .ToListAsync(cancellationToken);

        var upcoming = classes
            .Where(c => c.Fecha >= today && c.Estado == EstadoClase.Planificada)
            .OrderBy(c => c.Fecha)
            .Take(5)
            .ToList();

        var oaIds = upcoming.Select(c => c.ObjetivoAprendizajeId).Distinct().ToList();
        var oaCodes = await _db.ObjetivosAprendizaje.AsNoTracking()
            .Where(o => oaIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Codigo, cancellationToken);

        var alerts = await _db.PlanningAlerts.AsNoTracking()
            .CountAsync(a => planIds.Contains(a.PlanningId) && !a.IsResolved, cancellationToken);

        var supportCount = 0;
        if (inst is Guid iid)
        {
            supportCount = await _db.StudentSupportPlans.AsNoTracking()
                .CountAsync(s => s.InstitutionId == iid && s.IsActive, cancellationToken);
        }

        var reminders = new List<string>();
        if (classes.Any(c => c.Estado == EstadoClase.Planificada && c.Fecha < today))
            reminders.Add("Hay clases planificadas con fecha vencida. Márquelas como realizadas o reprogramelas.");
        if (alerts > 0)
            reminders.Add("Revise alertas de cobertura curricular (OA o indicadores sin evidencia).");
        if (supportCount > 0)
            reminders.Add("Hay planes PIE/DUA activos: aplique estrategias de diversificación en la clase de hoy.");
        reminders.Add("Este registro es de apoyo docente local. No reemplaza SIGE ni el libro de clases oficial.");

        var instName = inst is Guid iidName
            ? await _db.EducationalInstitutions.AsNoTracking()
                .Where(i => i.Id == iidName)
                .Select(i => i.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return new TeacherDashboardDto
        {
            TeacherName = _current.UserName ?? "Docente",
            InstitutionName = instName,
            ActivePlannings = planList.Count,
            UpcomingClasses = upcoming.Count,
            PendingClasses = classes.Count(c => c.Estado == EstadoClase.Planificada),
            OpenCoverageAlerts = alerts,
            StudentsWithSupportPlans = supportCount,
            NextClasses = upcoming.Select(c => new UpcomingClassDto
            {
                ClassId = c.Id,
                PlanningId = c.PlanificacionId,
                PlanningName = planList.First(p => p.Id == c.PlanificacionId).Nombre,
                Date = c.Fecha,
                ObjectiveCode = oaCodes.GetValueOrDefault(c.ObjetivoAprendizajeId, ""),
                Estado = c.Estado.ToString()
            }).ToList(),
            Reminders = reminders
        };
    }

    public async Task<StudentDto> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomManageStudents);
        EnsureInstitution(request.InstitutionId);
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
        Ensure(AppPermissions.ClassroomView);
        EnsureInstitution(institutionId);
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
        Ensure(AppPermissions.ClassroomView);
        var course = await _db.SchoolCourses.AsNoTracking().FirstAsync(c => c.Id == courseId, cancellationToken);
        EnsureInstitution(course.InstitutionId);
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
        Ensure(AppPermissions.ClassroomView);
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
        Ensure(AppPermissions.ClassroomManageStudents);
        var course = await _db.SchoolCourses.FirstAsync(c => c.Id == courseId, cancellationToken);
        EnsureInstitution(course.InstitutionId);
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

    public async Task<SupportPlanDto> AddSupportPlanAsync(Guid studentId, CreateSupportPlanRequest request, CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomSupportPlans);
        var student = await _db.Students.FirstAsync(s => s.Id == studentId && !s.IsDeleted, cancellationToken);
        EnsureInstitution(student.InstitutionId);
        var plan = new StudentSupportPlan
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            InstitutionId = student.InstitutionId,
            PlanType = request.PlanType,
            NeedType = request.NeedType,
            Title = request.Title.Trim(),
            Strategies = request.Strategies.Trim(),
            AccessAdjustments = request.AccessAdjustments,
            ObjectiveAdjustments = request.ObjectiveAdjustments,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedByUserId = _current.UserId
        };
        _db.StudentSupportPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);
        return MapPlan(plan);
    }

    public async Task<IReadOnlyList<SupportPlanDto>> ListSupportPlansAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomView);
        var list = await _db.StudentSupportPlans.AsNoTracking()
            .Where(p => p.StudentId == studentId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        return list.Select(MapPlan).ToList();
    }

    public async Task<ClassDuaStrategyDto> AddDuaStrategyAsync(Guid classId, AddClassDuaStrategyRequest request, CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomSupportPlans, AppPermissions.PlanningUpdateOwn);
        var entity = new ClassDuaStrategy
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            Principle = request.Principle,
            Strategy = request.Strategy.Trim(),
            Notes = request.Notes
        };
        _db.ClassDuaStrategies.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new ClassDuaStrategyDto { Id = entity.Id, Principle = entity.Principle, Strategy = entity.Strategy };
    }

    public async Task<IReadOnlyList<ClassDuaStrategyDto>> ListDuaAsync(Guid classId, CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomView);
        return await _db.ClassDuaStrategies.AsNoTracking()
            .Where(d => d.ClassId == classId)
            .Select(d => new ClassDuaStrategyDto { Id = d.Id, Principle = d.Principle, Strategy = d.Strategy })
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAttendanceAsync(Guid classId, SaveAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomAttendance);
        var existing = await _db.AttendanceRecords.Where(a => a.ClassId == classId).ToListAsync(cancellationToken);
        _db.AttendanceRecords.RemoveRange(existing);
        foreach (var e in request.Entries)
        {
            _db.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                StudentId = e.StudentId,
                Status = e.Status,
                Justification = e.Justification,
                RecordedByUserId = _current.UserId
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(Guid classId, CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomView);
        return await _db.AttendanceRecords.AsNoTracking()
            .Where(a => a.ClassId == classId)
            .Join(_db.Students, a => a.StudentId, s => s.Id, (a, s) => new AttendanceRecordDto
            {
                StudentId = a.StudentId,
                StudentName = s.DisplayName,
                Status = a.Status,
                Justification = a.Justification
            }).ToListAsync(cancellationToken);
    }

    public async Task<LearningAssessmentDto> CreateAssessmentAsync(CreateLearningAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomEvaluate);
        Guid institutionId = request.InstitutionId;
        Guid? courseId = request.SchoolCourseId;
        Guid? planningId = request.PlanningId;
        Guid? classId = request.ClassId;
        Guid? oaId = request.ObjectiveLearningId;

        if (classId is Guid cid)
        {
            var meta = await _db.Clases.AsNoTracking()
                .Where(c => c.Id == cid)
                .Join(_db.Planificaciones, c => c.PlanificacionId, p => p.Id, (c, p) => new { c, p })
                .FirstAsync(cancellationToken);
            planningId ??= meta.p.Id;
            courseId ??= meta.p.SchoolCourseId;
            oaId ??= meta.c.ObjetivoAprendizajeId;
            if (institutionId == Guid.Empty)
                institutionId = meta.p.InstitutionId ?? _current.ActiveInstitutionId ?? Guid.Empty;
        }

        if (institutionId == Guid.Empty)
            throw new InvalidOperationException("Indique el establecimiento o cree la evaluación desde una clase.");
        EnsureInstitution(institutionId);

        var entity = new LearningAssessment
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            SchoolCourseId = courseId,
            ClassId = classId,
            PlanningId = planningId,
            ObjectiveLearningId = oaId,
            EducationalDocumentId = request.EducationalDocumentId,
            Purpose = request.Purpose,
            Name = request.Name.Trim(),
            Date = request.Date,
            Criteria = request.Criteria,
            CreatedByUserId = _current.UserId ?? Guid.Empty
        };
        _db.LearningAssessments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return MapAssessment(entity);
    }

    public async Task SaveScoresAsync(Guid assessmentId, IReadOnlyList<SaveAssessmentScoreRequest> scores, CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomEvaluate);
        var existing = await _db.AssessmentScores.Where(s => s.LearningAssessmentId == assessmentId).ToListAsync(cancellationToken);
        _db.AssessmentScores.RemoveRange(existing);
        foreach (var s in scores)
        {
            _db.AssessmentScores.Add(new AssessmentScore
            {
                Id = Guid.NewGuid(),
                LearningAssessmentId = assessmentId,
                StudentId = s.StudentId,
                Score = s.Score,
                AchievementLevel = s.AchievementLevel,
                Feedback = s.Feedback
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AssessmentScoreDto>> GetScoresAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomView, AppPermissions.ClassroomEvaluate);
        var assessment = await _db.LearningAssessments.AsNoTracking()
            .FirstAsync(a => a.Id == assessmentId, cancellationToken);
        EnsureInstitution(assessment.InstitutionId);

        var saved = await _db.AssessmentScores.AsNoTracking()
            .Where(s => s.LearningAssessmentId == assessmentId)
            .ToListAsync(cancellationToken);
        var byStudent = saved.ToDictionary(s => s.StudentId);

        IReadOnlyList<RosterStudentDto> students = [];
        if (assessment.ClassId is Guid classId)
            students = (await GetRosterForClassAsync(classId, cancellationToken)).Students;
        else if (assessment.SchoolCourseId is Guid courseId)
            students = (await GetRosterAsync(courseId, cancellationToken)).Students;

        if (students.Count > 0)
        {
            return students.Select(s =>
            {
                byStudent.TryGetValue(s.StudentId, out var row);
                return new AssessmentScoreDto
                {
                    StudentId = s.StudentId,
                    StudentName = s.DisplayName,
                    Score = row?.Score,
                    AchievementLevel = row?.AchievementLevel,
                    Feedback = row?.Feedback
                };
            }).ToList();
        }

        var names = await _db.Students.AsNoTracking()
            .Where(s => saved.Select(x => x.StudentId).Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.DisplayName, cancellationToken);
        return saved.Select(s => new AssessmentScoreDto
        {
            StudentId = s.StudentId,
            StudentName = names.GetValueOrDefault(s.StudentId, "Estudiante"),
            Score = s.Score,
            AchievementLevel = s.AchievementLevel,
            Feedback = s.Feedback
        }).ToList();
    }

    public async Task<IReadOnlyList<LearningAssessmentDto>> ListAssessmentsAsync(Guid? courseId, Guid? classId = null, CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomView);
        var q = _db.LearningAssessments.AsNoTracking().AsQueryable();
        if (classId is Guid clid)
            q = q.Where(a => a.ClassId == clid);
        else if (courseId is Guid cid)
            q = q.Where(a => a.SchoolCourseId == cid);
        else if (_current.ActiveInstitutionId is Guid iid)
            q = q.Where(a => a.InstitutionId == iid);
        var list = await q.OrderByDescending(a => a.Date).Take(100).ToListAsync(cancellationToken);
        return list.Select(MapAssessment).ToList();
    }

    private void Ensure(params string[] permissions)
    {
        if (_current.IsInRole(nameof(ApplicationRole.SystemAdministrator))) return;
        if (permissions.Any(_current.HasPermission)) return;
        throw new UnauthorizedAccessException("No tiene permiso para esta acción de aula.");
    }

    private void EnsureInstitution(Guid institutionId)
    {
        if (_current.IsInRole(nameof(ApplicationRole.SystemAdministrator))) return;
        if (!_current.InstitutionIds.Contains(institutionId) && _current.ActiveInstitutionId != institutionId)
            throw new UnauthorizedAccessException("No tiene acceso a este establecimiento.");
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

    private static SupportPlanDto MapPlan(StudentSupportPlan p) => new()
    {
        Id = p.Id,
        StudentId = p.StudentId,
        PlanType = p.PlanType,
        NeedType = p.NeedType,
        Title = p.Title,
        Strategies = p.Strategies,
        AccessAdjustments = p.AccessAdjustments,
        ObjectiveAdjustments = p.ObjectiveAdjustments,
        IsActive = p.IsActive
    };

    private static LearningAssessmentDto MapAssessment(LearningAssessment a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Purpose = a.Purpose,
        Date = a.Date,
        ClassId = a.ClassId,
        SchoolCourseId = a.SchoolCourseId,
        ObjectiveLearningId = a.ObjectiveLearningId,
        Criteria = a.Criteria
    };
}
