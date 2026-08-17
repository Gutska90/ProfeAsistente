using AppEducativa.Shared.Enums;

namespace AppEducativa.Shared.Ui;

/// <summary>Etiquetas en español docente. Los enums técnicos no se muestran en la UI.</summary>
public static class MaterialUiLabels
{
    public static string Type(EducationalDocumentType type) => type switch
    {
        EducationalDocumentType.LearningGuide => "Guía",
        EducationalDocumentType.Exercises => "Actividad",
        EducationalDocumentType.Assessment => "Prueba",
        _ => "Material"
    };

    public static string Type(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Material";
        return Enum.TryParse<EducationalDocumentType>(raw, true, out var t) ? Type(t) : raw;
    }

    public static string Status(EducationalDocumentStatus status) => status switch
    {
        EducationalDocumentStatus.Draft => "Borrador",
        EducationalDocumentStatus.UnderReview => "En revisión",
        EducationalDocumentStatus.Reviewed => "Revisado",
        EducationalDocumentStatus.Final => "Final",
        EducationalDocumentStatus.Archived => "Archivado",
        EducationalDocumentStatus.Outdated => "Desactualizado",
        _ => "—"
    };

    public static string Status(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "—";
        return Enum.TryParse<EducationalDocumentStatus>(raw, true, out var s) ? Status(s) : raw;
    }

    public static string Difficulty(ItemDifficulty difficulty) => difficulty switch
    {
        ItemDifficulty.Basic => "Básica",
        ItemDifficulty.Intermediate => "Intermedia",
        ItemDifficulty.Advanced => "Avanzada",
        _ => "—"
    };

    public static string Difficulty(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "—";
        return Enum.TryParse<ItemDifficulty>(raw, true, out var d) ? Difficulty(d) : raw;
    }

    public static EducationalDocumentType? ParseTypeLabel(string? labelOrValue)
    {
        if (string.IsNullOrWhiteSpace(labelOrValue)) return null;
        if (Enum.TryParse<EducationalDocumentType>(labelOrValue, true, out var parsed))
            return parsed;
        return labelOrValue.Trim().ToLowerInvariant() switch
        {
            "guía" or "guia" => EducationalDocumentType.LearningGuide,
            "actividad" or "ejercicios" or "ejercicio" => EducationalDocumentType.Exercises,
            "prueba" or "evaluación" or "evaluacion" or "ticket" => EducationalDocumentType.Assessment,
            _ => null
        };
    }
}
