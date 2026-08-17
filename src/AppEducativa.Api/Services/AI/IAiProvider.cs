namespace AppEducativa.Api.Services.AI;

/// <summary>
/// Proveedor de IA abstraíble. Hoy: Gemini. Mañana: otro modelo sin reescribir servicios de dominio.
/// </summary>
public interface IAiProvider
{
    string ProviderName { get; }

    Task<AiGenerationResult> GenerateJsonAsync(
        string systemInstruction,
        string userPrompt,
        string? jsonSchema,
        CancellationToken cancellationToken);
}

public sealed class AiGenerationResult
{
    public required string Text { get; init; }
    public int? InputTokenCount { get; init; }
    public int? OutputTokenCount { get; init; }
    public long DurationMilliseconds { get; init; }
    public string Model { get; init; } = string.Empty;
}
