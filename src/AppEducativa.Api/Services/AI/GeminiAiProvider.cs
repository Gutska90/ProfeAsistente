using AppEducativa.Api.Services.AI.Gemini;

namespace AppEducativa.Api.Services.AI;

/// <summary>Adaptador: el cliente Gemini concreto detrás de <see cref="IAiProvider"/>.</summary>
public sealed class GeminiAiProvider : IAiProvider
{
    private readonly IGeminiClient _gemini;

    public GeminiAiProvider(IGeminiClient gemini) => _gemini = gemini;

    public string ProviderName => "Gemini";

    public async Task<AiGenerationResult> GenerateJsonAsync(
        string systemInstruction,
        string userPrompt,
        string? jsonSchema,
        CancellationToken cancellationToken)
    {
        var result = await _gemini.GenerateJsonAsync(systemInstruction, userPrompt, jsonSchema, cancellationToken);
        return new AiGenerationResult
        {
            Text = result.Text,
            InputTokenCount = result.InputTokenCount,
            OutputTokenCount = result.OutputTokenCount,
            DurationMilliseconds = result.DurationMilliseconds,
            Model = result.Model
        };
    }
}
