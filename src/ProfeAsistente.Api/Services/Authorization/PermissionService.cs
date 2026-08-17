using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models;
using ProfeAsistente.Shared.Enums;
using ProfeAsistente.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services.Authorization;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(Guid userId, Guid? institutionId, CancellationToken cancellationToken = default);
}

public interface IResourceAuthorizationService
{
    Task<bool> CanAccessPlanningAsync(Guid planningId, string operation, CancellationToken cancellationToken = default);
    Task EnsureCanAccessPlanningAsync(Guid planningId, string operation, CancellationToken cancellationToken = default);
    Task EnsureCanAccessExportAsync(Guid exportId, CancellationToken cancellationToken = default);
}

public sealed class PermissionService : IPermissionService
{
    private readonly ProfeAsistenteDbContext _db;

    public PermissionService(ProfeAsistenteDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(
        Guid userId, Guid? institutionId, CancellationToken cancellationToken = default)
    {
        var globalRoles = await _db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
            .ToListAsync(cancellationToken);

        var perms = new HashSet<string>(PermissionCatalog.ForRoles(globalRoles), StringComparer.OrdinalIgnoreCase);

        if (institutionId is Guid iid)
        {
            var membershipRole = await _db.InstitutionMemberships.AsNoTracking()
                .Where(m => m.UserId == userId && m.InstitutionId == iid && m.IsActive && !m.IsDeleted)
                .Select(m => (ApplicationRole?)m.Role)
                .FirstOrDefaultAsync(cancellationToken);
            if (membershipRole is ApplicationRole mr)
            {
                foreach (var p in PermissionCatalog.ForMembershipRole(mr))
                    perms.Add(p);
            }
        }

        return perms.OrderBy(x => x).ToList();
    }
}

public sealed class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly ProfeAsistenteDbContext _db;
    private readonly ICurrentUserService _current;

    public ResourceAuthorizationService(ProfeAsistenteDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task EnsureCanAccessPlanningAsync(Guid planningId, string operation, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessPlanningAsync(planningId, operation, cancellationToken))
            throw new UnauthorizedAccessException("No tiene acceso a esta planificación.");
    }

    public async Task<bool> CanAccessPlanningAsync(Guid planningId, string operation, CancellationToken cancellationToken = default)
    {
        if (!_current.IsAuthenticated || _current.UserId is null)
            return false;

        if (_current.IsInRole(nameof(ApplicationRole.SystemAdministrator)))
            return true;

        var plan = await _db.Planificaciones.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planningId && !p.IsDeleted, cancellationToken);
        if (plan is null) return false;

        // Legacy plans without ownership: allow teachers with Planning permissions in development-like unscoped data
        if (plan.InstitutionId is null && plan.OwnerUserId is null)
            return _current.HasPermission(AppPermissions.PlanningViewOwn)
                   || _current.HasPermission(AppPermissions.PlanningViewInstitution);

        if (plan.InstitutionId is Guid iid && !_current.InstitutionIds.Contains(iid))
            return false;

        var isOwner = plan.OwnerUserId == _current.UserId;
        var op = operation.ToLowerInvariant();

        if (op is "view")
        {
            if (isOwner && _current.HasPermission(AppPermissions.PlanningViewOwn)) return true;
            if (_current.HasPermission(AppPermissions.PlanningViewInstitution)) return true;
            if (plan.Visibility == PlanningVisibility.CourseTeachers && plan.CourseSubjectId is Guid csId)
                return await IsAssignedToCourseSubjectAsync(csId, cancellationToken);
            if (plan.Visibility == PlanningVisibility.Institution)
                return _current.InstitutionIds.Contains(plan.InstitutionId!.Value);
            return false;
        }

        if (op is "update" or "delete")
        {
            if (_current.HasPermission(AppPermissions.PlanningUpdateAny) || _current.HasPermission(AppPermissions.PlanningDeleteAny))
                return true;
            return isOwner && (_current.HasPermission(AppPermissions.PlanningUpdateOwn)
                               || _current.HasPermission(AppPermissions.PlanningDeleteOwn));
        }

        if (op is "export")
            return await CanAccessPlanningAsync(planningId, "view", cancellationToken)
                   && _current.HasPermission(AppPermissions.MaterialsExport);

        return false;
    }

    public async Task EnsureCanAccessExportAsync(Guid exportId, CancellationToken cancellationToken = default)
    {
        if (!_current.IsAuthenticated || _current.UserId is null)
            throw new UnauthorizedAccessException("No autenticado.");

        if (_current.IsInRole(nameof(ApplicationRole.SystemAdministrator)))
            return;

        var export = await _db.DocumentExports.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exportId && !e.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Exportación no encontrada.");

        if (export.PlanningId is Guid pid)
            await EnsureCanAccessPlanningAsync(pid, "export", cancellationToken);
        else if (!_current.HasPermission(AppPermissions.MaterialsExport))
            throw new UnauthorizedAccessException("No tiene permiso para exportar.");
    }

    private Task<bool> IsAssignedToCourseSubjectAsync(Guid courseSubjectId, CancellationToken cancellationToken) =>
        _db.CourseTeacherAssignments.AsNoTracking().AnyAsync(a =>
            a.CourseSubjectId == courseSubjectId
            && a.UserId == _current.UserId
            && a.IsActive
            && !a.IsDeleted, cancellationToken);
}
