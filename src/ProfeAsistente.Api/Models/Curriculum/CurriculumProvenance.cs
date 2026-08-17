using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Models.Curriculum;

public class CurriculumSource
{
    public Guid Id { get; set; }
    public string? ExternalId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Dominio { get; set; } = string.Empty;
    public TipoFuenteCurricular TipoFuente { get; set; }
    public FormatoFuenteCurricular Formato { get; set; }
    public string? NivelEsperado { get; set; }
    public string? AsignaturaEsperada { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public DateTime? FechaUltimaRevision { get; set; }

    public ICollection<CurriculumDocument> Documents { get; set; } = [];
}

public class CurriculumDocument
{
    public Guid Id { get; set; }
    public Guid CurriculumSourceId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string UrlOriginal { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string? NumeroDecreto { get; set; }
    public DateTime? FechaPublicacion { get; set; }
    public DateTime FechaDescarga { get; set; } = DateTime.UtcNow;
    public string? ETag { get; set; }
    public string? LastModified { get; set; }
    public string HashSha256 { get; set; } = string.Empty;
    public string RutaArchivoLocal { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string VersionDetectada { get; set; } = "1";
    public EstadoProcesamientoDocumento EstadoProcesamiento { get; set; }
    public string? TextoExtraido { get; set; }
    public string? TextoExtraidoPath { get; set; }
    public DateTime? FechaProcesamiento { get; set; }
    public string? ErrorProcesamiento { get; set; }

    public CurriculumSource? Source { get; set; }
}

public class CurriculumImportBatch
{
    public Guid Id { get; set; }
    public string? SourceExternalId { get; set; }
    public Guid? CurriculumSourceId { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaTermino { get; set; }
    public EstadoImportBatch Estado { get; set; } = EstadoImportBatch.EnCurso;
    public CurriculumImportStatus Status { get; set; } = CurriculumImportStatus.Created;
    public int CantidadFuentes { get; set; }
    public int CantidadRegistrosNuevos { get; set; }
    public int CantidadActualizados { get; set; }
    public int CantidadSinCambios { get; set; }
    public int CantidadAdvertencias { get; set; }
    public int CantidadErrores { get; set; }
    public string? DiffJson { get; set; }
    public string? ExtractionJson { get; set; }
    public string? ExtractionJsonPath { get; set; }
    public string? CorrectedJsonPath { get; set; }
    public string? OriginalExtractionJson { get; set; }
    public string? CorrectedExtractionJson { get; set; }
    public int CantidadUnidades { get; set; }
    public int CantidadOA { get; set; }
    public int CantidadIndicadores { get; set; }
    public int CantidadHabilidades { get; set; }
    public int CantidadActitudes { get; set; }
    public double ConfianzaPromedio { get; set; }
    public string? UsuarioRevisor { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public string? Mensaje { get; set; }
    public Guid? CurriculumDocumentId { get; set; }
    public Guid? ActiveReviewSessionId { get; set; }
    public string? ReviewContentHash { get; set; }
    public string? FinalReviewJson { get; set; }
    public string? FinalReviewJsonPath { get; set; }
    public DateTime? ReadyAt { get; set; }
    public string? ReadyBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? CurriculumReleaseId { get; set; }

    public CurriculumSource? CurriculumSource { get; set; }
    public CurriculumDocument? CurriculumDocument { get; set; }
    public ICollection<CurriculumReviewChange> ReviewChanges { get; set; } = [];
    public ICollection<CurriculumReviewSession> ReviewSessions { get; set; } = [];
}

/// <summary>Audit trail for reviewer edits (preview and structured review).</summary>
public class CurriculumReviewChange
{
    public Guid Id { get; set; }
    public Guid CurriculumImportBatchId { get; set; }
    public Guid? CurriculumReviewSessionId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;
    public string EntityTemporaryId { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public CurriculumReviewChangeType ChangeType { get; set; } = CurriculumReviewChangeType.ManualCorrection;
    public string? UsuarioRevisor { get; set; }
    public string? ChangedBy { get; set; }
    public string? Reason { get; set; }
    public DateTime FechaCambio { get; set; } = DateTime.UtcNow;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public bool IsReverted { get; set; }
    public DateTime? RevertedAt { get; set; }

    public CurriculumImportBatch? ImportBatch { get; set; }
    public CurriculumReviewSession? ReviewSession { get; set; }
}

public class CurriculumRecordSource
{
    public Guid Id { get; set; }
    public Guid CurriculumDocumentId { get; set; }
    public string TipoEntidad { get; set; } = string.Empty;
    public Guid EntidadId { get; set; }
    public int? PaginaInicio { get; set; }
    public int? PaginaFin { get; set; }
    public string? FragmentoFuente { get; set; }
    public DateTime? FechaVigenciaDesde { get; set; }
    public DateTime? FechaVigenciaHasta { get; set; }

    public CurriculumDocument? Document { get; set; }
}

public class ClaseCurriculumSnapshot
{
    public Guid Id { get; set; }
    public Guid ClaseId { get; set; }
    public Guid ObjetivoAprendizajeId { get; set; }
    public string CodigoOA { get; set; } = string.Empty;
    public string DescripcionOA { get; set; } = string.Empty;
    public string IndicadoresJson { get; set; } = "[]";
    public string HabilidadesJson { get; set; } = "[]";
    public string ActitudesJson { get; set; } = "[]";
    public string VersionCurricular { get; set; } = "1";
    public Guid? CurriculumDocumentId { get; set; }
    public DateTime FechaSnapshot { get; set; } = DateTime.UtcNow;
    // Nivel/Asignatura/Unidad/Eje names are packed into HabilidadesJson.context until the next migration.
}
