namespace ProfeAsistente.Api.Models.Export;

public sealed class ExportResult
{
    public Guid ExportId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long? SizeBytes { get; init; }
    public string? Sha256 { get; init; }
    public List<ExportWarning> Warnings { get; init; } = [];
}

public sealed class ExportDocumentContext
{
    public Guid? PlanningId { get; init; }
    public Guid? ClassId { get; init; }
    public Guid? EducationalDocumentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public List<ExportSection> Sections { get; init; } = [];
}

public sealed class ExportSection
{
    public string Heading { get; init; } = string.Empty;
    public string? Body { get; init; }
    public bool Required { get; init; }
}

public sealed class ExportWarning
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
