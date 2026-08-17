using ProfeAsistente.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ProfeAsistente.Api.Services.AI;

public interface IAiCostEstimator
{
    decimal EstimateUsd(string? model, int? inputTokens, int? outputTokens);
}

public sealed class AiCostEstimator : IAiCostEstimator
{
    private readonly AiUsageOptions _options;

    public AiCostEstimator(IOptions<AiUsageOptions> options) => _options = options.Value;

    public decimal EstimateUsd(string? model, int? inputTokens, int? outputTokens)
    {
        var pricing = Resolve(model);
        var input = Math.Max(0, inputTokens ?? 0);
        var output = Math.Max(0, outputTokens ?? 0);
        var cost = input / 1_000_000m * pricing.InputPerMillionUsd
                   + output / 1_000_000m * pricing.OutputPerMillionUsd;
        return Math.Round(cost, 6, MidpointRounding.AwayFromZero);
    }

    private AiModelPricingOptions Resolve(string? model)
    {
        if (!string.IsNullOrWhiteSpace(model)
            && _options.ModelPricing.TryGetValue(model.Trim(), out var exact))
            return exact;
        if (_options.ModelPricing.TryGetValue("default", out var fallback))
            return fallback;
        return new AiModelPricingOptions();
    }
}
