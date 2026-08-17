namespace ProfeAsistente.Shared.Dtos;

public sealed class AiUsageSummaryDto
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public int TotalGenerations { get; set; }
    public int SuccessfulGenerations { get; set; }
    public int FailedGenerations { get; set; }
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public long AvgLatencyMs { get; set; }
    public IReadOnlyList<AiUsagePurposeBreakdownDto> ByPurpose { get; set; } = [];
}

public sealed class AiUsagePurposeBreakdownDto
{
    public string Purpose { get; set; } = string.Empty;
    public int Count { get; set; }
    public int SuccessCount { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public long AvgLatencyMs { get; set; }
}

public sealed class AiUsageRecordDto
{
    public Guid Id { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string PromptId { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public long LatencyMilliseconds { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? DocumentId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
