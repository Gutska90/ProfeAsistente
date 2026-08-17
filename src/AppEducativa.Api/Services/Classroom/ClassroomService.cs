using AppEducativa.Api.Data;
using AppEducativa.Api.Models;
using AppEducativa.Api.Models.Classroom;
using AppEducativa.Api.Models.Planning;
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
    Task<AssessmentEvidenceSummaryDto> GetAssessmentEvidenceAsync(Guid assessmentId, CancellationToken cancellationToken = default);
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
        var today = LocalSchoolDate();

        var plans = _db.Planificaciones.AsNoTracking()
            .Include(p => p.NivelAsignatura)!.ThenInclude(n => n!.Asignatura)
            .Include(p => p.Unidad)
            .Where(p => !p.IsDeleted);
        if (userId is Guid uid && !_current.IsInRole(nameof(ApplicationRole.SystemAdministrator)))
            plans = plans.Where(p => p.OwnerUserId == uid || (inst != null && p.InstitutionId == inst));

        var planList = await plans.ToListAsync(cancellationToken);
        var planIds = planList.Select(p => p.Id).ToList();
        var planById = planList.ToDictionary(p => p.Id);

        var courseIds = planList.Where(p => p.SchoolCourseId is not null).Select(p => p.SchoolCourseId!.Value).Distinct().ToList();
        var courses = await _db.SchoolCourses.AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var classes = await _db.Clases.AsNoTracking()
            .Where(c => planIds.Contains(c.PlanificacionId))
            .ToListAsync(cancellationToken);

        var upcoming = classes
            .Where(c => c.Fecha >= today && c.Estado == EstadoClase.Planificada)
            .OrderBy(c => c.Fecha)
            .ThenBy(c => c.Numero)
            .Take(8)
            .ToList();

        var todayClasses = classes
            .Where(c => c.Fecha == today && c.Estado == EstadoClase.Planificada)
            .OrderBy(c => c.Numero)
            .ToList();

        var oaIds = upcoming.Concat(todayClasses).Select(c => c.ObjetivoAprendizajeId).Distinct().ToList();
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

        var overdue = classes.Count(c => c.Estado == EstadoClase.Planificada && c.Fecha < today);
        var pendingItems = new List<TeacherPendingItemDto>();
        if (overdue > 0)
            pendingItems.Add(new TeacherPendingItemDto
            {
                Kind = "overdue_class",
                Text = overdue == 1
                    ? "1 clase planificada con fecha vencida"
                    : $"{overdue} clases planificadas con fecha vencida"
            });
        if (alerts > 0)
            pendingItems.Add(new TeacherPendingItemDto
            {
                Kind = "coverage",
                Text = alerts == 1 ? "1 alerta de cobertura OA" : $"{alerts} alertas de cobertura OA"
            });
        if (supportCount > 0)
            pendingItems.Add(new TeacherPendingItemDto
            {
                Kind = "support",
                Text = "Hay planes PIE/DUA activos: revise diversificación en la clase"
            });
        if (planList.Count == 0)
            pendingItems.Add(new TeacherPendingItemDto
            {
                Kind = "planning",
                Text = "Aún no hay planificación. Cree una desde Mis cursos o Planificaciones"
            });

        var reminders = pendingItems.Select(p => p.Text).ToList();
        reminders.Add("Registro de apoyo docente. No reemplaza SIGE ni el libro de clases oficial.");

        var instName = inst is Guid iidName
            ? await _db.EducationalInstitutions.AsNoTracking()
                .Where(i => i.Id == iidName)
                .Select(i => i.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var teacherName = _current.UserName ?? "Docente";
        UpcomingClassDto MapUpcoming(Clase c)
        {
            var plan = planById[c.PlanificacionId];
            var courseName = plan.SchoolCourseId is Guid cid && courses.TryGetValue(cid, out var course)
                ? course.DisplayName
                : string.Empty;
            var subject = plan.NivelAsignatura?.NombreEnNivel
                          ?? plan.NivelAsignatura?.Asignatura?.Nombre
                          ?? string.Empty;
            var unit = plan.Unidad is null ? string.Empty : $"{plan.Unidad.Numero}. {plan.Unidad.Nombre}";
            return new UpcomingClassDto
            {
                ClassId = c.Id,
                PlanningId = c.PlanificacionId,
                SchoolCourseId = plan.SchoolCourseId,
                PlanningName = plan.Nombre,
                CourseDisplayName = courseName,
                SubjectName = subject,
                UnitName = unit,
                Date = c.Fecha,
                ObjectiveCode = oaCodes.GetValueOrDefault(c.ObjetivoAprendizajeId, ""),
                Estado = c.Estado.ToString()
            };
        }

        return new TeacherDashboardDto
        {
            TeacherName = teacherName,
            Greeting = BuildGreeting(teacherName),
            InstitutionName = instName,
            Today = today,
            ActivePlannings = planList.Count,
            UpcomingClasses = upcoming.Count,
            PendingClasses = classes.Count(c => c.Estado == EstadoClase.Planificada),
            OpenCoverageAlerts = alerts,
            StudentsWithSupportPlans = supportCount,
            TodayClasses = todayClasses.Select(MapUpcoming).ToList(),
            NextClasses = upcoming.Select(MapUpcoming).ToList(),
            PendingItems = pendingItems,
            Reminders = reminders
        };
    }

    private static DateOnly LocalSchoolDate()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
        }
        catch (TimeZoneNotFoundException)
        {
            return DateOnly.FromDateTime(DateTime.Now);
        }
        catch (InvalidTimeZoneException)
        {
            return DateOnly.FromDateTime(DateTime.Now);
        }
    }

    private static string BuildGreeting(string teacherName)
    {
        var hour = DateTime.Now.Hour;
        var saludo = hour < 12 ? "Buenos días" : hour < 19 ? "Buenas tardes" : "Buenas noches";
        var shortName = teacherName.Contains('@') ? teacherName.Split('@')[0] : teacherName;
        if (string.Equals(shortName, "admin", StringComparison.OrdinalIgnoreCase))
            shortName = "docente";
        return $"{saludo}, {shortName}";
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

        var documentId = request.EducationalDocumentId;
        if (documentId is null && classId is Guid classForDoc)
        {
            documentId = await _db.EducationalDocuments.AsNoTracking()
                .Where(d => d.ClassId == classForDoc && !d.IsDeleted
                            && d.DocumentType == EducationalDocumentType.Assessment)
                .OrderByDescending(d => d.UpdatedAt)
                .Select(d => (Guid?)d.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var entity = new LearningAssessment
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            SchoolCourseId = courseId,
            ClassId = classId,
            PlanningId = planningId,
            ObjectiveLearningId = oaId,
            EducationalDocumentId = documentId,
            Purpose = request.Purpose,
            Name = request.Name.Trim(),
            Date = request.Date,
            Criteria = request.Criteria,
            CreatedByUserId = _current.UserId ?? Guid.Empty
        };
        _db.LearningAssessments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAssessmentAsync(entity, cancellationToken);
    }

    public async Task SaveScoresAsync(Guid assessmentId, IReadOnlyList<SaveAssessmentScoreRequest> scores, CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomEvaluate);
        var assessment = await _db.LearningAssessments
            .FirstAsync(a => a.Id == assessmentId, cancellationToken);
        EnsureInstitution(assessment.InstitutionId);

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

        await RecordAssessmentEvidenceAsync(assessment, scores, cancellationToken);
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
        var result = new List<LearningAssessmentDto>();
        foreach (var a in list)
            result.Add(await MapAssessmentAsync(a, cancellationToken));
        return result;
    }

    public async Task<AssessmentEvidenceSummaryDto> GetAssessmentEvidenceAsync(
        Guid assessmentId, CancellationToken cancellationToken = default)
    {
        Ensure(AppPermissions.ClassroomView, AppPermissions.ClassroomEvaluate);
        var assessment = await _db.LearningAssessments.AsNoTracking()
            .FirstAsync(a => a.Id == assessmentId, cancellationToken);
        EnsureInstitution(assessment.InstitutionId);
        var scores = await GetScoresAsync(assessmentId, cancellationToken);
        return await BuildEvidenceSummaryAsync(assessment, scores, cancellationToken);
    }

    private async Task RecordAssessmentEvidenceAsync(
        LearningAssessment assessment,
        IReadOnlyList<SaveAssessmentScoreRequest> scores,
        CancellationToken cancellationToken)
    {
        if (assessment.ClassId is not Guid classId) return;

        var tag = $"assessment:{assessment.Id}";
        var previous = await _db.ClassLearningEvidences
            .Where(e => e.ClassId == classId && e.Notes == tag)
            .ToListAsync(cancellationToken);
        _db.ClassLearningEvidences.RemoveRange(previous);

        var scoreDtos = scores.Select(s => new AssessmentScoreDto
        {
            StudentId = s.StudentId,
            StudentName = string.Empty,
            Score = s.Score,
            AchievementLevel = s.AchievementLevel,
            Feedback = s.Feedback
        }).ToList();
        var summary = await BuildEvidenceSummaryAsync(assessment, scoreDtos, cancellationToken);

        var evidenceType = assessment.Purpose switch
        {
            EvaluationPurpose.Summative => LearningEvidenceType.SummativeAssessment,
            EvaluationPurpose.Diagnostic => LearningEvidenceType.FormativeAssessment,
            _ => LearningEvidenceType.FormativeAssessment
        };

        Guid? indicatorId = null;
        if (assessment.ClassId is Guid cid)
        {
            indicatorId = await _db.ClaseIndicadores.AsNoTracking()
                .Where(i => i.ClaseId == cid)
                .Select(i => (Guid?)i.IndicadorEvaluacionId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        _db.ClassLearningEvidences.Add(new ClassLearningEvidence
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            EvaluationIndicatorId = indicatorId,
            EvidenceType = evidenceType,
            Description = summary.ReadingSummary,
            Source = "Assessment",
            Notes = tag,
            RecordedAt = DateTime.UtcNow
        });
    }

    private async Task<AssessmentEvidenceSummaryDto> BuildEvidenceSummaryAsync(
        LearningAssessment assessment,
        IReadOnlyList<AssessmentScoreDto> scores,
        CancellationToken cancellationToken)
    {
        var withLevel = scores.Where(s => !string.IsNullOrWhiteSpace(s.AchievementLevel)).ToList();
        static bool IsPorLograr(string? l) =>
            (l ?? "").Contains("por lograr", StringComparison.OrdinalIgnoreCase)
            || (l ?? "").Equals("PL", StringComparison.OrdinalIgnoreCase);
        static bool IsLogrado(string? l) =>
            (l ?? "").Equals("logrado", StringComparison.OrdinalIgnoreCase)
            || ((l ?? "").Contains("logrado", StringComparison.OrdinalIgnoreCase)
                && !(l ?? "").Contains("medianamente", StringComparison.OrdinalIgnoreCase)
                && !(l ?? "").Contains("por lograr", StringComparison.OrdinalIgnoreCase));
        static bool IsMedianamente(string? l) =>
            (l ?? "").Contains("medianamente", StringComparison.OrdinalIgnoreCase);

        var porLograr = withLevel.Count(s => IsPorLograr(s.AchievementLevel));
        var logrado = withLevel.Count(s => IsLogrado(s.AchievementLevel));
        var medianamente = withLevel.Count(s => IsMedianamente(s.AchievementLevel));
        // If labels don't match buckets, count remainder as medianamente for display stability.
        var classified = porLograr + logrado + medianamente;
        if (classified < withLevel.Count)
            medianamente += withLevel.Count - classified;

        var numeric = scores.Where(s => s.Score is not null).Select(s => s.Score!.Value).ToList();
        var avg = numeric.Count == 0 ? (decimal?)null : Math.Round(numeric.Average(), 1);

        string oaCode = string.Empty;
        string oaDesc = string.Empty;
        Guid? oaId = assessment.ObjectiveLearningId;
        if (oaId is Guid oid)
        {
            var oa = await _db.ObjetivosAprendizaje.AsNoTracking().FirstOrDefaultAsync(o => o.Id == oid, cancellationToken);
            if (oa is not null)
            {
                oaCode = oa.Codigo;
                oaDesc = oa.Descripcion;
            }
        }

        var indicators = new List<string>();
        if (assessment.ClassId is Guid classId)
        {
            var indIds = await _db.ClaseIndicadores.AsNoTracking()
                .Where(i => i.ClaseId == classId)
                .Select(i => i.IndicadorEvaluacionId)
                .ToListAsync(cancellationToken);
            indicators = await _db.IndicadoresEvaluacion.AsNoTracking()
                .Where(i => indIds.Contains(i.Id))
                .Select(i => i.Descripcion)
                .ToListAsync(cancellationToken);
        }

        IReadOnlyList<AssessmentSpecificationRowDto> specs = [];
        if (assessment.EducationalDocumentId is Guid docId)
        {
            var rawSpecs = await _db.AssessmentSpecifications.AsNoTracking()
                .Where(s => s.EducationalDocumentId == docId)
                .ToListAsync(cancellationToken);
            var indIds = rawSpecs.Select(s => s.EvaluationIndicatorId).Distinct().ToList();
            var indNames = await _db.IndicadoresEvaluacion.AsNoTracking()
                .Where(i => indIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Descripcion, cancellationToken);
            specs = rawSpecs.Select(s => new AssessmentSpecificationRowDto
            {
                Id = s.Id,
                EvaluationIndicatorId = s.EvaluationIndicatorId,
                IndicatorDescription = indNames.GetValueOrDefault(s.EvaluationIndicatorId, "Indicador"),
                BloomLevel = s.BloomLevel,
                ItemCount = s.ItemCount,
                TotalPoints = s.TotalPoints,
                WeightPercentage = s.WeightPercentage
            }).ToList();
        }

        var weakIds = scores.Where(s => IsPorLograr(s.AchievementLevel)).Select(s => s.StudentId).ToHashSet();
        var needsSupport = scores
            .Where(s => weakIds.Contains(s.StudentId) && !string.IsNullOrWhiteSpace(s.StudentName))
            .Select(s => s.StudentName)
            .Distinct()
            .Take(12)
            .ToList();
        if (needsSupport.Count == 0 && weakIds.Count > 0 && assessment.ClassId is Guid cid2)
        {
            var roster = await GetRosterForClassAsync(cid2, cancellationToken);
            needsSupport = roster.Students
                .Where(s => weakIds.Contains(s.StudentId))
                .Select(s => s.DisplayName)
                .Take(12)
                .ToList();
        }

        var needsReinforcement = withLevel.Count > 0
            && (porLograr * 2 >= withLevel.Count || (porLograr + medianamente) * 3 >= withLevel.Count * 2);

        var purposeLabel = assessment.Purpose switch
        {
            EvaluationPurpose.Diagnostic => "Diagnóstica",
            EvaluationPurpose.Summative => "Sumativa",
            _ => "Formativa"
        };

        var reading = withLevel.Count == 0
            ? $"OA {oaCode}: aún no hay niveles de logro registrados."
            : $"OA {oaCode}: {logrado} logrado(s), {medianamente} medianamente logrado(s), {porLograr} por lograr"
              + (avg is not null ? $". Promedio {avg}." : ".")
              + (needsReinforcement
                  ? " Se recomienda crear un refuerzo alineado a este OA."
                  : " El grupo avanza; mantenga seguimiento formativo.");

        return new AssessmentEvidenceSummaryDto
        {
            AssessmentId = assessment.Id,
            AssessmentName = assessment.Name,
            ClassId = assessment.ClassId,
            ObjectiveId = oaId,
            ObjectiveCode = oaCode,
            ObjectiveDescription = oaDesc,
            PurposeLabel = purposeLabel,
            StudentsTotal = scores.Count,
            StudentsWithLevel = withLevel.Count,
            CountPorLograr = porLograr,
            CountMedianamente = medianamente,
            CountLogrado = logrado,
            AverageScore = avg,
            NeedsReinforcement = needsReinforcement,
            ReadingSummary = reading,
            Indicators = indicators,
            SpecificationTable = specs,
            EducationalDocumentId = assessment.EducationalDocumentId,
            StudentsNeedingSupport = needsSupport
        };
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

    private async Task<LearningAssessmentDto> MapAssessmentAsync(LearningAssessment a, CancellationToken ct)
    {
        string? code = null;
        string? desc = null;
        if (a.ObjectiveLearningId is Guid oid)
        {
            var oa = await _db.ObjetivosAprendizaje.AsNoTracking().FirstOrDefaultAsync(o => o.Id == oid, ct);
            code = oa?.Codigo;
            desc = oa?.Descripcion;
        }

        return new LearningAssessmentDto
        {
            Id = a.Id,
            Name = a.Name,
            Purpose = a.Purpose,
            Date = a.Date,
            ClassId = a.ClassId,
            SchoolCourseId = a.SchoolCourseId,
            ObjectiveLearningId = a.ObjectiveLearningId,
            EducationalDocumentId = a.EducationalDocumentId,
            ObjectiveCode = code,
            ObjectiveDescription = desc,
            Criteria = a.Criteria
        };
    }
}
