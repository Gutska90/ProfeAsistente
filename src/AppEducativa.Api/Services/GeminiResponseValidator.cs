using AppEducativa.Api.Models.Curriculum;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Services;

/// <summary>Valida que Gemini no invente códigos OA ni indicadores inexistentes.</summary>
public static class GeminiResponseValidator
{
    public static bool ValidateMaterial(
        GeminiContentDto content,
        ObjetivoAprendizaje oa,
        IReadOnlyList<IndicadorEvaluacion> indicadores,
        out List<string> warnings)
    {
        warnings = [];
        var indTexts = indicadores.Select(i => i.Descripcion).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var item in content.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.NivelBloom) &&
                NivelBloomHelper.Normalizar(item.NivelBloom) is null)
                warnings.Add($"nivelBloom no reconocido: {item.NivelBloom}");

            var ind = item.IndicadorCodigoODescripcion;
            if (!string.IsNullOrWhiteSpace(ind) && indTexts.Count > 0 &&
                !indTexts.Any(t => t.Contains(ind, StringComparison.OrdinalIgnoreCase)
                                   || ind.Contains(t, StringComparison.OrdinalIgnoreCase)))
                warnings.Add($"Indicador no encontrado en OA {oa.Codigo}: {ind}");
        }

        return warnings.Count == 0;
    }
}
