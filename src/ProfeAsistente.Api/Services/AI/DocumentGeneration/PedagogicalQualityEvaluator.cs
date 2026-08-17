using ProfeAsistente.Api.Models.AI;
using ProfeAsistente.Api.Models.AI.Responses;
using ProfeAsistente.Api.Services.AI.DocumentGeneration;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Services.AI.DocumentGeneration;

public interface IPedagogicalQualityEvaluator
{
    PedagogicalQualityReport Evaluate(
        EducationalDocumentValidationResult validation,
        EducationalDocumentGenerationContext context,
        GeneratedEducationalDocument? document = null);
}

/// <summary>Métricas deterministas a partir del validador existente (sin llamar a Gemini).</summary>
public sealed class PedagogicalQualityEvaluator : IPedagogicalQualityEvaluator
{
    public PedagogicalQualityReport Evaluate(
        EducationalDocumentValidationResult validation,
        EducationalDocumentGenerationContext context,
        GeneratedEducationalDocument? document = null)
    {
        var doc = document ?? validation.Normalized;
        var items = doc?.Document?.Items ?? [];
        var errors = validation.Errors.ToList();
        var warnings = validation.Warnings.ToList();

        var schemaValidity = validation.IsValid;
        var structureOk = schemaValidity
                          && !string.IsNullOrWhiteSpace(doc?.Document?.Title)
                          && items.Count > 0;

        var answersOk = items.Count == 0 || items.All(ItemHasAnswerSupport);
        if (!answersOk && schemaValidity)
            warnings.Add("Algunos ítems no tienen respuesta esperada ni explicación.");

        var duplicationOk = !warnings.Any(w =>
            w.Contains("duplic", StringComparison.OrdinalIgnoreCase)
            || w.Contains("idéntic", StringComparison.OrdinalIgnoreCase)
            || w.Contains("similar", StringComparison.OrdinalIgnoreCase));

        // Alineación OA: independiente de SchemaValidity (no mezclar "válido" con "alineado").
        var alignment = 0;
        if (doc?.Curriculum is not null
            && doc.Curriculum.ObjectiveId == context.Objective.Id
            && string.Equals(doc.Curriculum.ObjectiveCode, context.Objective.Code, StringComparison.OrdinalIgnoreCase))
            alignment = 100;
        else if (doc?.Curriculum is null)
            alignment = 0;
        else if (errors.Any(e => e.Contains("objective", StringComparison.OrdinalIgnoreCase)
                                 || e.Contains("OA", StringComparison.OrdinalIgnoreCase)))
            alignment = 0;
        else
            alignment = 40;

        var authorized = context.Indicators.Select(i => i.Id).ToHashSet();
        var covered = items
            .SelectMany(i => i.IndicatorIds ?? [])
            .Where(authorized.Contains)
            .Distinct()
            .Count();
        var indicatorCoverage = authorized.Count == 0
            ? 100
            : (int)Math.Round(100.0 * covered / authorized.Count);

        var blooms = items
            .Select(i => (i.BloomLevel ?? string.Empty).Trim())
            .Where(b => b.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var cognitiveDiversity = items.Count == 0
            ? 0
            : (int)Math.Round(100.0 * Math.Min(blooms, 4) / 4.0);

        if (indicatorCoverage < 70)
            warnings.Add($"Cobertura de indicadores baja ({indicatorCoverage}%).");
        if (cognitiveDiversity < 50 && items.Count >= 4)
            warnings.Add("Baja diversidad cognitiva (Bloom) en los ítems.");
        if (alignment < 100)
            warnings.Add("Alineación de OA incompleta respecto del contexto autorizado.");

        var passed = schemaValidity && structureOk && answersOk && alignment == 100;
        var summary =
            $"OA {context.Objective.Code}: schema {(schemaValidity ? "OK" : "FAIL")} · " +
            $"alineación {alignment}% · indicadores {indicatorCoverage}% · " +
            $"Bloom {cognitiveDiversity}% · ítems {items.Count}/{context.ItemCount}" +
            (passed ? " · OK" : " · revisar");

        return new PedagogicalQualityReport
        {
            Passed = passed,
            SchemaValidity = schemaValidity,
            ObjectiveAlignmentPercent = alignment,
            IndicatorCoveragePercent = indicatorCoverage,
            CognitiveDiversityPercent = cognitiveDiversity,
            StructureOk = structureOk,
            AnswersOk = answersOk,
            DuplicationOk = duplicationOk,
            RequestedItemCount = context.ItemCount,
            ActualItemCount = items.Count,
            ObjectiveCode = context.Objective.Code,
            PromptVersion = context.PromptVersion,
            Errors = errors,
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            SummaryLine = summary
        };
    }

    private static bool ItemHasAnswerSupport(GeneratedEducationalItem item)
    {
        if (!Enum.TryParse<EducationalItemType>(item.Type, true, out var itemType))
            return !string.IsNullOrWhiteSpace(item.ExpectedAnswer)
                   || !string.IsNullOrWhiteSpace(item.Explanation)
                   || item.Options?.Any(o => o.IsCorrect) == true;

        if (itemType is EducationalItemType.MultipleChoice or EducationalItemType.TrueFalse)
            return item.Options?.Any(o => o.IsCorrect) == true;
        return !string.IsNullOrWhiteSpace(item.ExpectedAnswer)
               || !string.IsNullOrWhiteSpace(item.Explanation);
    }
}
