using AppEducativa.CurriculumImporter.Models.Extraction;

namespace AppEducativa.CurriculumImporter.Services.Validation;

public enum ValidationSeverity { Info, Warning, Error, Blocking }
public sealed record CurriculumValidationIssue(ValidationSeverity Severity, string Code, string Message);

public interface ICurriculumExtractionValidator
{
    IReadOnlyList<CurriculumValidationIssue> Validate(ExtractedCurriculumPackage package);
}

public sealed class CurriculumExtractionValidator : ICurriculumExtractionValidator
{
    public IReadOnlyList<CurriculumValidationIssue> Validate(ExtractedCurriculumPackage package)
    {
        var issues = new List<CurriculumValidationIssue>();
        if (package.Units.Count == 0)
            issues.Add(new(ValidationSeverity.Blocking, "no-units", "No se detectaron unidades; requiere revisión manual."));

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var oa in package.LearningObjectives)
        {
            if (!codes.Add(oa.Code))
                issues.Add(new(ValidationSeverity.Error, "duplicate-oa", $"OA duplicado: {oa.Code}."));
            if (string.IsNullOrWhiteSpace(oa.Description))
                issues.Add(new(ValidationSeverity.Warning, "empty-oa-description", $"OA sin descripción: {oa.Code}."));
        }
        foreach (var indicator in package.Indicators)
            if (!codes.Contains(indicator.LearningObjectiveCode))
                issues.Add(new(ValidationSeverity.Error, "orphan-indicator",
                    $"Indicador asociado a OA inexistente: {indicator.LearningObjectiveCode}."));
        return issues;
    }
}
