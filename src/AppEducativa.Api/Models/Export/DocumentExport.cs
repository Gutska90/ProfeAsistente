using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Models.Export;

public class DocumentExport
{
    public Guid Id { get; set; }
    public ExportDocumentType DocumentType { get; set; }
    public ExportAudience Audience { get; set; }
    public Guid? PlanningId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? EducationalDocumentId { get; set; }
    public ExportStatus Status { get; set; } = ExportStatus.Pending;
    public string FileName { get; set; } = string.Empty;
    public string? RelativeFilePath { get; set; }
    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public long? SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? RequestedBy { get; set; }
    public string OptionsJson { get; set; } = "{}";
    public Guid? CurriculumSnapshotId { get; set; }
    public Guid? CurriculumReleaseId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string WarningsJson { get; set; } = "[]";
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();
}
