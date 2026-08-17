using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ProfeAsistente.Api.Configuration;
using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.Identity;
using ProfeAsistente.Api.Models.Institutions;
using ProfeAsistente.Api.Services.Authorization;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ProfeAsistente.Api.Services.Auth;

public interface IAuthenticationService
{
    Task<AuthenticationResponse> LoginAsync(LoginRequest request, string? ip, string? userAgent, CancellationToken cancellationToken = default);
    Task<AuthenticationResponse> RefreshAsync(RefreshTokenRequest request, string? ip, string? userAgent, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, string? ip, CancellationToken cancellationToken = default);
    Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, bool exposeDevToken, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<UserSessionDto> GetMeAsync(Guid userId, Guid? activeInstitutionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuthSessionDto>> GetSessionsAsync(Guid userId, string? currentRefreshToken, CancellationToken cancellationToken = default);
    Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
}

public interface IAuditService
{
    Task WriteAsync(string action, bool success, Guid? userId = null, Guid? institutionId = null,
        string? entityType = null, string? entityId = null, string? failureReason = null,
        string? detailsJson = null, string? ip = null, string? userAgent = null, CancellationToken cancellationToken = default);
}

public sealed class AuditService : IAuditService
{
    private readonly ProfeAsistenteDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ProfeAsistenteDbContext db, IHttpContextAccessor http, ILogger<AuditService> logger)
    {
        _db = db;
        _http = http;
        _logger = logger;
    }

