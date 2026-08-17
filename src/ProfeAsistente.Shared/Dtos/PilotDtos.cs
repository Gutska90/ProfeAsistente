namespace ProfeAsistente.Shared.Dtos;

public sealed class PilotMetricsDto
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public int MaterialsCreated { get; set; }
    public int MaterialsCurrent { get; set; }
    public int MaterialsExported { get; set; }
    /// <summary>% de materiales (periodo) con al menos una exportación Word.</summary>
    public double ExportRatePercent { get; set; }
    public int FeedbackCount { get; set; }
    public int FeedbackUsefulCount { get; set; }
    public double FeedbackUsefulPercent { get; set; }
    public int MaterialsReused { get; set; }
    public int ClassesWithMaterial { get; set; }
    public int ClassesWithEvidence { get; set; }
    /// <summary>% de clases con material que también tienen evaluación/evidencia.</summary>
    public double EvidenceCoveragePercent { get; set; }
    public int AiGenerations { get; set; }
    public decimal EstimatedAiCostUsd { get; set; }
    public long AvgAiLatencyMs { get; set; }
    public int SessionReports { get; set; }
    public double? AvgMinutesSavedReported { get; set; }
    public string SummaryLine { get; set; } = string.Empty;
}

public sealed class SubmitPilotSessionReportRequest
{
    public Guid? ClassId { get; set; }
    public int MinutesSavedEstimate { get; set; }
    public bool? WouldUseAgain { get; set; }
    public bool? MaterialsUsedInClass { get; set; }
    /// <summary>Under15 | From15To30 | From30To60 | From1To2Hours | Over2Hours</summary>
    public string? WithoutAppDurationBucket { get; set; }
    public string? Comment { get; set; }
}

public sealed class PilotSessionReportDto
{
    public Guid Id { get; set; }
    public Guid? ClassId { get; set; }
    public int MinutesSavedEstimate { get; set; }
    public bool? WouldUseAgain { get; set; }
    public bool? MaterialsUsedInClass { get; set; }
    public string? WithoutAppDurationBucket { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class WithoutAppDurationBuckets
{
    public const string Under15 = "Under15";
    public const string From15To30 = "From15To30";
    public const string From30To60 = "From30To60";
    public const string From1To2Hours = "From1To2Hours";
    public const string Over2Hours = "Over2Hours";

    public static readonly IReadOnlyList<string> All =
        [Under15, From15To30, From30To60, From1To2Hours, Over2Hours];

    public static bool IsValid(string? value) =>
        string.IsNullOrWhiteSpace(value) || All.Contains(value, StringComparer.OrdinalIgnoreCase);

    public static string Label(string? value) => value switch
    {
        Under15 => "<15 min",
        From15To30 => "15–30 min",
        From30To60 => "30–60 min",
        From1To2Hours => "1–2 horas",
        Over2Hours => ">2 horas",
        _ => "—"
    };
}
