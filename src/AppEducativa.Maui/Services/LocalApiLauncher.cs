using System.Diagnostics;
using AppEducativa.Maui.Configuration;
using Microsoft.Extensions.Logging;

namespace AppEducativa.Maui.Services;

/// <summary>
/// Asegura que la API local esté disponible. Si no responde, la inicia con <c>dotnet run</c>.
/// </summary>
public sealed class LocalApiLauncher : IAsyncDisposable
{
    private readonly ApiSettings _settings;
    private Process? _process;
    private readonly ILogger<LocalApiLauncher>? _logger;

    public LocalApiLauncher(ApiSettings settings, ILogger<LocalApiLauncher>? logger = null)
    {
        _settings = settings;
        _logger = logger;
    }

    public string BaseUrl => _settings.BaseUrl.TrimEnd('/') + "/";

    public async Task EnsureRunningAsync(CancellationToken ct = default)
    {
        if (await IsHealthyAsync(ct))
            return;

        var apiProject = FindApiProjectPath();
        if (apiProject is null)
            throw new InvalidOperationException(
                "No se encontró AppEducativa.Api. Inicia la API: cd src/AppEducativa.Api && dotnet run");

        _logger?.LogInformation("Iniciando API local desde {Path}", apiProject);

        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveDotnetPath(),
            Arguments = $"run --project \"{apiProject}\" --urls {_settings.BaseUrl.TrimEnd('/')}",
            WorkingDirectory = Path.GetDirectoryName(apiProject)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dotnetHome = Path.Combine(home, ".dotnet");
        var path = startInfo.Environment.TryGetValue("PATH", out var existing) ? existing : Environment.GetEnvironmentVariable("PATH");
        startInfo.Environment["PATH"] = Directory.Exists(dotnetHome) ? $"{dotnetHome}:{path}" : path ?? string.Empty;
        startInfo.Environment["DOTNET_ROOT"] = Directory.Exists(dotnetHome) ? dotnetHome : (Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? string.Empty);

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("No se pudo iniciar el proceso de la API.");

        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            if (await IsHealthyAsync(ct))
                return;
            await Task.Delay(500, ct);
        }

        throw new TimeoutException($"La API local no respondió a tiempo en {_settings.BaseUrl}");
    }

    private async Task<bool> IsHealthyAsync(CancellationToken ct)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            using var response = await http.GetAsync(BaseUrl + "health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindApiProjectPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12 && dir is not null; i++, dir = dir.Parent)
        {
            var candidates = new[]
            {
                Path.Combine(dir.FullName, "src", "AppEducativa.Api", "AppEducativa.Api.csproj"),
                Path.Combine(dir.FullName, "AppEducativa.Api", "AppEducativa.Api.csproj"),
                Path.Combine(dir.FullName, "AppEducativa.Api.csproj")
            };
            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }

    private static string ResolveDotnetPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (var candidate in new[]
                 {
                     Path.Combine(home, ".dotnet", "dotnet"),
                     "/usr/local/share/dotnet/dotnet",
                     "/usr/local/bin/dotnet"
                 })
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return "dotnet";
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
            catch
            {
                // ignore
            }
        }

        _process?.Dispose();
    }
}
