using ProfeAsistente.Api.Services.Authorization;

namespace ProfeAsistente.Api.Tests.TestDoubles;

public sealed class FakeCurrentUserService : ICurrentUserService
{
    public bool IsAuthenticated { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; } = "test";
    public IReadOnlyList<string> Roles { get; set; } = [];
    public IReadOnlyList<string> Permissions { get; set; } = [];
    public IReadOnlyList<Guid> InstitutionIds { get; set; } = [];
    public Guid? ActiveInstitutionId { get; set; }
    public string TimeZoneId { get; set; } = "America/Santiago";

    public bool HasPermission(string permission) =>
        !IsAuthenticated || Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase)
        || Roles.Contains("SystemAdministrator", StringComparer.OrdinalIgnoreCase);

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}

public sealed class AllowAllResourceAuthorizationService : IResourceAuthorizationService
{
    public Task<bool> CanAccessPlanningAsync(Guid planningId, string operation, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task EnsureCanAccessPlanningAsync(Guid planningId, string operation, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task EnsureCanAccessExportAsync(Guid exportId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
