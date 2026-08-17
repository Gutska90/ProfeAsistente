using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ProfeAsistente.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ProfeAsistente.Api.Services.AI.Gemini;

public sealed class GeminiClient : IGeminiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public GeminiClient(
        IHttpClientFactory httpClientFactory,
        IOptions<GeminiOptions> options,
        ILogger<GeminiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GeminiGenerationResult> GenerateJsonAsync(
        string systemInstruction,
        string userPrompt,
        string? jsonSchema,
        CancellationToken cancellationToken)
    {
        if (!_options.EnableGeneration)
            throw new GeminiConfigurationException("La generación con Gemini está deshabilitada.");

        var apiKey = Environment.GetEnvironmentVariable(_options.ApiKeyEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new GeminiConfigurationException(
                $"No se encontró la variable de entorno {_options.ApiKeyEnvironmentVariable}. Configure la API key para generar estructuras.");

        if (string.IsNullOrWhiteSpace(_options.Model))
            throw new GeminiConfigurationException("Gemini:Model no está configurado.");

        var baseUri = new Uri(_options.BaseUrl, UriKind.Absolute);
        if (!string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new GeminiConfigurationException("Gemini:BaseUrl debe usar HTTPS.");

        var url = $"{_options.BaseUrl.TrimEnd('/')}/v1beta/models/{_options.Model}:generateContent";
        object? responseSchema = null;
        if (!string.IsNullOrWhiteSpace(jsonSchema))
        {
            try { responseSchema = JsonSerializer.Deserialize<JsonElement>(jsonSchema); }
            catch (JsonException) { /* schema opcional */ }
        }

        var payload = new
        {
            systemInstruction = new { parts = new[] { new { text = systemInstruction } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
            generationConfig = new
            {
                temperature = _options.Temperature,
                maxOutputTokens = _options.MaxOutputTokens,
                responseMimeType = "application/json",
                responseSchema
            }
        };

        var client = _httpClientFactory.CreateClient(nameof(GeminiClient));
        client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        Exception? last = null;
        for (var attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                _logger.LogInformation(
                    "GeminiRequestStarted Model={Model} Attempt={Attempt}",
                    _options.Model, attempt + 1);

                using var response = await client.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                sw.Stop();

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    throw new GeminiRateLimitException("Se alcanzó el límite de cuota del proveedor de IA.");

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    throw new GeminiApiException("Autenticación con el proveedor de IA fallida.", "AiAuthenticationFailed", (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GeminiRequestFailed Status={Status} Attempt={Attempt}", (int)response.StatusCode, attempt + 1);
                    if (attempt < _options.MaxRetries && (int)response.StatusCode >= 500)
                    {
                        await Task.Delay(200 * (attempt + 1), cancellationToken);
                        continue;
                    }

                    throw new GeminiApiException(
                        "No fue posible contactar el servicio de generación.",
                        "AiProviderUnavailable",
                        (int)response.StatusCode);
                }

                var text = ExtractText(body);
                if (string.IsNullOrWhiteSpace(text))
                    throw new GeminiApiException("El proveedor devolvió una respuesta vacía.", "AiEmptyResponse", 502);

                var (inputTokens, outputTokens) = ExtractUsage(body);
                _logger.LogInformation(
                    "GeminiRequestCompleted Model={Model} DurationMs={Duration} InputTokens={In} OutputTokens={Out}",
                    _options.Model, sw.ElapsedMilliseconds, inputTokens, outputTokens);

                return new GeminiGenerationResult
                {
                    Text = text,
                    InputTokenCount = inputTokens,
                    OutputTokenCount = outputTokens,
                    DurationMilliseconds = sw.ElapsedMilliseconds,
                    Model = _options.Model
                };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new GeminiApiException("La solicitud al proveedor de IA excedió el tiempo de espera.", "AiTimeout", 504);
            }
            catch (GeminiApiException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < _options.MaxRetries)
            {
                last = ex;
                _logger.LogWarning(ex, "GeminiRequestFailed Attempt={Attempt}", attempt + 1);
                await Task.Delay(200 * (attempt + 1), cancellationToken);
            }
        }

        throw new GeminiApiException(
            "No fue posible contactar el servicio de generación.",
            "AiProviderUnavailable",
            503,
            last);
    }

    private static string ExtractText(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return string.Empty;
        var content = candidates[0].GetProperty("content");
        if (!content.TryGetProperty("parts", out var parts) || parts.GetArrayLength() == 0)
            return string.Empty;
        return parts[0].TryGetProperty("text", out var text) ? text.GetString() ?? "" : "";
    }

    private static (int? Input, int? Output) ExtractUsage(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            if (!doc.RootElement.TryGetProperty("usageMetadata", out var usage))
                return (null, null);
            int? input = usage.TryGetProperty("promptTokenCount", out var p) ? p.GetInt32() : null;
            int? output = usage.TryGetProperty("candidatesTokenCount", out var c) ? c.GetInt32() : null;
            return (input, output);
        }
        catch
        {
            return (null, null);
        }
    }
}
