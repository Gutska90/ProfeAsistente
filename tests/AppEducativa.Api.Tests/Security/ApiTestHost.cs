using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppEducativa.Shared.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace AppEducativa.Api.Tests.Security;

public sealed class ApiTestHost : IAsyncDisposable
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplication _app;
    private readonly string _dbPath;

    public HttpClient Client { get; }

    private ApiTestHost(WebApplication app, HttpClient client, string dbPath)
    {
        _app = app;
        Client = client;
        _dbPath = dbPath;
    }

    public static async Task<ApiTestHost> StartAsync()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"appedu-sec-{Guid.NewGuid():N}.db");
        Environment.SetEnvironmentVariable("APPEDUCATIVA_JWT_KEY", "test-signing-key-32chars-minimum!!");
        Environment.SetEnvironmentVariable("APPEDUCATIVA_ADMIN_USERNAME", "admin");
        Environment.SetEnvironmentVariable("APPEDUCATIVA_ADMIN_EMAIL", "admin@test.local");
        Environment.SetEnvironmentVariable("APPEDUCATIVA_ADMIN_PASSWORD", "Admin!Pass123");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        var contentRoot = FindApiContentRoot();
        var app = ApiHostBuilder.Build(
            args: [$"ConnectionStrings:DefaultConnection=Data Source={dbPath}"],
            urls: "http://127.0.0.1:0",
            contentRoot: contentRoot);
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()
            ?.Addresses.FirstOrDefault()
            ?? app.Urls.FirstOrDefault()
            ?? throw new InvalidOperationException("No se obtuvo la URL del servidor de prueba.");

        var client = new HttpClient { BaseAddress = new Uri(address.TrimEnd('/') + "/") };
        using (var health = await client.GetAsync("api/health"))
        {
            if (!health.IsSuccessStatusCode)
                throw new InvalidOperationException($"Health check falló: {(int)health.StatusCode} {address}");
        }

        return new ApiTestHost(app, client, dbPath);
    }

    public async Task<AuthenticationResponse> LoginAsync(string user, string password, Guid? institutionId = null)
    {
        using var response = await Client.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            UserNameOrEmail = user,
            Password = password,
            InstitutionId = institutionId
        }, Json);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Login {(int)response.StatusCode}: {body}");
        }
        return (await response.Content.ReadFromJsonAsync<AuthenticationResponse>(Json))!;
    }

    public HttpRequestMessage Auth(HttpMethod method, string url, string accessToken, Guid? institutionId = null, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (institutionId is Guid iid)
            req.Headers.TryAddWithoutValidation("X-Institution-Id", iid.ToString());
        if (body is not null)
            req.Content = JsonContent.Create(body, options: Json);
        return req;
    }

    private static string FindApiContentRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "AppEducativa.Api");
            if (File.Exists(Path.Combine(candidate, "AppEducativa.Api.csproj")))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("No se encontró el content root de AppEducativa.Api.");
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        try { File.Delete(_dbPath); } catch { /* ignore */ }
    }
}
