using System.Security.Cryptography;
using System.Text;
using ProfeAsistente.CurriculumImporter.Abstractions;
using ProfeAsistente.CurriculumImporter.Models.Sources;
using NewDownloadedSource = ProfeAsistente.CurriculumImporter.Models.Download.DownloadedSource;
using NewSourceDownloader = ProfeAsistente.CurriculumImporter.Services.Download.ISourceDownloader;
using ProfeAsistente.CurriculumImporter.Services.Download;
using ProfeAsistente.Shared.Dtos;
using Microsoft.Extensions.Logging;

namespace ProfeAsistente.CurriculumImporter.Download;

public class DownloaderOptions
{
    public string[] AllowedDomains { get; set; } = ["www.curriculumnacional.cl", "curriculumnacional.cl", "localhost"];
    public string UserAgent { get; set; } = "ProfeAsistente-CurriculumImporter/1.0 (+local; educational use)";
    public int DelayMsBetweenRequests { get; set; } = 800;
    public string CacheDirectory { get; set; } = Path.Combine("App_Data", "curriculum");
    public long MaxDownloadSizeBytes { get; set; } = 25 * 1024 * 1024;
}

public class HttpSourceDownloader : ProfeAsistente.CurriculumImporter.Abstractions.ISourceDownloader, NewSourceDownloader
{
    private readonly HttpClient _http;
    private readonly DownloaderOptions _options;
    private readonly ILogger<HttpSourceDownloader> _logger;
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public HttpSourceDownloader(HttpClient http, DownloaderOptions options, ILogger<HttpSourceDownloader> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", _options.UserAgent);
    }

    public async Task<DownloadedSource> DownloadAsync(CurriculumSourceConfig source, CancellationToken cancellationToken = default)
    {
        if (string.Equals(source.Formato, "Json", StringComparison.OrdinalIgnoreCase) &&
            (source.Url.StartsWith("file:", StringComparison.OrdinalIgnoreCase) || File.Exists(source.Url)))
        {
            var path = source.Url.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                ? new Uri(source.Url).LocalPath
                : source.Url;
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
            return new DownloadedSource
            {
                UrlOriginal = source.Url,
                RutaArchivoLocal = path,
                HashSha256 = Sha256(bytes),
                Content = bytes,
                ContentType = "application/json",
                FromCache = true
            };
        }

        if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"URL inválida: {source.Url}");

