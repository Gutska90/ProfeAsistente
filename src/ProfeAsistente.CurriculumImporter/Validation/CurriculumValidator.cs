using ProfeAsistente.CurriculumImporter.Abstractions;
using ProfeAsistente.Shared.Dtos;

namespace ProfeAsistente.CurriculumImporter.Validation;

public class CurriculumValidator : ICurriculumValidator
{
    public CurriculumValidationResult Validate(CurriculumExtractionResult extraction)
    {
        var result = new CurriculumValidationResult();
        result.Warnings.AddRange(extraction.Advertencias);
        result.Errors.AddRange(extraction.Errores);

        if (extraction.Level is null || string.IsNullOrWhiteSpace(extraction.Level.Code))
            result.Errors.Add("Falta nivel (code).");
        if (extraction.Subject is null || string.IsNullOrWhiteSpace(extraction.Subject.Code))
            result.Errors.Add("Falta asignatura (code).");

        var oaCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var oa in extraction.LearningObjectives)
        {
            if (string.IsNullOrWhiteSpace(oa.Code))
                result.Errors.Add("OA sin código.");
            else if (!oaCodes.Add(oa.Code.Trim()))
                result.Errors.Add($"Código OA duplicado: {oa.Code}");
            if (string.IsNullOrWhiteSpace(oa.Description) || oa.Description.Trim().Length < 15)
                result.Errors.Add($"Descripción demasiado corta para OA {oa.Code}");
            if (LooksLikeHeaderFooter(oa.Description))
                result.Warnings.Add($"Posible encabezado/pie en OA {oa.Code}");
        }

        foreach (var ind in extraction.EvaluationIndicators)
        {
            if (string.IsNullOrWhiteSpace(ind.LearningObjectiveCode))
                result.Errors.Add("Indicador sin OA asociado.");
            else if (!oaCodes.Contains(ind.LearningObjectiveCode))
                result.Errors.Add($"Indicador apunta a OA inexistente: {ind.LearningObjectiveCode}");
            if (string.IsNullOrWhiteSpace(ind.Description) || ind.Description.Trim().Length < 10)
                result.Errors.Add($"Indicador con texto demasiado corto ({ind.LearningObjectiveCode}).");
        }

        foreach (var u in extraction.Units)
        {
            if (string.IsNullOrWhiteSpace(u.Name))
                result.Errors.Add("Unidad sin nombre.");
            if (u.LearningObjectiveCodes.Count == 0)
                result.Warnings.Add($"Unidad {u.Number} sin OA vinculados.");
            foreach (var code in u.LearningObjectiveCodes)
            {
                if (!oaCodes.Contains(code))
                    result.Errors.Add($"Unidad {u.Number} referencia OA inexistente: {code}");
            }
        }

        if (extraction.LearningObjectives.Count == 0)
            result.Errors.Add("La extracción no contiene Objetivos de Aprendizaje.");

        return result;
    }

    private static bool LooksLikeHeaderFooter(string text)
    {
        var t = text.ToLowerInvariant();
        return t.Contains("ministerio de educación") || t.Contains("página ") || t.Contains("bases curriculares");
    }
}
