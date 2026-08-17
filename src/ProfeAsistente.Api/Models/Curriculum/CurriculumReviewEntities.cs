using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Models.Curriculum;

public class CurriculumReviewSession
{
    public Guid Id { get; set; }
    public Guid CurriculumImportBatchId { get; set; }
    public CurriculumReviewStatus Estado { get; set; } = CurriculumReviewStatus.NotStarted;
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime FechaUltimaModificacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaCierre { get; set; }
    public string? RevisadoPor { get; set; }
    public string? ObservacionGeneral { get; set; }
    public int VersionRevision { get; set; } = 1;
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();
    public string? ReviewPackageJson { get; set; }
    public string? ReviewPackagePath { get; set; }
    public string? ReviewContentHash { get; set; }
    public string? ReviewContentPath { get; set; }
    public DateTime? ReadyAt { get; set; }
    public string? ReadyBy { get; set; }
    public DateTime? LastValidationAt { get; set; }
    public DateTime? LastDiffAt { get; set; }
    public string? DiffJson { get; set; }
    public string? IssuesJson { get; set; }

    public CurriculumImportBatch? ImportBatch { get; set; }
    public ICollection<CurriculumReviewChange> Changes { get; set; } = [];
    public ICollection<CurriculumReviewComment> Comments { get; set; } = [];
    public ICollection<CurriculumReviewDecision> Decisions { get; set; } = [];
}

public class CurriculumReviewComment
{
    public Guid Id { get; set; }
    public Guid CurriculumReviewSessionId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityTemporaryId { get; set; }
    public string Message { get; set; } = string.Empty;
    public CurriculumCommentSeverity Severity { get; set; } = CurriculumCommentSeverity.Info;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }

    public CurriculumReviewSession? Session { get; set; }
}

public class CurriculumReviewDecision
{
    public Guid Id { get; set; }
    public Guid CurriculumReviewSessionId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityTemporaryId { get; set; } = string.Empty;
    public CurriculumRecordDecision Decision { get; set; } = CurriculumRecordDecision.Pending;
    public string? Reason { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
    public string? DecidedBy { get; set; }

    public CurriculumReviewSession? Session { get; set; }
}

public class CurriculumRelease
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public string? PublishedBy { get; set; }
    public int SourceDocumentCount { get; set; }
    public int ImportBatchCount { get; set; }
    public CurriculumPublicationStatus Status { get; set; } = CurriculumPublicationStatus.Published;
    public string? Notes { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public Guid? CurriculumImportBatchId { get; set; }
}
