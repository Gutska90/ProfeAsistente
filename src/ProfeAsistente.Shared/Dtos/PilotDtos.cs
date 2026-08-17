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
    public string? Comment { get; set; }
}

public sealed class PilotSessionReportDto
{
    public Guid Id { get; set; }
    public Guid? ClassId { get; set; }
    public int MinutesSavedEstimate { get; set; }
    public bool? WouldUseAgain { get; set; }
    public bool? MaterialsUsedInClass { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
