namespace ProfeAsistente.Api.Models.AI.Responses;

public sealed class GeneratedEducationalDocument
{
    public bool RequiresReview { get; set; }
    public List<string> Warnings { get; set; } = [];
    public GeneratedCurriculumReference Curriculum { get; set; } = new();
    public GeneratedEducationalDocumentBody Document { get; set; } = new();
}

public sealed class GeneratedEducationalDocumentBody
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public int? EstimatedDurationMinutes { get; set; }
    public decimal? TotalPoints { get; set; }
    public List<GeneratedEducationalItem> Items { get; set; } = [];
    public List<GeneratedSpecificationRow> SpecificationTable { get; set; } = [];
    public List<GeneratedAnswerKeyEntry> AnswerKey { get; set; } = [];
}

public sealed class GeneratedEducationalItem
{
    public string TemporaryId { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public string Difficulty { get; set; } = "Intermediate";
    public string BloomLevel { get; set; } = string.Empty;
    public List<Guid> IndicatorIds { get; set; } = [];
    public decimal Points { get; set; } = 1;
    public List<GeneratedEducationalOption> Options { get; set; } = [];
    public string? ExpectedAnswer { get; set; }
    public string? Explanation { get; set; }
    public string? TeacherNotes { get; set; }
}

public sealed class GeneratedEducationalOption
{
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? Feedback { get; set; }
}

public sealed class GeneratedSpecificationRow
{
    public Guid IndicatorId { get; set; }
    public string BloomLevel { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalPoints { get; set; }
    public decimal WeightPercentage { get; set; }
}

public sealed class GeneratedAnswerKeyEntry
{
    public string TemporaryId { get; set; } = string.Empty;
    public string? ExpectedAnswer { get; set; }
    public List<int> CorrectOptionOrders { get; set; } = [];
    public string? Explanation { get; set; }
}
