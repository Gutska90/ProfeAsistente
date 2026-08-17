namespace ProfeAsistente.CurriculumImporter.Models.Extraction;

public sealed record ExtractedPage(int PageNumber, string OriginalText, string NormalizedText);
public sealed record ExtractionWarning(string Code, string Message, int? PageNumber = null);
public sealed record SourceReference(int PageStart, int PageEnd, string Fragment);

public sealed class ExtractedLearningObjectiveCandidate
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SourceReference Source { get; set; } = new(0, 0, string.Empty);
}

public sealed class ExtractedUnitCandidate
{
    public int Number { get; init; }
    public string Name { get; set; } = string.Empty;
    public int? SuggestedHours { get; set; }
    public List<string> LearningObjectiveCodes { get; set; } = [];
    public SourceReference Source { get; set; } = new(0, 0, string.Empty);
}

public sealed class ExtractedIndicatorCandidate
{
    public string LearningObjectiveCode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public SourceReference Source { get; init; } = new(0, 0, string.Empty);
}

public sealed class CurriculumExtractionResult
{
    public string SourceId { get; init; } = string.Empty;
    public List<ExtractedPage> Pages { get; } = [];
    public List<ExtractionWarning> Warnings { get; } = [];
    public bool RequiresManualReview { get; set; }
    public string? ExtractedTextPath { get; set; }
}

public sealed class ExtractedCurriculumPackage
{
    public string SourceId { get; init; } = string.Empty;
    public List<ExtractedUnitCandidate> Units { get; } = [];
    public List<ExtractedLearningObjectiveCandidate> LearningObjectives { get; } = [];
    public List<ExtractedIndicatorCandidate> Indicators { get; } = [];
    public List<string> Skills { get; } = [];
    public List<string> Attitudes { get; } = [];
    public bool RequiresManualReview { get; set; }
}
