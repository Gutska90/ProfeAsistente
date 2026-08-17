using System.Text.RegularExpressions;
using AppEducativa.Api.Models.AI;
using AppEducativa.Api.Models.AI.Responses;

namespace AppEducativa.Api.Services.AI.ClassGeneration;

public sealed class ClassGenerationValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public GeneratedClassStructure? NormalizedStructure { get; init; }
}

public sealed class ClassGenerationValidator
{
    private static readonly Regex ExecutableHtmlRegex = new(
        @"<\s*(script|iframe|object|embed|link|meta)\b|javascript\s*:|on\w+\s*=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ClassGenerationValidationResult Validate(
        GeneratedClassStructure structure,
        ClassGenerationContext context)
    {
        var errors = new List<string>();
        var warnings = new List<string>(structure.Warnings ?? []);

        if (structure.Curriculum is null || structure.Class is null)
        {
            errors.Add("La respuesta no incluye curriculum o class.");
            return Fail(errors, warnings);
        }

        var cur = structure.Curriculum;
        var body = structure.Class;

        if (cur.ObjectiveId != context.Objective.Id)
            errors.Add($"objectiveId no coincide con el OA de la clase ({context.Objective.Id}).");

        if (!string.Equals(cur.ObjectiveCode, context.Objective.Code, StringComparison.OrdinalIgnoreCase))
            errors.Add($"objectiveCode desconocido o distinto del OA ({context.Objective.Code}).");

        if (!string.Equals(cur.CurriculumRelease, context.CurriculumRelease, StringComparison.Ordinal))
            errors.Add($"curriculumRelease no coincide ({context.CurriculumRelease}).");

        var allowedIndicators = context.Indicators.Select(i => i.Id).ToHashSet();
        foreach (var id in cur.IndicatorIds ?? [])
        {
            if (!allowedIndicators.Contains(id))
                errors.Add($"Indicador desconocido: {id}");
        }

        var allowedSkills = context.Skills.Select(s => s.Id).ToHashSet();
        foreach (var id in cur.SkillIds ?? [])
        {
            if (!allowedSkills.Contains(id))
                errors.Add($"Habilidad desconocida: {id}");
        }

        var allowedAttitudes = context.Attitudes.Select(a => a.Id).ToHashSet();
        foreach (var id in cur.AttitudeIds ?? [])
        {
            if (!allowedAttitudes.Contains(id))
                errors.Add($"Actitud desconocida: {id}");
        }

        var allowedOats = context.TransversalObjectives.Select(t => t.Id).ToHashSet();
        foreach (var id in cur.TransversalObjectiveIds ?? [])
        {
            if (!allowedOats.Contains(id))
                errors.Add($"OAT desconocido: {id}");
        }

        ValidatePhase(body.Start, "start", errors);
        ValidatePhase(body.Development, "development", errors);
        ValidatePhase(body.Closure, "closure", errors);

        var sum = body.Start.DurationMinutes + body.Development.DurationMinutes + body.Closure.DurationMinutes;
        var total = body.TotalDurationMinutes > 0 ? body.TotalDurationMinutes : context.DurationMinutes;
        var diff = Math.Abs(sum - total);

        if (diff > 5)
        {
            errors.Add($"La suma de fases ({sum}) no coincide con la duración total ({total}).");
        }
        else if (diff > 0)
        {
            warnings.Add($"Duraciones ajustadas automáticamente (diferencia de {diff} min).");
            NormalizeDurations(body, total);
        }

        if (body.TotalDurationMinutes <= 0)
            body.TotalDurationMinutes = total;

        if (body.Development.DurationMinutes < body.Start.DurationMinutes ||
            body.Development.DurationMinutes < body.Closure.DurationMinutes)
        {
            warnings.Add("La duración de desarrollo suele ser mayor o igual que inicio y cierre.");
        }

        ScanExecutableHtml(structure, errors);

        if (errors.Count > 0)
            return Fail(errors, warnings);

        structure.Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return new ClassGenerationValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = structure.Warnings,
            NormalizedStructure = structure
        };
    }

    private static void ValidatePhase(GeneratedClassPhase phase, string name, List<string> errors)
    {
        if (phase is null)
        {
            errors.Add($"Fase {name} ausente.");
            return;
        }

        if (phase.DurationMinutes <= 0)
            errors.Add($"Fase {name}: duración debe ser positiva.");

        if (string.IsNullOrWhiteSpace(phase.Objective))
            errors.Add($"Fase {name}: objetivo vacío.");

        if (phase.Activities is null || phase.Activities.Count == 0)
            errors.Add($"Fase {name}: debe incluir al menos una actividad.");

        if (phase.Evidence is null || phase.Evidence.Count == 0 ||
            phase.Evidence.All(string.IsNullOrWhiteSpace))
            errors.Add($"Fase {name}: debe incluir evidencias.");
    }

    private static void NormalizeDurations(GeneratedClassBody body, int total)
    {
        var sum = body.Start.DurationMinutes + body.Development.DurationMinutes + body.Closure.DurationMinutes;
        var delta = total - sum;
        body.Development.DurationMinutes = Math.Max(1, body.Development.DurationMinutes + delta);
        body.TotalDurationMinutes = total;
    }

    private static void ScanExecutableHtml(GeneratedClassStructure structure, List<string> errors)
    {
        foreach (var text in EnumerateTexts(structure))
        {
            if (ExecutableHtmlRegex.IsMatch(text))
            {
                errors.Add("La respuesta contiene HTML o contenido ejecutable no permitido.");
                return;
            }
        }
    }

    private static IEnumerable<string> EnumerateTexts(GeneratedClassStructure structure)
    {
        yield return structure.Class.Title;
        yield return structure.Class.Purpose;
        foreach (var phase in new[] { structure.Class.Start, structure.Class.Development, structure.Class.Closure })
        {
            yield return phase.Objective;
            foreach (var s in phase.TeacherActions) yield return s;
            foreach (var s in phase.StudentActions) yield return s;
            foreach (var s in phase.Resources) yield return s;
            foreach (var s in phase.Evidence) yield return s;
            foreach (var a in phase.Activities)
            {
                yield return a.Name;
                yield return a.Description;
            }
        }
    }

    private static ClassGenerationValidationResult Fail(List<string> errors, List<string> warnings) =>
        new()
        {
            IsValid = false,
            Errors = errors,
            Warnings = warnings,
            NormalizedStructure = null
        };
}
