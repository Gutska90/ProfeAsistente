using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ProfeAsistente.Shared.Dtos;

namespace ProfeAsistente.Api.Tests.Security;

[Collection("Security")]
public class AuthenticationTests : IAsyncLifetime
{
    private ApiTestHost _host = null!;

    public async Task InitializeAsync() => _host = await ApiTestHost.StartAsync();

    public Task DisposeAsync() => _host.DisposeAsync().AsTask();

    [Fact]
    public async Task Login_Valido_DevuelveTokensYClaims()
    {
        var auth = await _host.LoginAsync("admin", "Admin!Pass123");
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.Contains("SystemAdministrator", auth.Roles);
        Assert.DoesNotContain("PasswordHash", JsonSerializer.Serialize(auth));

        var payload = DecodeJwtPayload(auth.AccessToken);
        Assert.Contains("ProfeAsistente.Api", payload);
        Assert.Contains("ProfeAsistente.Maui", payload);
    }

    [Fact]
    public async Task Login_Invalido_NoRevelaUsuario()
    {
        using var response = await _host.Client.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            UserNameOrEmail = "no-existe",
            Password = "Wrong!Pass123"
        }, ApiTestHost.Json);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Las credenciales no son válidas", body);
        Assert.DoesNotContain("no existe", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_PasswordIncorrecta_MismoMensaje()
    {
        using var response = await _host.Client.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            UserNameOrEmail = "admin",
            Password = "Wrong!Pass999"
        }, ApiTestHost.Json);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Las credenciales no son válidas", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Refresh_Valido_RotaToken()
    {
        var auth = await _host.LoginAsync("admin", "Admin!Pass123");
        using var response = await _host.Client.PostAsJsonAsync("api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = auth.RefreshToken }, ApiTestHost.Json);
        response.EnsureSuccessStatusCode();
        var next = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(ApiTestHost.Json);
        Assert.NotNull(next);
        Assert.NotEqual(auth.RefreshToken, next!.RefreshToken);
    }

    [Fact]
    public async Task Refresh_Reutilizado_RevocaSesiones()
    {
        var auth = await _host.LoginAsync("admin", "Admin!Pass123");
        using var first = await _host.Client.PostAsJsonAsync("api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = auth.RefreshToken }, ApiTestHost.Json);
        first.EnsureSuccessStatusCode();
        using var reuse = await _host.Client.PostAsJsonAsync("api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = auth.RefreshToken }, ApiTestHost.Json);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Logout_InvalidaRefresh()
    {
        var auth = await _host.LoginAsync("admin", "Admin!Pass123");
        using var logoutReq = _host.Auth(HttpMethod.Post, "api/auth/logout", auth.AccessToken,
            body: new RefreshTokenRequest { RefreshToken = auth.RefreshToken });
        using var logout = await _host.Client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        using var refresh = await _host.Client.PostAsJsonAsync("api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = auth.RefreshToken }, ApiTestHost.Json);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_RevocaRefresh()
    {
        var admin = await _host.LoginAsync("admin", "Admin!Pass123");
        using var createReq = _host.Auth(HttpMethod.Post, "api/admin/users", admin.AccessToken, body: new CreateUserRequest
        {
            UserName = "pwduser",
            Email = "pwduser@test.local",
            Password = "Change!Pass123",
            FirstName = "Pwd",
            LastName = "User",
            Roles = ["Teacher"],
            MustChangePassword = false
        });
        using var create = await _host.Client.SendAsync(createReq);
        create.EnsureSuccessStatusCode();

        var auth = await _host.LoginAsync("pwduser", "Change!Pass123");
        using var changeReq = _host.Auth(HttpMethod.Post, "api/auth/change-password", auth.AccessToken,
            body: new ChangePasswordRequest
            {
                CurrentPassword = "Change!Pass123",
                NewPassword = "Change!Pass456"
            });
        using var change = await _host.Client.SendAsync(changeReq);
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        using var refresh = await _host.Client.PostAsJsonAsync("api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = auth.RefreshToken }, ApiTestHost.Json);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_MensajeGenerico()
    {
        using var response = await _host.Client.PostAsJsonAsync("api/auth/forgot-password",
            new ForgotPasswordRequest { UserNameOrEmail = "alguien@noexiste.local" }, ApiTestHost.Json);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>(ApiTestHost.Json);
        Assert.Contains("Si la cuenta existe", body!.Message);
    }

    [Fact]
    public async Task Me_RequiereAuth()
    {
        using var anon = await _host.Client.GetAsync("api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);

        var auth = await _host.LoginAsync("admin", "Admin!Pass123");
        using var req = _host.Auth(HttpMethod.Get, "api/auth/me", auth.AccessToken);
        using var me = await _host.Client.SendAsync(req);
        me.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task TokenManipulado_Rechazado()
    {
        var auth = await _host.LoginAsync("admin", "Admin!Pass123");
        var bad = auth.AccessToken[..^4] + "xxxx";
        using var req = _host.Auth(HttpMethod.Get, "api/auth/me", bad);
        using var response = await _host.Client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string DecodeJwtPayload(string jwt)
    {
        var parts = jwt.Split('.');
        Assert.True(parts.Length >= 2);
        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }
        return Encoding.UTF8.GetString(Convert.FromBase64String(payload));
    }
}
