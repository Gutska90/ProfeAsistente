using System.Text.RegularExpressions;
using ProfeAsistente.Api.Models.AI;
using ProfeAsistente.Api.Models.AI.Responses;
using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Services.AI.DocumentGeneration;

public sealed class EducationalDocumentValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public GeneratedEducationalDocument? Normalized { get; init; }
}

public interface IEducationalDocumentGenerationValidator
{
    EducationalDocumentValidationResult Validate(
        GeneratedEducationalDocument structure,
        EducationalDocumentGenerationContext context);
}

public sealed class EducationalDocumentGenerationValidator : IEducationalDocumentGenerationValidator
{
    private static readonly Regex ExecutableHtmlRegex = new(
        @"<\s*(script|iframe|object|embed|link|meta)\b|javascript\s*:|on\w+\s*=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IEducationalItemSimilarityService _similarity;

    public EducationalDocumentGenerationValidator(IEducationalItemSimilarityService similarity)
        => _similarity = similarity;

    public EducationalDocumentValidationResult Validate(
        GeneratedEducationalDocument structure,
        EducationalDocumentGenerationContext context)
    {
        var errors = new List<string>();
        var warnings = new List<string>(structure.Warnings ?? []);

        if (structure.Curriculum is null || structure.Document is null)
            return Fail(["La respuesta no incluye curriculum o document."], warnings);

        var cur = structure.Curriculum;
        var doc = structure.Document;

        if (cur.ObjectiveId != context.Objective.Id)
            errors.Add("objectiveId no coincide con el OA de la clase.");
        if (!string.Equals(cur.ObjectiveCode, context.Objective.Code, StringComparison.OrdinalIgnoreCase))
            errors.Add($"objectiveCode distinto del OA ({context.Objective.Code}).");
        if (!string.Equals(cur.CurriculumRelease, context.CurriculumRelease, StringComparison.Ordinal))
            errors.Add($"curriculumRelease no coincide ({context.CurriculumRelease}).");

        var allowedIndicators = context.Indicators.Select(i => i.Id).ToHashSet();
        foreach (var id in cur.IndicatorIds ?? [])
        {
            if (!allowedIndicators.Contains(id))
                errors.Add($"Indicador curricular desconocido: {id}");
        }

        if (string.IsNullOrWhiteSpace(doc.Title))
            errors.Add("El documento no tiene título.");
        if (string.IsNullOrWhiteSpace(doc.Instructions))
            errors.Add("El documento no tiene instrucciones.");
        if (doc.Items is null || doc.Items.Count == 0)
            errors.Add("El documento no tiene ítems.");

        if (doc.Items is not null && Math.Abs(doc.Items.Count - context.ItemCount) > Math.Max(2, context.ItemCount / 3))
            warnings.Add($"La cantidad de ítems ({doc.Items.Count}) difiere de la solicitada ({context.ItemCount}).");

        var usedIndicators = new HashSet<Guid>();
        if (doc.Items is not null)
        {
            foreach (var item in doc.Items.OrderBy(i => i.Order))
                ValidateItem(item, allowedIndicators, context, errors, warnings, usedIndicators);

            warnings.AddRange(_similarity.DetectDuplicates(doc.Items));
        }

        foreach (var indicator in context.Indicators)
        {
            if (!usedIndicators.Contains(indicator.Id))
                warnings.Add($"El indicador {indicator.Id} no tiene cobertura en ítems.");
        }

        if (context.DocumentType == EducationalDocumentType.Assessment)
            ValidateSpecification(doc, context, allowedIndicators, errors, warnings);

        if (context.IncludeScoring && doc.Items is { Count: > 0 })
        {
            var sum = doc.Items.Sum(i => i.Points);
            if (doc.TotalPoints is null or <= 0)
                doc.TotalPoints = sum;
            else if (Math.Abs(doc.TotalPoints.Value - sum) > 0.51m)
                errors.Add($"El puntaje total ({doc.TotalPoints}) no coincide con la suma de ítems ({sum}).");
        }

        ScanExecutable(structure, errors);

        if (errors.Count > 0)
            return Fail(errors, warnings);

        structure.Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return new EducationalDocumentValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = structure.Warnings,
            Normalized = structure
        };
    }

