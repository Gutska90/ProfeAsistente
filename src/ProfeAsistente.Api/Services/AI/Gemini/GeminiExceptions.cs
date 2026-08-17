namespace ProfeAsistente.Api.Services.AI.Gemini;

public class GeminiApiException : Exception
{
    public int? StatusCode { get; }
    public string ErrorCode { get; }

    public GeminiApiException(string message, string errorCode = "AiProviderError", int? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

public sealed class GeminiConfigurationException : GeminiApiException
{
    public GeminiConfigurationException(string message)
        : base(message, "AiConfigurationMissing", 503) { }
}

public sealed class GeminiRateLimitException : GeminiApiException
{
    public GeminiRateLimitException(string message)
        : base(message, "AiRateLimited", 429) { }
}
