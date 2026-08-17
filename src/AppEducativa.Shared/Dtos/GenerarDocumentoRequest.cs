using AppEducativa.Shared.Enums;

namespace AppEducativa.Shared.Dtos;

public class GenerarDocumentoRequest
{
    public Guid NivelId { get; set; }
    public Guid AsignaturaId { get; set; }
    public Guid UnidadId { get; set; }
    public List<Guid> ObjetivoAprendizajeIds { get; set; } = [];

    /// <summary>Opcional: subtítulo o foco dentro de la unidad (no reemplaza al OA).</summary>
    public string? Tema { get; set; }

    public TipoDocumento Tipo { get; set; } = TipoDocumento.PlanificacionUnidad;

    /// <summary>Ítems (guía/prueba/ejercicios) o cantidad de sesiones si es planificación.</summary>
    public int CantidadItems { get; set; } = 6;

    /// <summary>Alias semántico para planificaciones (si > 0, se usa en lugar de CantidadItems).</summary>
    public int? CantidadSesiones { get; set; }

    public bool SoloSeleccionMultiple { get; set; }

    /// <summary>Si true (default en planificación), reparte por niveles Bloom de menor a mayor complejidad.</summary>
    public bool UsarTaxonomiaBloom { get; set; } = true;
}

public class ActualizarDocumentoRequest
{
    public string? ContenidoGenerado { get; set; }
    public string? Instrucciones { get; set; }
    public string? Tema { get; set; }
    public EstadoDocumento? Estado { get; set; }
    public List<ItemDto>? Items { get; set; }
    public List<SesionPlanificadaDto>? Sesiones { get; set; }
}

public class ExportarDocumentoRequest
{
    public string Formato { get; set; } = "docx";
    /// <summary>Si true, incluye clave de corrección (por defecto en Prueba).</summary>
    public bool? IncluirClave { get; set; }
}

public class DocumentoDto
{
    public Guid Id { get; set; }
    public TipoDocumento Tipo { get; set; }
    public Guid? ClaseId { get; set; }
    public Guid NivelId { get; set; }
    public Guid AsignaturaId { get; set; }
    public Guid? UnidadId { get; set; }
    public string Nivel { get; set; } = string.Empty;
    public string Asignatura { get; set; } = string.Empty;
    public string? Unidad { get; set; }
    public string Tema { get; set; } = string.Empty;
    public string? ObjetivoAprendizaje { get; set; }
    public string? Instrucciones { get; set; }
    public string ContenidoGenerado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public EstadoDocumento Estado { get; set; }
    public List<Guid> ObjetivoAprendizajeIds { get; set; } = [];
    public List<ItemDto> Items { get; set; } = [];
    public List<SesionPlanificadaDto> Sesiones { get; set; } = [];
}

public class SesionPlanificadaDto
{
    public Guid Id { get; set; }
    public Guid DocumentoId { get; set; }
    public int Numero { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Actividades { get; set; } = string.Empty;
    public string? NivelBloom { get; set; }
    public string? VerboBloom { get; set; }
    public Guid? ObjetivoAprendizajeId { get; set; }
    public string? IndicadorEvaluacion { get; set; }
    public string? CriterioLogro { get; set; }
    public int? MinutosEstimados { get; set; }
}

public class ItemDto
{
    public Guid Id { get; set; }
    public Guid DocumentoId { get; set; }
    public TipoItem Tipo { get; set; }
    public string Enunciado { get; set; } = string.Empty;
    public List<string> Alternativas { get; set; } = [];
    public string? RespuestaCorrecta { get; set; }
    public int Puntaje { get; set; } = 1;
    public int Orden { get; set; }
    public Guid? IndicadorEvaluacionId { get; set; }
    public string? IndicadorEvaluacion { get; set; }
    public string? NivelBloom { get; set; }
    public string? VerboBloom { get; set; }
}

public class GeminiContentDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Instrucciones { get; set; } = string.Empty;
    public string? PropositoUnidad { get; set; }
    public string? HabilidadFocal { get; set; }
    public List<GeminiItemDto> Items { get; set; } = [];
    public List<GeminiSesionDto> Sesiones { get; set; } = [];
}

public class GeminiSesionDto
{
    public int Numero { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Actividades { get; set; } = string.Empty;
    public string? NivelBloom { get; set; }
    public string? VerboBloom { get; set; }
    public string? IndicadorCodigoODescripcion { get; set; }
    public string? CriterioLogro { get; set; }
    public int? MinutosEstimados { get; set; }
}

public class GeminiItemDto
{
    public string Tipo { get; set; } = "desarrollo";
    public string Enunciado { get; set; } = string.Empty;
    public List<string> Alternativas { get; set; } = [];
    public string? RespuestaCorrecta { get; set; }
    public int Puntaje { get; set; } = 1;
    public string? IndicadorCodigoODescripcion { get; set; }
    public string? NivelBloom { get; set; }
    public string? VerboBloom { get; set; }
}
