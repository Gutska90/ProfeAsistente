namespace AppEducativa.Shared.Enums;

public enum TipoFuenteCurricular
{
    BaseCurricular = 0,
    ProgramaEstudio = 1,
    Orientacion = 2,
    ManualJson = 3,
    Otro = 9
}

public enum FormatoFuenteCurricular
{
    Pdf = 0,
    Html = 1,
    Json = 2
}

public enum EstadoProcesamientoDocumento
{
    Pendiente = 0,
    Descargado = 1,
    Extraido = 2,
    Validado = 3,
    Error = 9
}

public enum EstadoImportBatch
{
    EnCurso = 0,
    Extraido = 1,
    Validado = 2,
    DiffListo = 3,
    Aprobado = 4,
    Rechazado = 5,
    Error = 9
}

/// <summary>Lifecycle for the reviewed official curriculum import workflow.</summary>
public enum CurriculumImportStatus
{
    Created = 0,
    Downloaded = 1,
    Extracted = 2,
    Validated = 3,
    PendingReview = 4,
    Approved = 5,
    Rejected = 6,
    Imported = 7,
    ReadyForApproval = 8,
    Failed = 9
}

public enum CurriculumReviewStatus
{
    NotStarted = 0,
    InProgress = 1,
    CorrectionsRequired = 2,
    ReadyForApproval = 3,
    Approved = 4,
    Rejected = 5,
    Closed = 6
}

public enum CurriculumRecordDecision
{
    Pending = 0,
    Accepted = 1,
    Corrected = 2,
    Rejected = 3,
    Ignored = 4
}

public enum CurriculumReviewChangeType
{
    ManualCorrection = 0,
    AutomaticNormalization = 1,
    RelationshipChange = 2,
    RecordAdded = 3,
    RecordRemoved = 4,
    Reverted = 5
}

public enum CurriculumPublicationStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public enum TextChangeSignificance
{
    None = 0,
    FormattingOnly = 1,
    Minor = 2,
    Relevant = 3,
    Critical = 4
}

public enum CurriculumCommentSeverity
{
    Info = 0,
    RequiresAction = 1,
    Blocking = 2
}

public enum EstadoRevision
{
    Pendiente = 0,
    Aprobado = 1,
    Rechazado = 2,
    RequiereCorreccion = 3,
    /// <summary>Seed local de demostración; no es contenido oficial MINEDUC.</summary>
    AprobadoParaPruebas = 4
}

/// <summary>Alias semántico del prompt de estabilización.</summary>
public enum EstadoRevisionCurricular
{
    Pendiente = EstadoRevision.Pendiente,
    Aprobado = EstadoRevision.Aprobado,
    Rechazado = EstadoRevision.Rechazado,
    RequiereCorreccion = EstadoRevision.RequiereCorreccion,
    AprobadoParaPruebas = EstadoRevision.AprobadoParaPruebas
}

public enum TipoObjetivoAprendizaje
{
    NoClasificado = 0,
    Basal = 1,
    Complementario = 2,
    Otro = 9
}

public enum TipoCambioCurricular
{
    Nuevo = 0,
    Modificado = 1,
    SinCambios = 2,
    PosiblementeEliminado = 3,
    Conflicto = 4,
    RequiereRevision = 5
}