        if (!_options.AllowedDomains.Any(d => uri.Host.Equals(d, StringComparison.OrdinalIgnoreCase) ||
                                              uri.Host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Dominio no permitido: {uri.Host}");

        Directory.CreateDirectory(_options.CacheDirectory);
        var hashName = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source.Url))).ToLowerInvariant();
        var cachePath = Path.Combine(_options.CacheDirectory, hashName + GuessExt(source.Formato));
        var metaPath = cachePath + ".meta.json";

        await Gate.WaitAsync(cancellationToken);
        try
        {
            await Task.Delay(_options.DelayMsBetweenRequests, cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (File.Exists(metaPath))
            {
                try
                {
                    var meta = System.Text.Json.JsonSerializer.Deserialize<CacheMeta>(await File.ReadAllTextAsync(metaPath, cancellationToken));
                    if (!string.IsNullOrWhiteSpace(meta?.ETag))
                        request.Headers.TryAddWithoutValidation("If-None-Match", meta.ETag);
                    if (!string.IsNullOrWhiteSpace(meta?.LastModified))
                        request.Headers.TryAddWithoutValidation("If-Modified-Since", meta.LastModified);
                }
                catch { /* ignore meta */ }
            }

            using var response = await _http.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotModified && File.Exists(cachePath))
            {
                var cached = await File.ReadAllBytesAsync(cachePath, cancellationToken);
                _logger.LogInformation("Usando cache (304) para {Url}", source.Url);
                return new DownloadedSource
                {
                    UrlOriginal = source.Url,
                    RutaArchivoLocal = cachePath,
                    HashSha256 = Sha256(cached),
                    Content = cached,
                    FromCache = true,
                    ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream"
                };
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await File.WriteAllBytesAsync(cachePath, content, cancellationToken);
            var etag = response.Headers.ETag?.Tag;
            var lastMod = response.Content.Headers.LastModified?.ToString("R");
            await File.WriteAllTextAsync(metaPath, System.Text.Json.JsonSerializer.Serialize(new CacheMeta
            {
                ETag = etag,
                LastModified = lastMod,
                HashSha256 = Sha256(content)
            }), cancellationToken);

            return new DownloadedSource
            {
                UrlOriginal = source.Url,
                RutaArchivoLocal = cachePath,
                HashSha256 = Sha256(content),
                ETag = etag,
                LastModified = lastMod,
                Content = content,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream"
            };
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<NewDownloadedSource> DownloadAsync(CurriculumSourceDefinition source, CancellationToken cancellationToken = default)
    {
        SourceConfigurationLoader.Validate(source);
        if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new SourceDownloadException("Solo se permiten descargas HTTPS.");

        var directory = Path.Combine(_options.CacheDirectory, "Downloads");
        Directory.CreateDirectory(directory);
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source.Id))).ToLowerInvariant();
        var extension = Path.GetExtension(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension)) extension = GuessExt(source.Formato);
        var destination = Path.Combine(directory, key + extension);
        var metadataPath = destination + ".meta.json";
        CacheMeta? previous = null;
        if (File.Exists(metadataPath))
        {
            try { previous = System.Text.Json.JsonSerializer.Deserialize<CacheMeta>(await File.ReadAllTextAsync(metadataPath, cancellationToken)); }
            catch (System.Text.Json.JsonException) { }
        }

        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (source.IntervaloSolicitudesMs > 0)
                await Task.Delay(source.IntervaloSolicitudesMs, cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.TryAddWithoutValidation("User-Agent", "ProfeAsistente-CurriculumImporter/1.0");
            if (!string.IsNullOrWhiteSpace(previous?.ETag)) request.Headers.TryAddWithoutValidation("If-None-Match", previous.ETag);
            if (!string.IsNullOrWhiteSpace(previous?.LastModified)) request.Headers.TryAddWithoutValidation("If-Modified-Since", previous.LastModified);

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
            {
                if (!File.Exists(destination)) throw new SourceDownloadException("El servidor devolvió 304 sin una copia local.");
                var info = new FileInfo(destination);
                return new NewDownloadedSource
                {
                    SourceId = source.Id, OriginalUrl = source.Url, LocalFilePath = destination, FileName = info.Name,
                    ContentType = previous?.ContentType ?? "application/octet-stream", SizeBytes = info.Length,
                    Sha256 = previous?.HashSha256 ?? await HashFileAsync(destination, cancellationToken),
                    ETag = previous?.ETag, LastModified = DateTimeOffset.TryParse(previous?.LastModified, out var lm) ? lm : null,
                    DownloadedAt = DateTimeOffset.UtcNow, WasNotModified = true
                };
            }
            if (!response.IsSuccessStatusCode)
                throw new SourceDownloadException($"La descarga devolvió {(int)response.StatusCode} ({response.ReasonPhrase}).");
            if (response.Content.Headers.ContentLength is long contentLength && contentLength > _options.MaxDownloadSizeBytes)
                throw new SourceDownloadException($"La descarga excede el máximo de {_options.MaxDownloadSizeBytes} bytes.");

            var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                long size = 0;
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var output = File.Create(temporary))
                {
                    var buffer = new byte[81920];
                    int read;
                    while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        size += read;
                        if (size > _options.MaxDownloadSizeBytes)
                            throw new SourceDownloadException($"La descarga excede el máximo de {_options.MaxDownloadSizeBytes} bytes.");
                        hash.AppendData(buffer, 0, read);
                        await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    }
                }
                File.Move(temporary, destination, true);
                var sha = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
                var etag = response.Headers.ETag?.Tag;
                var lastModified = response.Content.Headers.LastModified;
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                await File.WriteAllTextAsync(metadataPath, System.Text.Json.JsonSerializer.Serialize(new CacheMeta
                {
                    ETag = etag, LastModified = lastModified?.ToString("R"), HashSha256 = sha, ContentType = contentType
                }), cancellationToken);
                return new NewDownloadedSource
                {
                    SourceId = source.Id, OriginalUrl = source.Url, LocalFilePath = destination, FileName = Path.GetFileName(destination),
                    ContentType = contentType, SizeBytes = size, Sha256 = sha, ETag = etag, LastModified = lastModified,
                    DownloadedAt = DateTimeOffset.UtcNow
                };
            }
            catch
            {
                if (File.Exists(temporary)) File.Delete(temporary);
                throw;
            }
        }
        catch (SourceDownloadException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            throw new SourceDownloadException($"No se pudo descargar {source.Id}.", ex);
        }
        finally { Gate.Release(); }
    }

    private static string GuessExt(string formato) => formato.ToLowerInvariant() switch
    {
        "pdf" => ".pdf",
        "html" => ".html",
        "json" => ".json",
        _ => ".bin"
    };

    private static string Sha256(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    private static async Task<string> HashFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
    }

    private class CacheMeta
    {
        public string? ETag { get; set; }
        public string? LastModified { get; set; }
        public string? HashSha256 { get; set; }
        public string? ContentType { get; set; }
    }
}
