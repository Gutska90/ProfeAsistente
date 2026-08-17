using System.Security.Claims;
using ProfeAsistente.Shared.Security;

namespace ProfeAsistente.Api.Services.Authorization;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? UserName { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    IReadOnlyList<Guid> InstitutionIds { get; }
    Guid? ActiveInstitutionId { get; }
    string TimeZoneId { get; }
    bool HasPermission(string permission);
    bool IsInRole(string role);
}

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var id = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(id, out var g) ? g : null;
        }
    }

    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name)
                               ?? Principal?.Identity?.Name;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        ?? [];

    public IReadOnlyList<string> Permissions =>
        Principal?.FindAll("permission").Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        ?? [];

    public IReadOnlyList<Guid> InstitutionIds =>
        Principal?.FindAll("institution")
            .Select(c => Guid.TryParse(c.Value, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .Distinct()
            .ToList()
        ?? [];

    public Guid? ActiveInstitutionId
    {
        get
        {
            var header = _http.HttpContext?.Request.Headers["X-Institution-Id"].FirstOrDefault();
            if (Guid.TryParse(header, out var fromHeader) && InstitutionIds.Contains(fromHeader))
                return fromHeader;
            var claim = Principal?.FindFirstValue("active_institution");
            return Guid.TryParse(claim, out var g) ? g : InstitutionIds.FirstOrDefault();
        }
    }

    public string TimeZoneId =>
        Principal?.FindFirstValue("timezone") ?? "America/Santiago";

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase)
        || Roles.Contains(nameof(Shared.Enums.ApplicationRole.SystemAdministrator), StringComparer.OrdinalIgnoreCase);

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
