using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProfeAsistente.Shared.Dtos;

namespace ProfeAsistente.Maui.Services.Auth;

public interface IAuthenticationService
{
    Task<AuthenticationResponse?> LoginAsync(string userNameOrEmail, string password, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<UserSessionDto?> GetMeAsync(CancellationToken ct = default);
    Task<bool> EnsureAuthenticatedAsync(CancellationToken ct = default);
    Task SetActiveInstitutionAsync(Guid institutionId);
    string? AccessToken { get; }
    Guid? ActiveInstitutionId { get; }
    UserSessionDto? CurrentUser { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    event EventHandler? SessionExpired;
}

public sealed class AuthenticationService : IAuthenticationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly ITokenStorageService _storage;
    private string? _access;
    private string? _refresh;
    private Guid? _activeInstitutionId;

    public AuthenticationService(HttpClient http, ITokenStorageService storage)
    {
        _http = http;
        _storage = storage;
    }

    public string? AccessToken => _access;
    public Guid? ActiveInstitutionId => _activeInstitutionId ?? CurrentUser?.ActiveInstitutionId;
    public UserSessionDto? CurrentUser { get; private set; }
    public IReadOnlyList<string> Roles { get; private set; } = [];
    public IReadOnlyList<string> Permissions { get; private set; } = [];
    public event EventHandler? SessionExpired;

    public async Task<AuthenticationResponse?> LoginAsync(string userNameOrEmail, string password, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            UserNameOrEmail = userNameOrEmail,
            Password = password
        }, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        var body = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions, ct);
        if (body is null) return null;
        await PersistAsync(body);
        return body;
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(_refresh))
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
                req.Content = JsonContent.Create(new RefreshTokenRequest { RefreshToken = _refresh }, options: JsonOptions);
                if (!string.IsNullOrWhiteSpace(_access))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _access);
                await _http.SendAsync(req, ct);
            }
            catch { /* ignore */ }
        }

        _access = null;
        _refresh = null;
        _activeInstitutionId = null;
        CurrentUser = null;
        Roles = [];
        Permissions = [];
        await _storage.ClearAsync();
    }

    public Task SetActiveInstitutionAsync(Guid institutionId)
    {
        if (CurrentUser?.Memberships.Any(m => m.InstitutionId == institutionId && m.IsActive) != true)
            throw new InvalidOperationException("Membresía de establecimiento no válida.");
        _activeInstitutionId = institutionId;
        Preferences.Default.Set("appedu.active_institution", institutionId.ToString());
        return Task.CompletedTask;
    }

    public async Task<UserSessionDto?> GetMeAsync(CancellationToken ct = default)
    {
        if (!await EnsureAuthenticatedAsync(ct)) return null;
        using var req = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _access);
        using var response = await _http.SendAsync(req, ct);
        if (!response.IsSuccessStatusCode) return null;
        CurrentUser = await response.Content.ReadFromJsonAsync<UserSessionDto>(JsonOptions, ct);
        if (CurrentUser is not null)
        {
            if (CurrentUser.Roles.Count > 0)
                Roles = CurrentUser.Roles;
            if (CurrentUser.Permissions.Count > 0)
                Permissions = CurrentUser.Permissions;
        }
        return CurrentUser;
    }

    public async Task<bool> EnsureAuthenticatedAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_access) || string.IsNullOrWhiteSpace(_refresh))
        {
            var loaded = await _storage.LoadAsync();
            _access = loaded.AccessToken;
            _refresh = loaded.RefreshToken;
        }

        if (string.IsNullOrWhiteSpace(_access)) return false;
        return true;
    }

    public async Task<bool> TryRefreshAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_refresh))
        {
            var loaded = await _storage.LoadAsync();
            _refresh = loaded.RefreshToken;
        }

        if (string.IsNullOrWhiteSpace(_refresh)) return false;
        using var response = await _http.PostAsJsonAsync("api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = _refresh }, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
        {
            await LogoutAsync(ct);
            SessionExpired?.Invoke(this, EventArgs.Empty);
            return false;
        }

        var body = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions, ct);
        if (body is null) return false;
        await PersistAsync(body);
        return true;
    }

    private async Task PersistAsync(AuthenticationResponse body)
    {
        _access = body.AccessToken;
        _refresh = body.RefreshToken;
        CurrentUser = body.User;
        Roles = body.Roles;
        Permissions = body.Permissions;
        _activeInstitutionId = body.User.ActiveInstitutionId
            ?? (Guid.TryParse(Preferences.Default.Get("appedu.active_institution", string.Empty), out var saved)
                ? saved
                : null);
        if (_activeInstitutionId is Guid aid
            && body.User.Memberships.All(m => m.InstitutionId != aid || !m.IsActive))
            _activeInstitutionId = body.User.Memberships.FirstOrDefault()?.InstitutionId;
        await _storage.SaveAsync(body.AccessToken, body.RefreshToken, body.AccessTokenExpiresAt);
    }
}

public sealed class AuthenticatedApiClientHandler : DelegatingHandler
{
    private readonly IAuthenticationService _auth;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private bool _refreshing;

    public AuthenticatedApiClientHandler(IAuthenticationService auth)
    {
        _auth = auth;
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var isAuthAnonymous = path.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase)
                              || path.Contains("/api/auth/refresh", StringComparison.OrdinalIgnoreCase)
                              || path.Contains("/api/auth/forgot-password", StringComparison.OrdinalIgnoreCase)
                              || path.Contains("/api/auth/reset-password", StringComparison.OrdinalIgnoreCase);

        if (!isAuthAnonymous)
        {
            await _auth.EnsureAuthenticatedAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(_auth.AccessToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
            if (_auth.ActiveInstitutionId is Guid iid)
            {
                request.Headers.Remove("X-Institution-Id");
                request.Headers.TryAddWithoutValidation("X-Institution-Id", iid.ToString());
            }
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized || isAuthAnonymous)
            return response;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (_refreshing) return response;
            _refreshing = true;
            response.Dispose();
            if (_auth is AuthenticationService concrete)
            {
                if (!await concrete.TryRefreshAsync(cancellationToken))
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            var retry = await CloneAsync(request);
            if (!string.IsNullOrWhiteSpace(_auth.AccessToken))
                retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
            if (_auth.ActiveInstitutionId is Guid iid2)
            {
                retry.Headers.Remove("X-Institution-Id");
                retry.Headers.TryAddWithoutValidation("X-Institution-Id", iid2.ToString());
            }
            return await base.SendAsync(retry, cancellationToken);
        }
        finally
        {
            _refreshing = false;
            _refreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (request.Content is not null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);
            if (request.Content.Headers is not null)
            {
                foreach (var h in request.Content.Headers)
                    clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }
        }

        foreach (var h in request.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        return clone;
    }
}
