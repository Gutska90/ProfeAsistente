using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Services.Auth;
using ProfeAsistente.Api.Services.Institutions;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.CanManageUsers)]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserAdminService _users;
    private readonly ProfeAsistenteDbContext _db;

    public AdminUsersController(IUserAdminService users, ProfeAsistenteDbContext db)
    {
        _users = users;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> List(CancellationToken ct)
        => Ok(await _users.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserSummaryDto>> Get(Guid id, CancellationToken ct)
    {
        var u = await _users.GetAsync(id, ct);
        return u is null ? NotFound() : Ok(u);
    }

    [HttpPost]
    public async Task<ActionResult<UserSummaryDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        try { return Ok(await _users.CreateAsync(request, ct)); }
        catch (AuthException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserSummaryDto>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
        => Ok(await _users.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await _users.SetActiveAsync(id, true, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _users.SetActiveAsync(id, false, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> Roles(Guid id, [FromBody] AssignRolesRequest request, CancellationToken ct)
    {
        await _users.AssignRolesAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/roles/{role}")]
    public async Task<IActionResult> RemoveRole(Guid id, string role, CancellationToken ct)
    {
        var current = await _users.GetAsync(id, ct);
        if (current is null) return NotFound();
        var roles = current.Roles.Where(r => !r.Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();
        await _users.AssignRolesAsync(id, new AssignRolesRequest { Roles = roles }, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/force-password-change")]
    public async Task<IActionResult> ForcePassword(Guid id, CancellationToken ct)
    {
        await _users.ForcePasswordChangeAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult<object>> ResetPassword(Guid id, [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        try
        {
            await _users.AdminResetPasswordAsync(id, request.NewPassword, ct);
            return Ok(new { message = "Contraseña restablecida. El usuario debe cambiarla." });
        }
        catch (AuthException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}/memberships")]
    public async Task<ActionResult<IReadOnlyList<InstitutionMembershipDto>>> Memberships(Guid id, CancellationToken ct)
    {
        var list = await _db.InstitutionMemberships.AsNoTracking()
            .Where(m => m.UserId == id && !m.IsDeleted)
            .Join(_db.EducationalInstitutions, m => m.InstitutionId, i => i.Id, (m, i) => new InstitutionMembershipDto
            {
                Id = m.Id,
                InstitutionId = m.InstitutionId,
                InstitutionName = i.Name,
                UserId = m.UserId,
                Role = m.Role,
                IsActive = m.IsActive
            }).ToListAsync(ct);
        return Ok(list);
    }
}

[ApiController]
[Authorize(Policy = AppPolicies.CanViewAudit)]
[Route("api/admin/audit")]
public class AdminAuditController : ControllerBase
{
    private readonly ProfeAsistenteDbContext _db;

    public AdminAuditController(ProfeAsistenteDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<object>> List(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? institutionId,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool? success,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = _db.AuditEvents.AsNoTracking().AsQueryable();
        if (userId is not null) q = q.Where(a => a.UserId == userId);
        if (institutionId is not null) q = q.Where(a => a.InstitutionId == institutionId);
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(a => a.Action == action);
        if (from is not null) q = q.Where(a => a.Timestamp >= from);
        if (to is not null) q = q.Where(a => a.Timestamp <= to);
        if (success is not null) q = q.Where(a => a.Success == success);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditEventDto
            {
                Id = a.Id,
                UserId = a.UserId,
                InstitutionId = a.InstitutionId,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Success = a.Success,
                Timestamp = a.Timestamp,
                FailureReason = a.FailureReason
            }).ToListAsync(ct);
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuditEventDto>> Get(Guid id, CancellationToken ct)
    {
        var a = await _db.AuditEvents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return a is null ? NotFound() : Ok(new AuditEventDto
        {
            Id = a.Id,
            UserId = a.UserId,
            InstitutionId = a.InstitutionId,
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            Success = a.Success,
            Timestamp = a.Timestamp,
            FailureReason = a.FailureReason
        });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var items = await _db.AuditEvents.AsNoTracking().OrderByDescending(a => a.Timestamp).Take(5000)
            .Select(a => $"{a.Timestamp:o},{a.Action},{a.UserId},{a.InstitutionId},{a.Success},{a.EntityType},{a.EntityId}")
            .ToListAsync(ct);
        var csv = "Timestamp,Action,UserId,InstitutionId,Success,EntityType,EntityId\n" + string.Join('\n', items);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "audit.csv");
    }
}
