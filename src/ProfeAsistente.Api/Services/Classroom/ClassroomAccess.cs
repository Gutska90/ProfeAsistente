using ProfeAsistente.Api.Services.Authorization;
using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Services.Classroom;

public sealed class ClassroomAccess(ICurrentUserService current)
{
    public void Ensure(params string[] permissions)
    {
        if (current.IsInRole(nameof(ApplicationRole.SystemAdministrator))) return;
        if (permissions.Any(current.HasPermission)) return;
        throw new UnauthorizedAccessException("No tiene permiso para esta acción de aula.");
    }

    public void EnsureInstitution(Guid institutionId)
    {
        if (current.IsInRole(nameof(ApplicationRole.SystemAdministrator))) return;
        if (!current.InstitutionIds.Contains(institutionId) && current.ActiveInstitutionId != institutionId)
            throw new UnauthorizedAccessException("No tiene acceso a este establecimiento.");
    }
}
