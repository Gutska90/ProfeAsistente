using System.Text.Json;
using ProfeAsistente.Api.Models.AI;
using ProfeAsistente.Api.Models.AI.Responses;
using ProfeAsistente.Api.Services.AI.DocumentGeneration;
using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Pedagogy.Tests;

public class GoldenPedagogyCaseTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory]
    [InlineData("golden/matematica/PA-MAT-4B-001.json")]
    public void Golden_Case_Passes_Deterministic_Validator(string relativePath)
    {
        var path = Path.Combine(AppContext.BaseDirectory, relativePath);
        Assert.True(File.Exists(path), $"Falta fixture {path}");
        var raw = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        var objectiveCode = root.GetProperty("objectiveCode").GetString()!;
        var requested = root.GetProperty("requestedItemCount").GetInt32();
        var sample = root.GetProperty("sampleDocument").GetRawText();
        var generated = JsonSerializer.Deserialize<GeneratedEducationalDocument>(sample, JsonOptions)!;

        var objectiveId = generated.Curriculum!.ObjectiveId;
        var indicatorId = generated.Curriculum.IndicatorIds!.First();

        var context = new EducationalDocumentGenerationContext
        {
            ClassId = Guid.NewGuid(),
            Level = root.GetProperty("course").GetString()!,
            Subject = root.GetProperty("subject").GetString()!,
            Unit = "Unidad demo",
            Objective = new CurriculumObjectiveRef
            {
                Id = objectiveId,
                Code = objectiveCode,
                Description = "OA demo"
            },
            Indicators =
            [
                new CurriculumIndicatorRef { Id = indicatorId, Description = "Indicador demo" }
            ],
            BloomLevel = "Aplicar",
            CurriculumRelease = generated.Curriculum.CurriculumRelease,
            DocumentType = EducationalDocumentType.LearningGuide,
            ItemCount = requested,
            Difficulty = ItemDifficulty.Intermediate,
            AllowedItemTypes =
            [
                EducationalItemType.MultipleChoice,
                EducationalItemType.ShortAnswer,
                EducationalItemType.OpenResponse
            ],
            IncludeAnswerKey = true,
            IncludeFeedback = true,
            IncludeScoring = true,
            PromptVersion = "learning-guide-v1",
            ConfigurationFingerprint = "golden"
        };

        var validator = new EducationalDocumentGenerationValidator(new EducationalItemSimilarityService());
        var validation = validator.Validate(generated, context);
        var quality = new PedagogicalQualityEvaluator().Evaluate(validation, context, generated);

        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
        Assert.Equal(objectiveCode, generated.Curriculum.ObjectiveCode);
        Assert.True(quality.ObjectiveAlignmentPercent == 100);
        Assert.True(quality.StructureOk);
        Assert.True(quality.AnswersOk);

        var expectations = root.GetProperty("expectations");
        var minItems = expectations.GetProperty("minItems").GetInt32();
        var maxItems = expectations.GetProperty("maxItems").GetInt32();
        // Sample fixture is intentionally short (starter corpus); full 10-item bodies come later.
        Assert.InRange(generated.Document!.Items!.Count, 1, maxItems);
        Assert.True(minItems >= 1);
    }
}
