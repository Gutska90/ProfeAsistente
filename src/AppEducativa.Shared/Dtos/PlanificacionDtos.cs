using AppEducativa.Shared.Enums;

namespace AppEducativa.Shared.Dtos;

public class CrearPlanificacionRequest
{
    public Guid NivelId { get; set; }
    public Guid AsignaturaId { get; set; }
    public Guid UnidadId { get; set; }
    public string? Nombre { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public Guid? InstitutionId { get; set; }
    public Guid? AcademicPeriodId { get; set; }
    public Guid? SchoolCourseId { get; set; }
    public Guid? CourseSubjectId { get; set; }
    public PlanningVisibility Visibility { get; set; } = PlanningVisibility.Private;
}

public class ActualizarPlanificacionRequest
{
    public string? Nombre { get; set; }
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public EstadoPlanificacion? Estado { get; set; }
}

public class CrearClaseRequest
{
    public DateOnly? Fecha { get; set; }
    public Guid? ObjetivoAprendizajeId { get; set; }
    public string? NivelBloom { get; set; }
    public List<Guid>? IndicadorEvaluacionIds { get; set; }
}

public class ActualizarClaseRequest
{
    public DateOnly? Fecha { get; set; }
    public Guid? ObjetivoAprendizajeId { get; set; }
    public string? NivelBloom { get; set; }
    public string? DescripcionInicio { get; set; }
    public string? DescripcionDesarrollo { get; set; }
    public string? DescripcionCierre { get; set; }
    public EstadoClase? Estado { get; set; }
    public List<Guid>? IndicadorEvaluacionIds { get; set; }
}

public class GenerarMaterialClaseRequest
{
    public TipoDocumento Tipo { get; set; } = TipoDocumento.Guia;
    public int CantidadItems { get; set; } = 5;
    public bool SoloSeleccionMultiple { get; set; }
}

/// <summary>Alias Prompt 2 / listado de planificaciones.</summary>
public class PlanificacionDto : PlanificacionResumenDto { }

public class PlanificacionResumenDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public string Asignatura { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public EstadoPlanificacion Estado { get; set; }
    public int CantidadClases { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class PlanificacionDetalleDto
{
    public Guid Id { get; set; }
    public Guid NivelId { get; set; }
    public Guid AsignaturaId { get; set; }
    public Guid UnidadId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public string Asignatura { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public EstadoPlanificacion Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<ClaseResumenDto> Clases { get; set; } = [];
}

public class ClaseResumenDto
{
    public Guid Id { get; set; }
    public Guid PlanificacionId { get; set; }
    public int Numero { get; set; }
    public DateOnly Fecha { get; set; }
    public Guid ObjetivoAprendizajeId { get; set; }
    public string ObjetivoCodigo { get; set; } = string.Empty;
    public string ObjetivoResumen { get; set; } = string.Empty;
    public string NivelBloom { get; set; } = string.Empty;
    public EstadoClase Estado { get; set; }
    public bool TieneEstructura { get; set; }
    /// <summary>Estado legible de la estructura IA (Inicio/Desarrollo/Cierre).</summary>
    public string EstructuraEstado { get; set; } = "Sin estructura";
    public ClassStructureUiStatus EstructuraUiStatus { get; set; } = ClassStructureUiStatus.None;
    public bool TieneGuia { get; set; }
    public bool TieneEjercicios { get; set; }
    public bool TienePrueba { get; set; }
}

/// <summary>Alias Prompt 2 para resumen de clase.</summary>
public class ClaseDto : ClaseResumenDto { }

public class ClaseDetalleDto
{
    public Guid Id { get; set; }
    public Guid PlanificacionId { get; set; }
    public int Numero { get; set; }
    public DateOnly Fecha { get; set; }
    public Guid ObjetivoAprendizajeId { get; set; }
    public string ObjetivoCodigo { get; set; } = string.Empty;
    public string ObjetivoDescripcion { get; set; } = string.Empty;
    public string NivelBloom { get; set; } = string.Empty;
    public string? DescripcionInicio { get; set; }
    public string? DescripcionDesarrollo { get; set; }
    public string? DescripcionCierre { get; set; }
    public EstadoClase Estado { get; set; }
    public List<Guid> IndicadorEvaluacionIds { get; set; } = [];
    public List<string> Indicadores { get; set; } = [];
    public List<DocumentoDto> Documentos { get; set; } = [];
    public Guid NivelId { get; set; }
    public Guid AsignaturaId { get; set; }
    public Guid UnidadId { get; set; }
    public string Nivel { get; set; } = string.Empty;
    public string Asignatura { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
}

public class EstructuraClaseDto
{
    public string Inicio { get; set; } = string.Empty;
    public string Desarrollo { get; set; } = string.Empty;
    public string Cierre { get; set; } = string.Empty;
    public string? VerboBloom { get; set; }
    public string? PropositoClase { get; set; }
}
