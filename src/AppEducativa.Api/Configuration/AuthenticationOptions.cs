namespace AppEducativa.Api.Configuration;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string Issuer { get; set; } = "AppEducativa.Api";
    public string Audience { get; set; } = "AppEducativa.Maui";
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 7;
    public bool RequireConfirmedEmail { get; set; }
    public int MaximumFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    /// <summary>Solo Development: permite iniciar sin APPEDUCATIVA_JWT_KEY usando una clave efímera.</summary>
    public bool AllowDevelopmentSigningKey { get; set; } = true;
}

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";
    public int LoginRequestsPerMinute { get; set; } = 10;
    public int PasswordResetRequestsPerHour { get; set; } = 5;
    public int RefreshRequestsPerMinute { get; set; } = 30;
}

public sealed class DevelopmentAuthenticationOptions
{
    public const string SectionName = "DevelopmentAuthentication";
    public bool Enabled { get; set; }
    public string DefaultUserName { get; set; } = string.Empty;
}
