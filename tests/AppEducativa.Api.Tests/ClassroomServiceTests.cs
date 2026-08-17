using AppEducativa.Api.Data;
using AppEducativa.Api.Models;
using AppEducativa.Api.Models.Institutions;
using AppEducativa.Api.Services.Classroom;
using AppEducativa.Api.Tests.TestDoubles;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using AppEducativa.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api.Tests;

public class ClassroomServiceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"appedu-aula-{Guid.NewGuid():N}.db");
    private readonly AppEducativaDbContext _db;
    private readonly ClassroomService _svc;
    private readonly Guid _inst = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();

    public ClassroomServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppEducativaDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _db = new AppEducativaDbContext(options);
        _db.Database.Migrate();
        _db.EducationalInstitutions.Add(new Api.Models.Institutions.EducationalInstitution
        {
            Id = _inst,
            Name = "Escuela Test",
            PeiVision = "Formación integral",
            PeiSeals = "Inclusión"
        });
        _db.SaveChanges();

        var current = new FakeCurrentUserService
        {
            IsAuthenticated = true,
            UserId = _user,
            UserName = "prof",
            Roles = ["Teacher"],
            Permissions =
            [
                AppPermissions.ClassroomView, AppPermissions.ClassroomManageStudents,
                AppPermissions.ClassroomAttendance, AppPermissions.ClassroomEvaluate,
                AppPermissions.ClassroomSupportPlans, AppPermissions.PlanningViewOwn
            ],
            InstitutionIds = [_inst],
            ActiveInstitutionId = _inst
        };
        _svc = new ClassroomService(_db, current);
    }

    [Fact]
    public async Task CrearEstudiante_YPlanPie()
    {
        var student = await _svc.CreateStudentAsync(new CreateStudentRequest
        {
            InstitutionId = _inst,
            FirstName = "Ana",
            LastName = "Soto"
        });
        Assert.Equal("Ana Soto", student.DisplayName);

        var plan = await _svc.AddSupportPlanAsync(student.Id, new CreateSupportPlanRequest
        {
            PlanType = SupportPlanType.Pie,
            NeedType = SpecialEducationalNeedType.Transitory,
            Title = "PIE lenguaje",
            Strategies = "Apoyo en aula común, Decreto 83 acceso.",
            StartDate = new DateOnly(2026, 3, 1)
        });
        Assert.True(plan.IsActive);

        var dash = await _svc.GetDashboardAsync();
        Assert.Equal("Escuela Test", dash.InstitutionName);
        Assert.Equal(1, dash.StudentsWithSupportPlans);
        Assert.False(string.IsNullOrWhiteSpace(dash.Greeting));
        Assert.Contains(dash.Reminders, r => r.Contains("SIGE", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(dash.PendingItems, p => p.Kind == "support");
    }

    [Fact]
    public async Task Dashboard_HoyIncluyeClasesDelDia()
    {
        DemoCurriculumSeed.Seed(_db);
        await _db.SaveChangesAsync();
        var today = DateOnly.FromDateTime(DateTime.Now);
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");
            today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
        }
        catch { /* usa DateTime.Now */ }

        var plan = new Planificacion
        {
            Id = Guid.NewGuid(),
            NivelId = DemoCurriculumSeed.NivelId,
            NivelAsignaturaId = DemoCurriculumSeed.NivelAsignaturaId,
            UnidadId = DemoCurriculumSeed.UnidadId,
            Nombre = "Plan hoy",
            FechaInicio = today.AddDays(-1),
            FechaFin = today.AddDays(14),
            InstitutionId = _inst,
            OwnerUserId = _user
        };
        _db.Planificaciones.Add(plan);
        _db.Clases.Add(new Clase
        {
            Id = Guid.NewGuid(),
            PlanificacionId = plan.Id,
            Numero = 1,
            Fecha = today,
            ObjetivoAprendizajeId = DemoCurriculumSeed.Oa1Id,
            Estado = EstadoClase.Planificada
        });
        await _db.SaveChangesAsync();

        var dash = await _svc.GetDashboardAsync();
        Assert.Contains(dash.TodayClasses, c => c.PlanningName == "Plan hoy");
        Assert.Contains(dash.NextClasses, c => c.ObjectiveCode.Length > 0);
    }

    [Fact]
    public async Task EvaluacionFormativa_AlineadaAOa()
    {
        var assessment = await _svc.CreateAssessmentAsync(new CreateLearningAssessmentRequest
        {
            InstitutionId = _inst,
            Purpose = EvaluationPurpose.Formative,
            Name = "Ticket de salida OA 01",
            Date = new DateOnly(2026, 3, 10),
            Criteria = "Indicadores de evaluación de la unidad",
            ObjectiveLearningId = DemoCurriculumSeed.Oa1Id
        });
        Assert.Equal(EvaluationPurpose.Formative, assessment.Purpose);
        var list = await _svc.ListAssessmentsAsync(null);
        Assert.Contains(list, a => a.Id == assessment.Id);
    }

    [Fact]
    public async Task InscribirEstudiante_ApareceEnNominaDelCursoYDeLaClase()
    {
        DemoCurriculumSeed.Seed(_db);
        var periodId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        _db.AcademicPeriods.Add(new AcademicPeriod
        {
            Id = periodId,
            InstitutionId = _inst,
            Name = "2026",
            Year = 2026,
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 12, 15),
            IsCurrent = true
        });
        _db.SchoolCourses.Add(new SchoolCourse
        {
            Id = courseId,
            InstitutionId = _inst,
            AcademicPeriodId = periodId,
            LevelId = DemoCurriculumSeed.NivelId,
            Name = "4B",
            Section = "B",
            DisplayName = "4° básico B"
        });
        await _db.SaveChangesAsync();

        var student = await _svc.CreateStudentAsync(new CreateStudentRequest
        {
            InstitutionId = _inst,
            FirstName = "Luis",
            LastName = "Perez"
        });
        await _svc.EnrollAsync(courseId, new EnrollStudentRequest { StudentId = student.Id });
        var roster = await _svc.GetRosterAsync(courseId);
        Assert.Contains(roster.Students, s => s.StudentId == student.Id);

        var planId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _db.Planificaciones.Add(new Planificacion
        {
            Id = planId,
            NivelId = DemoCurriculumSeed.NivelId,
            NivelAsignaturaId = DemoCurriculumSeed.NivelAsignaturaId,
            UnidadId = DemoCurriculumSeed.UnidadId,
            Nombre = "Unidad 1",
            FechaInicio = new DateOnly(2026, 3, 1),
            FechaFin = new DateOnly(2026, 4, 1),
            InstitutionId = _inst,
            SchoolCourseId = courseId,
            OwnerUserId = _user
        });
        _db.Clases.Add(new Clase
        {
            Id = classId,
            PlanificacionId = planId,
            Numero = 1,
            Fecha = new DateOnly(2026, 3, 5),
            ObjetivoAprendizajeId = DemoCurriculumSeed.Oa1Id
        });
        await _db.SaveChangesAsync();

        var classRoster = await _svc.GetRosterForClassAsync(classId);
        Assert.Equal(courseId, classRoster.CourseId);
        Assert.Single(classRoster.Students);
        Assert.Equal(student.Id, classRoster.Students[0].StudentId);
    }

    [Fact]
    public async Task EvaluacionDeClase_CreaYGuardaPuntajesDeLaNomina()
    {
        DemoCurriculumSeed.Seed(_db);
        var periodId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        _db.AcademicPeriods.Add(new AcademicPeriod
        {
            Id = periodId,
            InstitutionId = _inst,
            Name = "2026",
            Year = 2026,
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 12, 15),
            IsCurrent = true
        });
        _db.SchoolCourses.Add(new SchoolCourse
        {
            Id = courseId,
            InstitutionId = _inst,
            AcademicPeriodId = periodId,
            LevelId = DemoCurriculumSeed.NivelId,
            Name = "4B",
            DisplayName = "4°B"
        });
        var planId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _db.Planificaciones.Add(new Planificacion
        {
            Id = planId,
            NivelId = DemoCurriculumSeed.NivelId,
            NivelAsignaturaId = DemoCurriculumSeed.NivelAsignaturaId,
            UnidadId = DemoCurriculumSeed.UnidadId,
            Nombre = "U1",
            FechaInicio = new DateOnly(2026, 3, 1),
            FechaFin = new DateOnly(2026, 4, 1),
            InstitutionId = _inst,
            SchoolCourseId = courseId,
            OwnerUserId = _user
        });
        _db.Clases.Add(new Clase
        {
            Id = classId,
            PlanificacionId = planId,
            Numero = 1,
            Fecha = new DateOnly(2026, 3, 5),
            ObjetivoAprendizajeId = DemoCurriculumSeed.Oa1Id
        });
        await _db.SaveChangesAsync();

        var student = await _svc.CreateStudentAsync(new CreateStudentRequest
        {
            InstitutionId = _inst,
            FirstName = "Eva",
            LastName = "Rojas"
        });
        await _svc.EnrollAsync(courseId, new EnrollStudentRequest { StudentId = student.Id });

        var assessment = await _svc.CreateAssessmentAsync(new CreateLearningAssessmentRequest
        {
            InstitutionId = Guid.Empty,
            ClassId = classId,
            Purpose = EvaluationPurpose.Formative,
            Name = "Ticket de salida",
            Date = new DateOnly(2026, 3, 5)
        });
        Assert.Equal(classId, assessment.ClassId);
        Assert.Equal(DemoCurriculumSeed.Oa1Id, assessment.ObjectiveLearningId);

        var ofClass = await _svc.ListAssessmentsAsync(null, classId);
        Assert.Contains(ofClass, a => a.Id == assessment.Id);

        await _svc.SaveScoresAsync(assessment.Id,
        [
            new SaveAssessmentScoreRequest
            {
                StudentId = student.Id,
                Score = 6.0m,
                AchievementLevel = "Logrado",
                Feedback = "Explica el OA con ejemplo."
            }
        ]);
        var scores = await _svc.GetScoresAsync(assessment.Id);
        Assert.Single(scores);
        Assert.Equal(student.Id, scores[0].StudentId);
        Assert.Equal("Eva Rojas", scores[0].StudentName);
        Assert.Equal(6.0m, scores[0].Score);
        Assert.Equal("Logrado", scores[0].AchievementLevel);

        var evidence = await _svc.GetAssessmentEvidenceAsync(assessment.Id);
        Assert.Equal(DemoCurriculumSeed.Oa1Id, evidence.ObjectiveId);
        Assert.False(string.IsNullOrWhiteSpace(evidence.ObjectiveCode));
        Assert.Equal(1, evidence.CountLogrado);
        Assert.False(evidence.NeedsReinforcement);
        Assert.Contains(evidence.ObjectiveCode, evidence.ReadingSummary);

        var stored = await _db.ClassLearningEvidences.AsNoTracking()
            .Where(e => e.ClassId == classId && e.Notes == $"assessment:{assessment.Id}")
            .ToListAsync();
        Assert.Single(stored);
        Assert.Equal(LearningEvidenceType.FormativeAssessment, stored[0].EvidenceType);
    }

    [Fact]
    public async Task EvidenciaPorOa_SugiereRefuerzoCuandoMayoriaPorLograr()
    {
        DemoCurriculumSeed.Seed(_db);
        var periodId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _db.AcademicPeriods.Add(new AcademicPeriod
        {
            Id = periodId,
            InstitutionId = _inst,
            Name = "2026",
            Year = 2026,
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 12, 15),
            IsCurrent = true
        });
        _db.SchoolCourses.Add(new SchoolCourse
        {
            Id = courseId,
            InstitutionId = _inst,
            AcademicPeriodId = periodId,
            LevelId = DemoCurriculumSeed.NivelId,
            Name = "4A",
            Section = "A",
            DisplayName = "4° básico A"
        });
        _db.Planificaciones.Add(new Planificacion
        {
            Id = planId,
            NivelId = DemoCurriculumSeed.NivelId,
            NivelAsignaturaId = DemoCurriculumSeed.NivelAsignaturaId,
            UnidadId = DemoCurriculumSeed.UnidadId,
            Nombre = "Unidad refuerzo",
            FechaInicio = new DateOnly(2026, 3, 1),
            FechaFin = new DateOnly(2026, 4, 1),
            InstitutionId = _inst,
            SchoolCourseId = courseId,
            OwnerUserId = _user
        });
        _db.Clases.Add(new Clase
        {
            Id = classId,
            PlanificacionId = planId,
            Numero = 2,
            Fecha = new DateOnly(2026, 3, 12),
            ObjetivoAprendizajeId = DemoCurriculumSeed.Oa1Id
        });
        await _db.SaveChangesAsync();

        var a = await _svc.CreateStudentAsync(new CreateStudentRequest { InstitutionId = _inst, FirstName = "Ana", LastName = "Uno" });
        var b = await _svc.CreateStudentAsync(new CreateStudentRequest { InstitutionId = _inst, FirstName = "Ben", LastName = "Dos" });
        await _svc.EnrollAsync(courseId, new EnrollStudentRequest { StudentId = a.Id });
        await _svc.EnrollAsync(courseId, new EnrollStudentRequest { StudentId = b.Id });

        var assessment = await _svc.CreateAssessmentAsync(new CreateLearningAssessmentRequest
        {
            InstitutionId = _inst,
            ClassId = classId,
            Purpose = EvaluationPurpose.Formative,
            Name = "Chequeo OA",
            Date = new DateOnly(2026, 3, 12)
        });

        await _svc.SaveScoresAsync(assessment.Id,
        [
            new SaveAssessmentScoreRequest { StudentId = a.Id, AchievementLevel = "Por lograr" },
            new SaveAssessmentScoreRequest { StudentId = b.Id, AchievementLevel = "Por lograr" }
        ]);

        var evidence = await _svc.GetAssessmentEvidenceAsync(assessment.Id);
        Assert.True(evidence.NeedsReinforcement);
        Assert.Equal(2, evidence.CountPorLograr);
        Assert.Contains("refuerzo", evidence.ReadingSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ana Uno", evidence.StudentsNeedingSupport);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { /* ignore */ }
    }
}
