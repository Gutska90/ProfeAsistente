using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models;
using ProfeAsistente.Api.Repositories;
using ProfeAsistente.Api.Services.Authorization;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using ProfeAsistente.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services;

public interface IPlanificacionService
{
    Task<PlanificacionDetalleDto> CrearAsync(CrearPlanificacionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlanificacionDto>> ListarAsync(CancellationToken cancellationToken = default);
    Task<PlanificacionDetalleDto?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default);
}

public class PlanificacionService : IPlanificacionService
{
    private readonly ProfeAsistenteDbContext _db;
    private readonly IPlanificacionRepository _repo;
    private readonly ICurrentUserService _current;
    private readonly IResourceAuthorizationService _authz;

    public PlanificacionService(
        ProfeAsistenteDbContext db,
        IPlanificacionRepository repo,
        ICurrentUserService current,
        IResourceAuthorizationService authz)
    {
        _db = db;
        _repo = repo;
        _current = current;
        _authz = authz;
    }

    public async Task<PlanificacionDetalleDto> CrearAsync(CrearPlanificacionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.FechaFin < request.FechaInicio)
            throw new ArgumentException("FechaInicio debe ser menor o igual a FechaFin.");
        if (request.NivelId == Guid.Empty || request.AsignaturaId == Guid.Empty || request.UnidadId == Guid.Empty)
            throw new ArgumentException("NivelId, AsignaturaId (NivelAsignatura) y UnidadId son obligatorios.");

        var nivel = await _db.Niveles.AsNoTracking().FirstOrDefaultAsync(n => n.Id == request.NivelId, cancellationToken)
            ?? throw new ArgumentException("El nivel indicado no existe.");