    public async Task WriteAsync(string action, bool success, Guid? userId = null, Guid? institutionId = null,
        string? entityType = null, string? entityId = null, string? failureReason = null,
        string? detailsJson = null, string? ip = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        var ctx = _http.HttpContext;
        _db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InstitutionId = institutionId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Success = success,
            Timestamp = DateTime.UtcNow,
            IpAddress = ip ?? ctx?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Truncate(userAgent ?? ctx?.Request.Headers.UserAgent.ToString(), 300),
            TraceId = ctx?.TraceIdentifier,
            DetailsJson = detailsJson,
            FailureReason = Truncate(failureReason, 500)
        });
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Audit {Action} success={Success} user={UserId}", action, success, userId);
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value[..max]);
}

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly RoleManager<ApplicationRoleEntity> _roles;
    private readonly ProfeAsistenteDbContext _db;
    private readonly AuthenticationOptions _options;
    private readonly IPermissionService _permissions;
    private readonly IAuditService _audit;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        RoleManager<ApplicationRoleEntity> roles,
        ProfeAsistenteDbContext db,
        IOptions<AuthenticationOptions> options,
        IPermissionService permissions,
        IAuditService audit,
        SymmetricSecurityKey signingKey,
        ILogger<AuthenticationService> logger)
    {
        _users = users;
        _signIn = signIn;
        _roles = roles;
        _db = db;
        _options = options.Value;
        _permissions = permissions;
        _audit = audit;
        _signingKey = signingKey;
        _logger = logger;
    }

    public async Task<AuthenticationResponse> LoginAsync(LoginRequest request, string? ip, string? userAgent, CancellationToken cancellationToken = default)
    {
        ValidateLoginInput(request);
        var user = await FindUserAsync(request.UserNameOrEmail);
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            await _audit.WriteAsync("LoginFailed", false, failureReason: "InvalidCredentials", ip: ip, userAgent: userAgent, cancellationToken: cancellationToken);
            throw new AuthException("Las credenciales no son válidas.");
        }

        if (await _users.IsLockedOutAsync(user))
        {
            await _audit.WriteAsync("Lockout", false, user.Id, failureReason: "LockedOut", ip: ip, cancellationToken: cancellationToken);
            throw new AuthException("Las credenciales no son válidas.");
        }

        var result = await _signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            await _audit.WriteAsync("LoginFailed", false, user.Id, failureReason: result.IsLockedOut ? "LockedOut" : "InvalidPassword", ip: ip, cancellationToken: cancellationToken);
            throw new AuthException("Las credenciales no son válidas.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        var activeInstitution = await ResolveActiveInstitutionAsync(user.Id, request.InstitutionId, cancellationToken);
        var response = await IssueTokensAsync(user, activeInstitution, request.RememberMe, ip, userAgent, cancellationToken);
        await _audit.WriteAsync("LoginSucceeded", true, user.Id, activeInstitution, ip: ip, userAgent: userAgent, cancellationToken: cancellationToken);
        return response;
    }

    public async Task<AuthenticationResponse> RefreshAsync(RefreshTokenRequest request, string? ip, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new AuthException("Refresh token inválido.");

        var hash = HashToken(request.RefreshToken);
        var stored = await _db.RefreshTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            ?? throw new AuthException("Refresh token inválido.");

        if (stored.IsRevoked || stored.RevokedAt is not null)
        {
            // reuse detection: revoke all
            await RevokeAllForUserAsync(stored.UserId, ip, cancellationToken);
            await _audit.WriteAsync("RefreshReuseDetected", false, stored.UserId, ip: ip, cancellationToken: cancellationToken);
            throw new AuthException("Refresh token inválido.");
        }

        if (stored.ExpiresAt < DateTime.UtcNow || stored.User is null || !stored.User.IsActive || stored.User.IsDeleted)
            throw new AuthException("Refresh token inválido.");

        stored.IsUsed = true;
        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedByIp = ip;

        var response = await IssueTokensAsync(stored.User, stored.InstitutionId, rememberMe: true, ip, userAgent, cancellationToken);
        stored.ReplacedByTokenId = await _db.RefreshTokens.AsNoTracking()
            .Where(t => t.UserId == stored.UserId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("TokenRefreshed", true, stored.UserId, stored.InstitutionId, ip: ip, cancellationToken: cancellationToken);
        return response;
    }

    public async Task LogoutAsync(string refreshToken, string? ip, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is null) return;
        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedByIp = ip;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("Logout", true, stored.UserId, stored.InstitutionId, ip: ip, cancellationToken: cancellationToken);
    }

    public async Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await RevokeAllForUserAsync(userId, null, cancellationToken);
        await _audit.WriteAsync("LogoutAll", true, userId, cancellationToken: cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString())
                   ?? throw new AuthException("Usuario no encontrado.");
        EnsurePasswordStrength(request.NewPassword, user);
        await EnsureNotInHistoryAsync(user, request.NewPassword, cancellationToken);

        var result = await _users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new AuthException(string.Join(" ", result.Errors.Select(e => e.Description)));

        await AddPasswordHistoryAsync(user, cancellationToken);
        user.MustChangePassword = false;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        await RevokeAllForUserAsync(userId, null, cancellationToken);
        await _audit.WriteAsync("PasswordChanged", true, userId, cancellationToken: cancellationToken);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, bool exposeDevToken, CancellationToken cancellationToken = default)
    {
        var response = new ForgotPasswordResponse();
        var user = await FindUserAsync(request.UserNameOrEmail);
        if (user is null || !user.IsActive || user.IsDeleted)
            return response;

        var token = await _users.GeneratePasswordResetTokenAsync(user);
        await _audit.WriteAsync("PasswordResetRequested", true, user.Id, cancellationToken: cancellationToken);
        if (exposeDevToken)
            response.DevelopmentResetToken = token;
        return response;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(request.UserNameOrEmail)
                   ?? throw new AuthException("No se pudo restablecer la contraseña.");
        EnsurePasswordStrength(request.NewPassword, user);
        await EnsureNotInHistoryAsync(user, request.NewPassword, cancellationToken);
        var result = await _users.ResetPasswordAsync(user, request.ResetToken, request.NewPassword);
        if (!result.Succeeded)
            throw new AuthException("No se pudo restablecer la contraseña.");

        await AddPasswordHistoryAsync(user, cancellationToken);
        user.MustChangePassword = true;
        user.PasswordChangedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        await RevokeAllForUserAsync(user.Id, null, cancellationToken);
        await _audit.WriteAsync("PasswordResetCompleted", true, user.Id, cancellationToken: cancellationToken);
    }

    public async Task<UserSessionDto> GetMeAsync(Guid userId, Guid? activeInstitutionId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString())
                   ?? throw new AuthException("Usuario no encontrado.");
        return await MapSessionAsync(user, activeInstitutionId, cancellationToken);
    }

    public async Task<IReadOnlyList<AuthSessionDto>> GetSessionsAsync(Guid userId, string? currentRefreshToken, CancellationToken cancellationToken = default)
    {
        var currentHash = string.IsNullOrWhiteSpace(currentRefreshToken) ? null : HashToken(currentRefreshToken);
        return await _db.RefreshTokens.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => new AuthSessionDto
            {
                Id = t.Id,
                CreatedAt = t.CreatedAt,
                ExpiresAt = t.ExpiresAt,
                RevokedAt = t.RevokedAt,
                UserAgent = t.UserAgent,
                IsCurrent = currentHash != null && t.TokenHash == currentHash
            }).ToListAsync(cancellationToken);
    }

    public async Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Id == sessionId && t.UserId == userId, cancellationToken)
                    ?? throw new AuthException("Sesión no encontrada.");
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthenticationResponse> IssueTokensAsync(
        ApplicationUser user, Guid? institutionId, bool rememberMe, string? ip, string? userAgent, CancellationToken cancellationToken)
    {
        var roles = await _users.GetRolesAsync(user);
        var permissions = await _permissions.GetEffectivePermissionsAsync(user.Id, institutionId, cancellationToken);
        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("timezone", user.PreferredTimeZone),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));
        foreach (var p in permissions)
            claims.Add(new Claim("permission", p));

        var memberships = await _db.InstitutionMemberships.AsNoTracking()
            .Where(m => m.UserId == user.Id && m.IsActive && !m.IsDeleted)
            .Select(m => m.InstitutionId)
            .ToListAsync(cancellationToken);
        foreach (var iid in memberships)
            claims.Add(new Claim("institution", iid.ToString()));
        if (institutionId is Guid active)
            claims.Add(new Claim("active_institution", active.ToString()));

        // CurriculumAdmin claim for existing policy
        if (roles.Contains(nameof(ApplicationRole.CurriculumAdministrator))
            || roles.Contains(nameof(ApplicationRole.SystemAdministrator)))
            claims.Add(new Claim("CurriculumAdmin", "true"));

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        var refreshRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshDays = rememberMe ? _options.RefreshTokenDays : Math.Min(_options.RefreshTokenDays, 1);
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(refreshRaw),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            CreatedByIp = ip,
            UserAgent = Truncate(userAgent, 300),
            InstitutionId = institutionId
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthenticationResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshRaw,
            AccessTokenExpiresAt = expires,
            User = await MapSessionAsync(user, institutionId, cancellationToken),
            Roles = roles.ToList(),
            Permissions = permissions,
            MustChangePassword = user.MustChangePassword
        };
    }

    private async Task<UserSessionDto> MapSessionAsync(ApplicationUser user, Guid? activeInstitutionId, CancellationToken cancellationToken)
    {
        var memberships = await _db.InstitutionMemberships.AsNoTracking()
            .Where(m => m.UserId == user.Id && m.IsActive && !m.IsDeleted)
            .Join(_db.EducationalInstitutions, m => m.InstitutionId, i => i.Id, (m, i) => new InstitutionMembershipDto
            {
                Id = m.Id,
                InstitutionId = m.InstitutionId,
                InstitutionName = i.Name,
                UserId = m.UserId,
                UserDisplayName = user.DisplayName,
                Role = m.Role,
                IsActive = m.IsActive
            }).ToListAsync(cancellationToken);

        var roles = await _users.GetRolesAsync(user);
        var permissions = await _permissions.GetEffectivePermissionsAsync(user.Id, activeInstitutionId, cancellationToken);
        var activeName = memberships.FirstOrDefault(m => m.InstitutionId == activeInstitutionId)?.InstitutionName;
        return new UserSessionDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PreferredTimeZone = user.PreferredTimeZone,
            PreferredLanguage = user.PreferredLanguage,
            ActiveInstitutionId = activeInstitutionId,
            ActiveInstitutionName = activeName,
            Roles = roles.ToList(),
            Permissions = permissions,
            Memberships = memberships
        };
    }

    private async Task<Guid?> ResolveActiveInstitutionAsync(Guid userId, Guid? requested, CancellationToken cancellationToken)
    {
        var memberships = await _db.InstitutionMemberships.AsNoTracking()
            .Where(m => m.UserId == userId && m.IsActive && !m.IsDeleted)
            .Select(m => m.InstitutionId)
            .ToListAsync(cancellationToken);
        if (requested is Guid r && memberships.Contains(r)) return r;
        return memberships.FirstOrDefault();
    }

    private async Task RevokeAllForUserAsync(Guid userId, string? ip, CancellationToken cancellationToken)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var t in tokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = DateTime.UtcNow;
            t.RevokedByIp = ip;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser?> FindUserAsync(string userNameOrEmail)
    {
        if (string.IsNullOrWhiteSpace(userNameOrEmail)) return null;
        var value = userNameOrEmail.Trim();
        return value.Contains('@')
            ? await _users.FindByEmailAsync(value)
            : await _users.FindByNameAsync(value);
    }

    private static void ValidateLoginInput(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserNameOrEmail) || request.UserNameOrEmail.Length > 256)
            throw new AuthException("Las credenciales no son válidas.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length > 200)
            throw new AuthException("Las credenciales no son válidas.");
    }

    private static void EnsurePasswordStrength(string password, ApplicationUser user)
    {
        if (password.Length < 10
            || !password.Any(char.IsUpper)
            || !password.Any(char.IsLower)
            || !password.Any(char.IsDigit)
            || !password.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new AuthException("La contraseña no cumple los requisitos de seguridad.");
        if (string.Equals(password, user.UserName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(password, user.Email, StringComparison.OrdinalIgnoreCase))
            throw new AuthException("La contraseña no puede coincidir con el usuario o correo.");
    }

    private async Task EnsureNotInHistoryAsync(ApplicationUser user, string newPassword, CancellationToken cancellationToken)
    {
        var recent = await _db.PasswordHistories.AsNoTracking()
            .Where(h => h.UserId == user.Id)
            .OrderByDescending(h => h.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);
        foreach (var h in recent)
        {
            if (_users.PasswordHasher.VerifyHashedPassword(user, h.PasswordHash, newPassword)
                != PasswordVerificationResult.Failed)
                throw new AuthException("No puede reutilizar una de las últimas contraseñas.");
        }
    }

    private async Task AddPasswordHistoryAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        _db.PasswordHistories.Add(new PasswordHistory
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PasswordHash = user.PasswordHash ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value[..max]);
}

public sealed class AuthException : Exception
{
    public AuthException(string message) : base(message) { }
}
