using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Shared.Dtos;

public sealed class CreateExportRequest
{
    public ExportDocumentType DocumentType { get; set; }
    public ExportAudience Audience { get; set; } = ExportAudience.Teacher;
    public Guid? PlanningId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? EducationalDocumentId { get; set; }
    public bool IncludeCurriculumReferences { get; set; } = true;
    public bool IncludeIndicators { get; set; } = true;
    public bool IncludeSkills { get; set; } = true;
    public bool IncludeAttitudes { get; set; } = true;
    public bool IncludeTransversalObjectives { get; set; } = true;
    public bool IncludeAnswerKey { get; set; }
    public bool IncludeSpecificationTable { get; set; }
    public bool IncludeTeacherNotes { get; set; }
    public bool IncludePageNumbers { get; set; } = true;
    public bool IncludeHeader { get; set; } = true;
    public bool IncludeFooter { get; set; } = true;
    public bool ConfirmOutdatedExport { get; set; }
    public bool PageBreakPerClass { get; set; } = true;
    public string? SchoolName { get; set; }
    public string? TeacherName { get; set; }
    public string? CourseName { get; set; }
    public string? CustomTitle { get; set; }
    public string? AdditionalInstructions { get; set; }
}

public sealed class ExportResultDto
{
    public Guid ExportId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<string> Warnings { get; set; } = [];
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class ExportSummaryDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid? PlanningId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? EducationalDocumentId { get; set; }
}

public sealed class ExportStorageSummaryDto
{
    public int FileCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public int CompletedCount { get; set; }
    public int ExpiredCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime? OldestRequestedAt { get; set; }
    public DateTime? NewestRequestedAt { get; set; }
}

public sealed class ExportCleanupResultDto
{
    public int ExpiredMarked { get; set; }
    public int FilesDeleted { get; set; }
    public List<string> Warnings { get; set; } = [];
}
