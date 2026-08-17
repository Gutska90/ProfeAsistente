namespace ProfeAsistente.Maui.Services.Auth;

public interface ITokenStorageService
{
    Task SaveAsync(string accessToken, string refreshToken, DateTimeOffset accessExpiresAt);
    Task<(string? AccessToken, string? RefreshToken, DateTimeOffset? ExpiresAt)> LoadAsync();
    Task ClearAsync();
}

/// <summary>
/// Guarda tokens en SecureStorage. Si el keychain falla (frecuente en Mac Catalyst sin entitlements),
/// mantiene los tokens solo en memoria para la sesión actual.
/// </summary>
public sealed class SecureTokenStorageService : ITokenStorageService
{
    private const string AccessKey = "appedu.access";
    private const string RefreshKey = "appedu.refresh";
    private const string ExpiresKey = "appedu.access.expires";

    private string? _memoryAccess;
    private string? _memoryRefresh;
    private DateTimeOffset? _memoryExpires;

    public async Task SaveAsync(string accessToken, string refreshToken, DateTimeOffset accessExpiresAt)
    {
        _memoryAccess = accessToken;
        _memoryRefresh = refreshToken;
        _memoryExpires = accessExpiresAt;
        try
        {
            await SecureStorage.Default.SetAsync(AccessKey, accessToken);
            await SecureStorage.Default.SetAsync(RefreshKey, refreshToken);
            await SecureStorage.Default.SetAsync(ExpiresKey, accessExpiresAt.UtcDateTime.ToString("O"));
        }
        catch
        {
            // Sesión en memoria; no bloquear el login en desarrollo local.
        }
    }

    public async Task<(string? AccessToken, string? RefreshToken, DateTimeOffset? ExpiresAt)> LoadAsync()
    {
        try
        {
            var access = await SecureStorage.Default.GetAsync(AccessKey);
            var refresh = await SecureStorage.Default.GetAsync(RefreshKey);
            var expiresRaw = await SecureStorage.Default.GetAsync(ExpiresKey);
            DateTimeOffset? expires = DateTimeOffset.TryParse(expiresRaw, out var e) ? e : null;
            if (!string.IsNullOrWhiteSpace(access))
            {
                _memoryAccess = access;
                _memoryRefresh = refresh;
                _memoryExpires = expires;
                return (access, refresh, expires);
            }
        }
        catch
        {
            // fallback memoria
        }

        return (_memoryAccess, _memoryRefresh, _memoryExpires);
    }

    public Task ClearAsync()
    {
        _memoryAccess = null;
        _memoryRefresh = null;
        _memoryExpires = null;
        try
        {
            SecureStorage.Default.Remove(AccessKey);
            SecureStorage.Default.Remove(RefreshKey);
            SecureStorage.Default.Remove(ExpiresKey);
        }
        catch
        {
            // ignore
        }

        return Task.CompletedTask;
    }
}
