namespace AppEducativa.Api.Services.AI.Gemini;

public interface IGeminiClient
{
    Task<GeminiGenerationResult> GenerateJsonAsync(
        string systemInstruction,
        string userPrompt,
        string? jsonSchema,
        CancellationToken cancellationToken);
}

public sealed class GeminiGenerationResult
{
    public required string Text { get; init; }
    public int? InputTokenCount { get; init; }
    public int? OutputTokenCount { get; init; }
    public long DurationMilliseconds { get; init; }
    public string Model { get; init; } = string.Empty;
}