        var nivelAsignatura = await _db.NivelesAsignaturas.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AsignaturaId && a.Vigente &&
                (a.EstadoRevision == EstadoRevision.Aprobado || a.EstadoRevision == EstadoRevision.AprobadoParaPruebas),
                cancellationToken)
            ?? throw new ArgumentException("La asignatura (NivelAsignatura) no existe o no está publicada.");

        var unidad = await _db.Unidades.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UnidadId && u.Vigente &&
                (u.EstadoRevision == EstadoRevision.Aprobado || u.EstadoRevision == EstadoRevision.AprobadoParaPruebas),
                cancellationToken)
            ?? throw new ArgumentException("La unidad no existe o no está publicada.");

        if (nivelAsignatura.NivelId != nivel.Id || unidad.NivelAsignaturaId != nivelAsignatura.Id)
            throw new ArgumentException("Selección Nivel → Asignatura → Unidad incoherente.");

        var nombre = string.IsNullOrWhiteSpace(request.Nombre)
            ? $"Planificación — {unidad.Nombre}"
            : request.Nombre.Trim();

        if (_current.IsAuthenticated && !_current.HasPermission(AppPermissions.PlanningCreate)
            && !_current.IsInRole(nameof(ApplicationRole.SystemAdministrator)))
            throw new UnauthorizedAccessException("No tiene permiso para crear planificaciones.");

        var plan = new Planificacion
        {
            Id = Guid.NewGuid(),
            NivelId = nivel.Id,
            NivelAsignaturaId = nivelAsignatura.Id,
            UnidadId = unidad.Id,
            Nombre = nombre,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            Estado = EstadoPlanificacion.EnCurso,
            FechaCreacion = DateTime.UtcNow,
            InstitutionId = request.InstitutionId ?? _current.ActiveInstitutionId,
            AcademicPeriodId = request.AcademicPeriodId,
            SchoolCourseId = request.SchoolCourseId,
            CourseSubjectId = request.CourseSubjectId,
            OwnerUserId = _current.UserId,
            CreatedByUserId = _current.UserId,
            Visibility = request.Visibility
        };

        if (plan.InstitutionId is Guid iid && _current.IsAuthenticated
            && !_current.InstitutionIds.Contains(iid)
            && !_current.IsInRole(nameof(ApplicationRole.SystemAdministrator)))
            throw new UnauthorizedAccessException("No tiene acceso a este establecimiento.");

        var saved = await _repo.AddAsync(plan, cancellationToken);
        return PlanificacionMapper.ToDetalle(saved);
    }

    public async Task<IReadOnlyList<PlanificacionDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetAllAsync(cancellationToken);
        if (_current.IsAuthenticated && !_current.IsInRole(nameof(ApplicationRole.SystemAdministrator)))
        {
            list = list.Where(p =>
            {
                if (p.IsDeleted) return false;
                if (p.OwnerUserId == _current.UserId) return true;
                if (p.InstitutionId is Guid iid && _current.InstitutionIds.Contains(iid)
                    && (_current.HasPermission(AppPermissions.PlanningViewInstitution)
                        || p.Visibility == PlanningVisibility.Institution
                        || p.Visibility == PlanningVisibility.CourseTeachers))
                    return true;
                // Legacy without ownership remains visible to authenticated planners
                if (p.InstitutionId is null && p.OwnerUserId is null)
                    return _current.HasPermission(AppPermissions.PlanningViewOwn);
                return false;
            }).ToList();
        }

        return list.Select(p =>
        {
            var r = PlanificacionMapper.ToResumen(p);
            return new PlanificacionDto
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Nivel = r.Nivel,
                Asignatura = r.Asignatura,
                Unidad = r.Unidad,
                FechaInicio = r.FechaInicio,
                FechaFin = r.FechaFin,
                Estado = r.Estado,
                CantidadClases = r.CantidadClases,
                FechaCreacion = r.FechaCreacion
            };
        }).ToList();
    }

    public async Task<PlanificacionDetalleDto?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _repo.GetByIdAsync(id, cancellationToken);
        if (plan is null || plan.IsDeleted) return null;

        if (_current.IsAuthenticated
            && !await _authz.CanAccessPlanningAsync(id, "view", cancellationToken))
            return null;

        var dto = PlanificacionMapper.ToDetalle(plan);
        await EnrichStructureStatusesAsync(dto.Clases, cancellationToken);
        return dto;
    }

    private async Task EnrichStructureStatusesAsync(
        List<ClaseResumenDto> clases,
        CancellationToken cancellationToken)
    {
        if (clases.Count == 0) return;

        var ids = clases.Select(c => c.Id).ToList();
        var generations = await _db.ClassStructureGenerations.AsNoTracking()
            .Where(g => ids.Contains(g.ClassId) && !g.IsDeleted)
            .Select(g => new StructureGenInfo(
                g.ClassId,
                g.Status,
                g.IsCurrentVersion,
                g.IsOutdated,
                g.RequiresReview,
                g.CreatedAt))
            .ToListAsync(cancellationToken);

        foreach (var clase in clases)
        {
            var forClass = generations.Where(g => g.ClassId == clase.Id).ToList();
            var (ui, label) = ResolveStructureUiStatus(clase.TieneEstructura, forClass);
            clase.EstructuraUiStatus = ui;
            clase.EstructuraEstado = label;
            if (ui is ClassStructureUiStatus.Generated
                or ClassStructureUiStatus.RequiresReview
                or ClassStructureUiStatus.Reviewed
                or ClassStructureUiStatus.Outdated)
            {
                clase.TieneEstructura = true;
            }
        }
    }

    private static (ClassStructureUiStatus Status, string Label) ResolveStructureUiStatus(
        bool hasLegacyStructure,
        IReadOnlyList<StructureGenInfo> generations)
    {
        if (generations.Count == 0)
            return hasLegacyStructure
                ? (ClassStructureUiStatus.Generated, "Estructura generada")
                : (ClassStructureUiStatus.None, "Sin estructura");

        if (generations.Any(g => g.Status == AiGenerationStatus.Processing))
            return (ClassStructureUiStatus.Generating, "Generando");

        var current = generations
            .Where(g => g.IsCurrentVersion)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefault()
            ?? generations.OrderByDescending(g => g.CreatedAt).First();

        if (current.Status is AiGenerationStatus.Failed
            or AiGenerationStatus.RejectedByValidation
            or AiGenerationStatus.Cancelled)
            return (ClassStructureUiStatus.Error, "Error de generación");

        if (current.IsOutdated)
            return (ClassStructureUiStatus.Outdated, "Desactualizada");

        if (current.RequiresReview)
            return (ClassStructureUiStatus.RequiresReview, "Requiere revisión");

        if (current.Status == AiGenerationStatus.Completed)
            return (ClassStructureUiStatus.Generated, "Estructura generada");

        return (ClassStructureUiStatus.Generated, "Estructura generada");
    }

    private sealed record StructureGenInfo(
        Guid ClassId,
        AiGenerationStatus Status,
        bool IsCurrentVersion,
        bool IsOutdated,
        bool RequiresReview,
        DateTime CreatedAt);
}
