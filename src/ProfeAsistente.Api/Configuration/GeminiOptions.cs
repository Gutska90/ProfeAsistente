using System.ComponentModel.DataAnnotations;

namespace ProfeAsistente.Api.Configuration;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    [Required]
    public string ApiKeyEnvironmentVariable { get; set; } = "GEMINI_API_KEY";

    [Required]
    public string Model { get; set; } = "gemini-2.5-flash";

    [Required]
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 60;

    [Range(0, 5)]
    public int MaxRetries { get; set; } = 2;

    [Range(0, 1)]
    public double Temperature { get; set; } = 0.3;

    [Range(256, 32000)]
    public int MaxOutputTokens { get; set; } = 5000;

    public bool EnableGeneration { get; set; } = true;

    public bool PersistRequestPayloads { get; set; } = true;

    public string PromptVersion { get; set; } = "class-structure-v1";
}

public sealed class AiUsageOptions
{
    public const string SectionName = "AiUsage";

    public int MaximumGenerationsPerClassPerDay { get; set; } = 10;

    public int MaximumConcurrentGenerations { get; set; } = 2;

    public int MaximumDocumentGenerationsPerClassPerDay { get; set; } = 10;

    public int MaximumItemRegenerationsPerDocumentPerDay { get; set; } = 30;

    /// <summary>Precios estimados USD por 1M tokens (configurables; no son facturación real).</summary>
    public Dictionary<string, AiModelPricingOptions> ModelPricing { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gemini-2.5-flash"] = new() { InputPerMillionUsd = 0.15m, OutputPerMillionUsd = 0.60m },
        ["default"] = new() { InputPerMillionUsd = 0.15m, OutputPerMillionUsd = 0.60m }
    };
}

public sealed class AiModelPricingOptions
{
    public decimal InputPerMillionUsd { get; set; } = 0.15m;
    public decimal OutputPerMillionUsd { get; set; } = 0.60m;
}
