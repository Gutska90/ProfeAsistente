using Microsoft.AspNetCore.Identity;

namespace ProfeAsistente.Api.Models.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string PreferredTimeZone { get; set; } = "America/Santiago";
    public string PreferredLanguage { get; set; } = "es-CL";
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletionReason { get; set; }
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();
}

public class ApplicationRoleEntity : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public ApplicationRoleEntity() { }

    public ApplicationRoleEntity(string roleName) : base(roleName) { }
}

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }
    public string? UserAgent { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public Guid? InstitutionId { get; set; }

    public ApplicationUser? User { get; set; }
}

public class PasswordHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
