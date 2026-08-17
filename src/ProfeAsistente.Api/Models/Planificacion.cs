using ProfeAsistente.Api.Models.Curriculum;
using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Models;

public class Planificacion
{
    public Guid Id { get; set; }
    public Guid NivelId { get; set; }
    /// <summary>FK a NivelAsignatura (no a Asignatura global).</summary>
    public Guid NivelAsignaturaId { get; set; }
    public Guid UnidadId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public EstadoPlanificacion Estado { get; set; } = EstadoPlanificacion.EnCurso;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Guid? InstitutionId { get; set; }
    public Guid? AcademicPeriodId { get; set; }
    public Guid? SchoolCourseId { get; set; }
    public Guid? CourseSubjectId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public PlanningVisibility Visibility { get; set; } = PlanningVisibility.Private;
    public bool IsShared { get; set; }
    public string? PeiAlignment { get; set; }
    public string? PmeAction { get; set; }
    public string? DuaUnitNotes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    public Nivel? Nivel { get; set; }
    public NivelAsignatura? NivelAsignatura { get; set; }
    public Unidad? Unidad { get; set; }
    public ICollection<Clase> Clases { get; set; } = [];
}

public class Clase
{
    public Guid Id { get; set; }
    public Guid PlanificacionId { get; set; }
    public int Numero { get; set; }
    public DateOnly Fecha { get; set; }
    public Guid ObjetivoAprendizajeId { get; set; }
    public string NivelBloom { get; set; } = "Recordar";
    public string? DescripcionInicio { get; set; }
    public string? DescripcionDesarrollo { get; set; }
    public string? DescripcionCierre { get; set; }
    public string? Titulo { get; set; }
    public string? Proposito { get; set; }
    public PlanningClassType ClassType { get; set; } = PlanningClassType.Regular;
    public TimeOnly? StartTime { get; set; }
    public int? DurationMinutes { get; set; }
    public DateOnly? ActualDate { get; set; }
    public int? ActualDurationMinutes { get; set; }
    public string? CompletionNotes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public EstadoClase Estado { get; set; } = EstadoClase.Planificada;

    public Planificacion? Planificacion { get; set; }
    public ObjetivoAprendizaje? ObjetivoAprendizaje { get; set; }
    public ICollection<ClaseIndicadorEvaluacion> Indicadores { get; set; } = [];
    public ICollection<Documento> Documentos { get; set; } = [];
    public ClaseCurriculumSnapshot? CurriculumSnapshot { get; set; }
}

public class ClaseIndicadorEvaluacion
{
    public Guid ClaseId { get; set; }
    public Guid IndicadorEvaluacionId { get; set; }

    public Clase? Clase { get; set; }
    public IndicadorEvaluacion? IndicadorEvaluacion { get; set; }
}
