using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models;
using ProfeAsistente.Api.Models.Institutions;
using ProfeAsistente.Api.Services.Authorization;
using ProfeAsistente.Api.Services.DateTimeServices;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using ProfeAsistente.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services.Classroom;

public interface ITeacherDashboardService
{
    Task<TeacherDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public sealed class TeacherDashboardService : ITeacherDashboardService
{
    private readonly ProfeAsistenteDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly ClassroomAccess _access;
    private readonly IApplicationClock _clock;
    private readonly ITimeZoneService _timeZones;

    public TeacherDashboardService(
        ProfeAsistenteDbContext db,
        ICurrentUserService current,
        ClassroomAccess access,
        IApplicationClock clock,
        ITimeZoneService timeZones)
    {
        _db = db;
        _current = current;
        _access = access;
        _clock = clock;
        _timeZones = timeZones;
    }

    public async Task<TeacherDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomView, AppPermissions.PlanningViewOwn);
        var userId = _current.UserId;
        var inst = _current.ActiveInstitutionId;
        var localNow = await LocalNowAsync(cancellationToken);
        var today = DateOnly.FromDateTime(localNow);

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
            Greeting = BuildGreeting(teacherName, localNow),
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

    private async Task<DateTime> LocalNowAsync(CancellationToken cancellationToken)
    {
        string? tzId = null;
        if (_current.ActiveInstitutionId is Guid instId)
        {
            tzId = await _db.EducationalInstitutions.AsNoTracking()
                .Where(i => i.Id == instId)
                .Select(i => i.TimeZoneId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(tzId))
            tzId = _current.TimeZoneId;
        if (string.IsNullOrWhiteSpace(tzId))
            tzId = "America/Santiago";

        if (!_timeZones.TryResolve(tzId, out var tz))
            _timeZones.TryResolve("America/Santiago", out tz);

        return TimeZoneInfo.ConvertTimeFromUtc(_clock.UtcNow, tz);
    }

    private static string BuildGreeting(string teacherName, DateTime localNow)
    {
        var hour = localNow.Hour;
        var saludo = hour < 12 ? "Buenos días" : hour < 19 ? "Buenas tardes" : "Buenas noches";
        var shortName = teacherName.Contains('@') ? teacherName.Split('@')[0] : teacherName;
        if (string.Equals(shortName, "admin", StringComparison.OrdinalIgnoreCase))
            shortName = "docente";
        return $"{saludo}, {shortName}";
    }
}
