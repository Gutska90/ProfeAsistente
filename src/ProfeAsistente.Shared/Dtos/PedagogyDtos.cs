namespace ProfeAsistente.Shared.Dtos;

/// <summary>Reporte interno de calidad pedagógica (determinista) tras generar un material.</summary>
public sealed class PedagogicalQualityReport
{
    public bool Passed { get; init; }
    /// <summary>¿Pasó validación estructural/esquema (no implica alineación pedagógica).</summary>
    public bool SchemaValidity { get; init; }
    /// <summary>% alineación OA código/id vs contexto autorizado (independiente de SchemaValidity).</summary>
    public int ObjectiveAlignmentPercent { get; init; }
    public int IndicatorCoveragePercent { get; init; }
    public int CognitiveDiversityPercent { get; init; }
    public bool StructureOk { get; init; }
    public bool AnswersOk { get; init; }
    public bool DuplicationOk { get; init; }
    public int RequestedItemCount { get; init; }
    public int ActualItemCount { get; init; }
    public string ObjectiveCode { get; init; } = string.Empty;
    public string PromptVersion { get; init; } = string.Empty;
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public string SummaryLine { get; init; } = string.Empty;
}

public sealed class SubmitMaterialFeedbackRequest
{
    public bool Useful { get; init; }
    /// <summary>NotAligned | TooHard | TooEasy | Duplicated | Unclear | ContentError | Other</summary>
    public string? Reason { get; init; }
    public string? Comment { get; init; }
}

public sealed class MaterialFeedbackDto
{
    public Guid Id { get; init; }
    public Guid DocumentId { get; init; }
    public bool Useful { get; init; }
    public string? Reason { get; init; }
    public string? Comment { get; init; }
    public string PromptVersion { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public static class MaterialFeedbackReasons
{
    public const string NotAligned = "NotAligned";
    public const string TooHard = "TooHard";
    public const string TooEasy = "TooEasy";
    public const string Duplicated = "Duplicated";
    public const string Unclear = "Unclear";
    public const string ContentError = "ContentError";
    public const string WrongContent = "WrongContent";
    public const string BadAnswerKey = "BadAnswerKey";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All =
    [
        NotAligned, TooHard, TooEasy, Duplicated, Unclear, ContentError, WrongContent, BadAnswerKey, Other
    ];

    public static string Label(string? reason) => reason switch
    {
        NotAligned => "No está alineado al OA",
        TooHard => "Demasiado difícil",
        TooEasy => "Demasiado fácil",
        Duplicated => "Preguntas repetidas",
        Unclear => "Instrucciones poco claras",
        ContentError => "Error de contenido",
        WrongContent => "Contenido incorrecto",
        BadAnswerKey => "Clave de respuestas mala",
        Other => "Otro",
        _ => "—"
    };
}