    private static void ValidateItem(
        GeneratedEducationalItem item,
        HashSet<Guid> allowedIndicators,
        EducationalDocumentGenerationContext context,
        List<string> errors,
        List<string> warnings,
        HashSet<Guid> usedIndicators)
    {
        var label = $"Ítem {item.Order}";
        if (string.IsNullOrWhiteSpace(item.Statement))
            errors.Add($"{label}: enunciado vacío.");
        if (item.Points <= 0 && context.IncludeScoring)
            errors.Add($"{label}: puntaje debe ser positivo.");
        if (string.IsNullOrWhiteSpace(item.BloomLevel))
            warnings.Add($"{label}: sin nivel de Bloom.");
        if (item.IndicatorIds is null || item.IndicatorIds.Count == 0)
            errors.Add($"{label}: debe asociarse al menos un indicador.");
        else
        {
            foreach (var id in item.IndicatorIds)
            {
                if (!allowedIndicators.Contains(id))
                    errors.Add($"{label}: indicador no autorizado {id}.");
                else
                    usedIndicators.Add(id);
            }
        }

        if (!Enum.TryParse<EducationalItemType>(item.Type, true, out var itemType))
        {
            errors.Add($"{label}: tipo inválido ({item.Type}).");
            return;
        }

        if (context.AllowedItemTypes.Count > 0 && !context.AllowedItemTypes.Contains(itemType))
            warnings.Add($"{label}: tipo {itemType} no estaba en la lista permitida.");

        switch (itemType)
        {
            case EducationalItemType.MultipleChoice:
                ValidateMultipleChoice(item, label, errors, warnings);
                break;
            case EducationalItemType.TrueFalse:
                if (string.IsNullOrWhiteSpace(item.ExpectedAnswer)
                    && (item.Options is null || item.Options.Count(o => o.IsCorrect) != 1))
                    errors.Add($"{label}: verdadero/falso requiere respuesta correcta.");
                break;
            case EducationalItemType.OpenResponse:
            case EducationalItemType.ShortAnswer:
            case EducationalItemType.ProblemSolving:
                if (string.IsNullOrWhiteSpace(item.ExpectedAnswer) && string.IsNullOrWhiteSpace(item.Explanation))
                    errors.Add($"{label}: respuesta abierta requiere pauta o explicación.");
                break;
            case EducationalItemType.Matching:
                if (item.Options is null || item.Options.Count < 2)
                    errors.Add($"{label}: emparejamiento requiere correspondencias.");
                break;
        }
    }

    private static void ValidateMultipleChoice(
        GeneratedEducationalItem item, string label, List<string> errors, List<string> warnings)
    {
        var options = item.Options ?? [];
        if (options.Count is < 3 or > 5)
            errors.Add($"{label}: selección múltiple debe tener entre 3 y 5 alternativas.");
        if (options.Any(o => string.IsNullOrWhiteSpace(o.Text)))
            errors.Add($"{label}: hay alternativas vacías.");
        var correct = options.Count(o => o.IsCorrect);
        if (correct != 1)
            errors.Add($"{label}: debe haber exactamente una alternativa correcta.");
        var distinct = options.Select(o => o.Text.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (distinct != options.Count)
            errors.Add($"{label}: alternativas duplicadas.");

        var correctOpt = options.FirstOrDefault(o => o.IsCorrect);
        if (correctOpt is not null)
        {
            var avgLen = options.Average(o => o.Text.Length);
            if (correctOpt.Text.Length > avgLen * 1.8)
                warnings.Add($"{label}: la alternativa correcta es notoriamente más larga.");
        }
    }

    private static void ValidateSpecification(
        GeneratedEducationalDocumentBody doc,
        EducationalDocumentGenerationContext context,
        HashSet<Guid> allowedIndicators,
        List<string> errors,
        List<string> warnings)
    {
        var table = doc.SpecificationTable ?? [];
        if (table.Count == 0)
        {
            // Auto-build a minimal table if missing — warning + synthetic rows
            warnings.Add("Tabla de especificaciones ausente; se reconstruyó desde los ítems.");
            doc.SpecificationTable = (doc.Items ?? [])
                .SelectMany(i => (i.IndicatorIds ?? []).Select(id => new { i, id }))
                .GroupBy(x => new { x.id, Bloom = x.i.BloomLevel })
                .Select(g => new GeneratedSpecificationRow
                {
                    IndicatorId = g.Key.id,
                    BloomLevel = g.Key.Bloom,
                    ItemCount = g.Count(),
                    TotalPoints = g.Sum(x => x.i.Points),
                    WeightPercentage = 0
                })
                .ToList();
            table = doc.SpecificationTable;
            var totalPts = table.Sum(r => r.TotalPoints);
            if (totalPts > 0)
            {
                foreach (var row in table)
                    row.WeightPercentage = Math.Round(row.TotalPoints * 100m / totalPts, 2);
            }
        }

        foreach (var row in table)
        {
            if (!allowedIndicators.Contains(row.IndicatorId))
                errors.Add($"Especificación con indicador no autorizado: {row.IndicatorId}");
        }

        var weight = table.Sum(r => r.WeightPercentage);
        if (Math.Abs(weight - 100m) > 2m)
            errors.Add($"La suma de ponderaciones es {weight}% y debe aproximarse a 100%.");

        foreach (var indicator in context.Indicators)
        {
            if (!table.Any(r => r.IndicatorId == indicator.Id))
                errors.Add($"Indicador sin cobertura en tabla de especificaciones: {indicator.Id}");
        }

        var itemCountFromTable = table.Sum(r => r.ItemCount);
        if (doc.Items is not null && itemCountFromTable > 0
            && Math.Abs(itemCountFromTable - doc.Items.Count) > 1)
            warnings.Add($"La tabla declara {itemCountFromTable} ítems y el documento tiene {doc.Items.Count}.");
    }

    private static void ScanExecutable(GeneratedEducationalDocument structure, List<string> errors)
    {
        foreach (var item in structure.Document.Items ?? [])
        {
            foreach (var text in new[] { item.Statement, item.ExpectedAnswer, item.Explanation, item.Instructions })
            {
                if (!string.IsNullOrEmpty(text) && ExecutableHtmlRegex.IsMatch(text))
                {
                    errors.Add("La respuesta contiene HTML o contenido ejecutable no permitido.");
                    return;
                }
            }
        }
    }

    private static EducationalDocumentValidationResult Fail(List<string> errors, List<string> warnings) =>
        new() { IsValid = false, Errors = errors, Warnings = warnings };
}
