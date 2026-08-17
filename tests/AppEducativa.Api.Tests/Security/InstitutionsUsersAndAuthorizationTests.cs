using System.Net;
using System.Net.Http.Json;
using AppEducativa.Api.Data;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Tests.Security;

[Collection("Security")]
public class InstitutionsUsersAndAuthorizationTests : IAsyncLifetime
{
    private ApiTestHost _host = null!;

    public async Task InitializeAsync() => _host = await ApiTestHost.StartAsync();

    public Task DisposeAsync() => _host.DisposeAsync().AsTask();

    [Fact]
    public async Task FlujoCompleto_InstitucionesCursosYAislamiento()
    {
        var admin = await _host.LoginAsync("admin", "Admin!Pass123");

        // Establecimientos A y B
        var instA = await CreateInstitutionAsync(admin.AccessToken, "Colegio Norte");
        var instB = await CreateInstitutionAsync(admin.AccessToken, "Colegio Sur");

        // Profesores
        var teacherA = await CreateUserAsync(admin.AccessToken, "profA", "profA@test.local", "Teacher!Pass123");
        var teacherB = await CreateUserAsync(admin.AccessToken, "profB", "profB@test.local", "Teacher!Pass123");
        var reviewerA = await CreateUserAsync(admin.AccessToken, "revA", "revA@test.local", "Reviewer!Pass1", "Reviewer");

        await AddMemberAsync(admin.AccessToken, instA.Id, teacherA.Id, ApplicationRole.Teacher);
        await AddMemberAsync(admin.AccessToken, instB.Id, teacherB.Id, ApplicationRole.Teacher);
        await AddMemberAsync(admin.AccessToken, instA.Id, reviewerA.Id, ApplicationRole.Reviewer);

        var period = await CreatePeriodAsync(admin.AccessToken, instA.Id);
        var course = await CreateCourseAsync(admin.AccessToken, instA.Id, period.Id);
        var subject = await AddSubjectAsync(admin.AccessToken, course.Id);
        await AssignTeacherAsync(admin.AccessToken, subject.Id, teacherA.Id);

        // Profesor principal duplicado
        using var dupReq = _host.Auth(HttpMethod.Post, $"api/course-subjects/{subject.Id}/teachers", admin.AccessToken, instA.Id,
            new AssignTeacherRequest
            {
                UserId = teacherB.Id,
                AssignmentType = TeacherAssignmentType.PrimaryTeacher,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                IsPrimary = true
            });
        using var dup = await _host.Client.SendAsync(dupReq);
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        // Login profesores
        var loginA = await _host.LoginAsync("profA", "Teacher!Pass123", instA.Id);
        var loginB = await _host.LoginAsync("profB", "Teacher!Pass123", instB.Id);

        Assert.Contains(loginA.Permissions, p => p.Contains("Planning", StringComparison.OrdinalIgnoreCase));

        // Planificación del profesor A
        using var createPlanReq = _host.Auth(HttpMethod.Post, "api/planificaciones", loginA.AccessToken, instA.Id,
            new CrearPlanificacionRequest
            {
                NivelId = DemoCurriculumSeed.NivelId,
                AsignaturaId = DemoCurriculumSeed.NivelAsignaturaId,
                UnidadId = DemoCurriculumSeed.UnidadId,
                Nombre = "Plan Norte",
                FechaInicio = new DateOnly(2026, 3, 1),
                FechaFin = new DateOnly(2026, 3, 31),
                InstitutionId = instA.Id,
                AcademicPeriodId = period.Id,
                SchoolCourseId = course.Id,
                CourseSubjectId = subject.Id,
                Visibility = PlanningVisibility.Private
            });
        using var createPlan = await _host.Client.SendAsync(createPlanReq);
        createPlan.EnsureSuccessStatusCode();
        var plan = await createPlan.Content.ReadFromJsonAsync<PlanificacionDetalleDto>(ApiTestHost.Json);
        Assert.NotNull(plan);

        // Clase
        using var claseReq = _host.Auth(HttpMethod.Post, $"api/planificaciones/{plan!.Id}/clases", loginA.AccessToken, instA.Id,
            new CrearClaseRequest
            {
                ObjetivoAprendizajeId = DemoCurriculumSeed.Oa1Id,
                NivelBloom = "Recordar",
                Fecha = new DateOnly(2026, 3, 5)
            });
        using var clase = await _host.Client.SendAsync(claseReq);
        clase.EnsureSuccessStatusCode();

        // Profesor B no accede (404 seguro)
        using var spyReq = _host.Auth(HttpMethod.Get, $"api/planificaciones/{plan.Id}", loginB.AccessToken, instB.Id);
        using var spy = await _host.Client.SendAsync(spyReq);
        Assert.True(spy.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden);

        // Profesor B intenta listar instituciones ajenas
        using var listBReq = _host.Auth(HttpMethod.Get, "api/institutions", loginB.AccessToken, instB.Id);
        using var listB = await _host.Client.SendAsync(listBReq);
        listB.EnsureSuccessStatusCode();
        var institutionsB = await listB.Content.ReadFromJsonAsync<List<InstitutionDto>>(ApiTestHost.Json) ?? [];
        Assert.DoesNotContain(institutionsB, i => i.Id == instA.Id);

        // Exportar A ok; B denegado
        using var exportAReq = _host.Auth(HttpMethod.Post, $"api/planificaciones/{plan.Id}/exportar", loginA.AccessToken, instA.Id);
        using var exportA = await _host.Client.SendAsync(exportAReq);
        exportA.EnsureSuccessStatusCode();

        using var exportBReq = _host.Auth(HttpMethod.Post, $"api/planificaciones/{plan.Id}/exportar", loginB.AccessToken, instB.Id);
        using var exportB = await _host.Client.SendAsync(exportBReq);
        Assert.True(exportB.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden);

        // Auditoría admin
        using var auditReq = _host.Auth(HttpMethod.Get, "api/admin/audit", admin.AccessToken);
        using var audit = await _host.Client.SendAsync(auditReq);
        audit.EnsureSuccessStatusCode();

        // Desactivar profesor A → refresh inválido
        using var deactReq = _host.Auth(HttpMethod.Post, $"api/admin/users/{teacherA.Id}/deactivate", admin.AccessToken);
        using var deact = await _host.Client.SendAsync(deactReq);
        Assert.Equal(HttpStatusCode.NoContent, deact.StatusCode);

        using var refreshA = await _host.Client.PostAsJsonAsync("api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = loginA.RefreshToken }, ApiTestHost.Json);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshA.StatusCode);

        // Teacher no administra usuarios
        using var usersReq = _host.Auth(HttpMethod.Get, "api/admin/users", loginB.AccessToken, instB.Id);
        using var users = await _host.Client.SendAsync(usersReq);
        Assert.Equal(HttpStatusCode.Forbidden, users.StatusCode);
    }

    [Fact]
    public async Task Lockout_TrasIntentosFallidos()
    {
        var admin = await _host.LoginAsync("admin", "Admin!Pass123");
        var lockUser = await CreateUserAsync(admin.AccessToken, "lockoutuser", "lockout@test.local", "Lockout!Pass1");

        for (var i = 0; i < 5; i++)
        {
            using var fail = await _host.Client.PostAsJsonAsync("api/auth/login", new LoginRequest
            {
                UserNameOrEmail = "lockoutuser",
                Password = "Wrong!Pass000"
            }, ApiTestHost.Json);
            Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
        }

        using var locked = await _host.Client.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            UserNameOrEmail = "lockoutuser",
            Password = "Lockout!Pass1"
        }, ApiTestHost.Json);
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.NotNull(lockUser);
    }

    private async Task<InstitutionDto> CreateInstitutionAsync(string token, string name)
    {
        using var req = _host.Auth(HttpMethod.Post, "api/institutions", token,
            body: new CreateInstitutionRequest { Name = name });
        using var response = await _host.Client.SendAsync(req);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InstitutionDto>(ApiTestHost.Json))!;
    }

    private async Task<UserSummaryDto> CreateUserAsync(string token, string user, string email, string password, string role = "Teacher")
    {
        using var req = _host.Auth(HttpMethod.Post, "api/admin/users", token, body: new CreateUserRequest
        {
            UserName = user,
            Email = email,
            Password = password,
            FirstName = user,
            LastName = "Test",
            Roles = [role],
            MustChangePassword = false
        });
        using var response = await _host.Client.SendAsync(req);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserSummaryDto>(ApiTestHost.Json))!;
    }

    private async Task AddMemberAsync(string token, Guid institutionId, Guid userId, ApplicationRole role)
    {
        using var req = _host.Auth(HttpMethod.Post, $"api/institutions/{institutionId}/members", token,
            body: new AddMembershipRequest { UserId = userId, Role = role });
        using var response = await _host.Client.SendAsync(req);
        response.EnsureSuccessStatusCode();
    }

    private async Task<AcademicPeriodDto> CreatePeriodAsync(string token, Guid institutionId)
    {
        using var req = _host.Auth(HttpMethod.Post, $"api/institutions/{institutionId}/academic-periods", token, institutionId,
            new CreateAcademicPeriodRequest
            {
                Name = "2026",
                Year = 2026,
                StartDate = new DateOnly(2026, 3, 1),
                EndDate = new DateOnly(2026, 12, 15),
                IsCurrent = true
            });
        using var response = await _host.Client.SendAsync(req);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AcademicPeriodDto>(ApiTestHost.Json))!;
    }

    private async Task<SchoolCourseDto> CreateCourseAsync(string token, Guid institutionId, Guid periodId)
    {
        using var req = _host.Auth(HttpMethod.Post, $"api/institutions/{institutionId}/courses", token, institutionId,
            new CreateSchoolCourseRequest
            {
                AcademicPeriodId = periodId,
                LevelId = DemoCurriculumSeed.NivelId,
                Name = "4° Básico",
                Section = "A"
            });
        using var response = await _host.Client.SendAsync(req);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SchoolCourseDto>(ApiTestHost.Json))!;
    }

    private async Task<CourseSubjectDto> AddSubjectAsync(string token, Guid courseId)
    {
        using var req = _host.Auth(HttpMethod.Post, $"api/courses/{courseId}/subjects", token,
            body: new CreateCourseSubjectRequest { SubjectId = DemoCurriculumSeed.AsignaturaId, WeeklyHours = 6 });
        using var response = await _host.Client.SendAsync(req);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CourseSubjectDto>(ApiTestHost.Json))!;
    }

    private async Task AssignTeacherAsync(string token, Guid courseSubjectId, Guid userId)
    {
        using var req = _host.Auth(HttpMethod.Post, $"api/course-subjects/{courseSubjectId}/teachers", token,
            body: new AssignTeacherRequest
            {
                UserId = userId,
                AssignmentType = TeacherAssignmentType.PrimaryTeacher,
                StartDate = new DateOnly(2026, 3, 1),
                IsPrimary = true
            });
        using var response = await _host.Client.SendAsync(req);
        response.EnsureSuccessStatusCode();
    }
}
