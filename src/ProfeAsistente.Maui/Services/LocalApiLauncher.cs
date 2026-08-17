using System.Diagnostics;
using ProfeAsistente.Maui.Configuration;
using Microsoft.Extensions.Logging;

namespace ProfeAsistente.Maui.Services;

/// <summary>
/// Asegura que la API local esté disponible.
/// Orden: health check → proceso publicado (piloto) → <c>dotnet run</c> del proyecto fuente.
/// En distribución con App Sandbox, preferir arrancar la API con <c>start-piloto.sh</c>.
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

        if (TryStartPublishedApi(out var publishError))
        {
            if (await WaitHealthyAsync(ct))
                return;
            _logger?.LogWarning("API publicada no respondió a tiempo: {Error}", publishError);
        }

        var apiProject = FindApiProjectPath();
        if (apiProject is null)
            throw new InvalidOperationException(
                "No se encontró la API. Inicie el piloto con scripts/start-piloto.sh " +
                "o: cd src/ProfeAsistente.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://127.0.0.1:5180");

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
        ApplyDotnetEnvironment(startInfo);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("No se pudo iniciar el proceso de la API.");

        if (await WaitHealthyAsync(ct))
            return;

        throw new TimeoutException($"La API local no respondió a tiempo en {_settings.BaseUrl}");
    }

    private bool TryStartPublishedApi(out string? error)
    {
        error = null;
        var published = FindPublishedApi();
        if (published is null)
            return false;

        try
        {
            _logger?.LogInformation("Iniciando API publicada desde {Path}", published.Value.Entry);
            var startInfo = published.Value.IsDll
                ? new ProcessStartInfo
                {
                    FileName = ResolveDotnetPath(),
                    Arguments = $"\"{published.Value.Entry}\" --urls {_settings.BaseUrl.TrimEnd('/')}",
                    WorkingDirectory = published.Value.Directory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
                : new ProcessStartInfo
                {
                    FileName = published.Value.Entry,
                    Arguments = $"--urls {_settings.BaseUrl.TrimEnd('/')}",
                    WorkingDirectory = published.Value.Directory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

            ApplyDotnetEnvironment(startInfo);
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] =
                startInfo.Environment.TryGetValue("ASPNETCORE_ENVIRONMENT", out var env) && !string.IsNullOrWhiteSpace(env)
                    ? env
                    : "Production";

            _process = Process.Start(startInfo);
            if (_process is null)
            {
                error = "Process.Start devolvió null";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private async Task<bool> WaitHealthyAsync(CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            if (await IsHealthyAsync(ct))
                return true;
            await Task.Delay(500, ct);
        }

        return false;
    }

    private async Task<bool> IsHealthyAsync(CancellationToken ct)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            foreach (var path in new[] { "health/live", "health", "api/health" })
            {
                using var response = await http.GetAsync(BaseUrl + path, ct);
                if (response.IsSuccessStatusCode)
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static (string Directory, string Entry, bool IsDll)? FindPublishedApi()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12 && dir is not null; i++, dir = dir.Parent)
        {
            foreach (var apiDir in new[]
                     {
                         Path.Combine(dir.FullName, "api"),
                         Path.Combine(dir.FullName, "ProfeAsistente.Api"),
                         Path.Combine(dir.FullName, "artifacts", "piloto-mac", "api")
                     })
            {
                var dll = Path.Combine(apiDir, "ProfeAsistente.Api.dll");
                if (File.Exists(dll))
                    return (apiDir, dll, true);

                var unix = Path.Combine(apiDir, "ProfeAsistente.Api");
                if (File.Exists(unix))
                    return (apiDir, unix, false);
            }
        }

        return null;
    }

    private static string? FindApiProjectPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12 && dir is not null; i++, dir = dir.Parent)
        {
            var candidates = new[]
            {
                Path.Combine(dir.FullName, "src", "ProfeAsistente.Api", "ProfeAsistente.Api.csproj"),
                Path.Combine(dir.FullName, "ProfeAsistente.Api", "ProfeAsistente.Api.csproj"),
                Path.Combine(dir.FullName, "ProfeAsistente.Api.csproj")
            };
            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }

    private static void ApplyDotnetEnvironment(ProcessStartInfo startInfo)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dotnetHome = Path.Combine(home, ".dotnet");
        var path = startInfo.Environment.TryGetValue("PATH", out var existing)
            ? existing
            : Environment.GetEnvironmentVariable("PATH");
        startInfo.Environment["PATH"] = Directory.Exists(dotnetHome) ? $"{dotnetHome}:{path}" : path ?? string.Empty;
        startInfo.Environment["DOTNET_ROOT"] = Directory.Exists(dotnetHome)
            ? dotnetHome
            : (Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? string.Empty);
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
