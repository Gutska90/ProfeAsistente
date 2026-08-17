using ProfeAsistente.Api.Configuration;
using ProfeAsistente.Api.Services.AI;
using ProfeAsistente.Shared.Enums;
using Microsoft.Extensions.Options;

namespace ProfeAsistente.Api.Tests.Ai;

public class AiCostEstimatorTests
{
    [Fact]
    public void EstimateUsd_UsesConfiguredPerMillionRates()
    {
        var estimator = new AiCostEstimator(Options.Create(new AiUsageOptions
        {
            ModelPricing = new Dictionary<string, AiModelPricingOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["gemini-2.5-flash"] = new() { InputPerMillionUsd = 1.0m, OutputPerMillionUsd = 2.0m }
            }
        }));

        // 1M input + 0.5M output => 1 + 1 = 2
        var cost = estimator.EstimateUsd("gemini-2.5-flash", 1_000_000, 500_000);
        Assert.Equal(2.0m, cost);
    }

    [Fact]
    public void EstimateUsd_FallsBackToDefaultModel()
    {
        var estimator = new AiCostEstimator(Options.Create(new AiUsageOptions()));
        var cost = estimator.EstimateUsd("unknown-model", 1_000_000, 1_000_000);
        Assert.Equal(0.75m, cost); // 0.15 + 0.60
    }

    [Fact]
    public void EstimateUsd_HandlesNullTokensAsZero()
    {
        var estimator = new AiCostEstimator(Options.Create(new AiUsageOptions()));
        Assert.Equal(0m, estimator.EstimateUsd("gemini-2.5-flash", null, null));
    }
}

public class PromptCatalogTests
{
    [Theory]
    [InlineData(EducationalDocumentType.LearningGuide, AiGenerationPurposes.Guide, "learning-guide")]
    [InlineData(EducationalDocumentType.Exercises, AiGenerationPurposes.Exercises, "exercises")]
    [InlineData(EducationalDocumentType.Assessment, AiGenerationPurposes.Assessment, "assessment")]
    public void ForDocument_MapsTypeToPromptAndPurpose(
        EducationalDocumentType type, string purpose, string promptId)
    {
        var (id, version) = PromptCatalog.ForDocument(type);
        Assert.Equal(promptId, id);
        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.Equal(purpose, PromptCatalog.PurposeForDocument(type));
    }

    [Fact]
    public void PurposeForDocument_DetectsExitTicketHint()
    {
        Assert.Equal(
            AiGenerationPurposes.ExitTicket,
            PromptCatalog.PurposeForDocument(EducationalDocumentType.Exercises, "ticket de salida"));
    }

    [Fact]
    public void ForClassStructure_UsesConfiguredVersion()
    {
        var (id, version) = PromptCatalog.ForClassStructure("class-structure-v2");
        Assert.Equal(PromptCatalog.ClassStructureId, id);
        Assert.Equal("class-structure-v2", version);
    }
}
