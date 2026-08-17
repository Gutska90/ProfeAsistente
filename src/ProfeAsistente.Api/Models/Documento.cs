using ProfeAsistente.Api.Models.Curriculum;
using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Models;

public class Documento
{
    public Guid Id { get; set; }
    public TipoDocumento Tipo { get; set; }

    /// <summary>Material ligado a una clase del planificador (nullable para historial legacy).</summary>
    public Guid? ClaseId { get; set; }

    public Guid NivelId { get; set; }
    public Guid AsignaturaId { get; set; }
    public Guid? UnidadId { get; set; }

    /// <summary>Denormalizado para exportación e historial (snapshot al generar).</summary>
    public string Nivel { get; set; } = string.Empty;
    public string Asignatura { get; set; } = string.Empty;
    public string? Unidad { get; set; }
    public string Tema { get; set; } = string.Empty;
    public string? ObjetivoAprendizaje { get; set; }

    public string ContenidoGenerado { get; set; } = string.Empty;
    public string? Instrucciones { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Borrador;

    public Clase? Clase { get; set; }
    public Nivel? NivelNav { get; set; }
    public Asignatura? AsignaturaNav { get; set; }
    public ICollection<DocumentoObjetivoAprendizaje> ObjetivosSeleccionados { get; set; } = [];
    public ICollection<Item> Items { get; set; } = [];
    public ICollection<SesionPlanificada> Sesiones { get; set; } = [];
}

/// <summary>
/// Sesión/clase dentro de una planificación de unidad.
/// El NivelBloom debe subir sesión a sesión respecto de la anterior.
/// </summary>
public class SesionPlanificada
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

    public Documento? Documento { get; set; }
    public ObjetivoAprendizaje? ObjetivoAprendizaje { get; set; }
}

public class Item
{
    public Guid Id { get; set; }
    public Guid DocumentoId { get; set; }
    public TipoItem Tipo { get; set; }
    public string Enunciado { get; set; } = string.Empty;
    public string AlternativasJson { get; set; } = "[]";
    public string? RespuestaCorrecta { get; set; }
    public int Puntaje { get; set; } = 1;
    public int Orden { get; set; }
    public Guid? IndicadorEvaluacionId { get; set; }

    /// <summary>Nivel Bloom: Recordar, Comprender, Aplicar, Analizar, Evaluar, Crear.</summary>
    public string? NivelBloom { get; set; }
    public string? VerboBloom { get; set; }

    public Documento? Documento { get; set; }
    public IndicadorEvaluacion? IndicadorEvaluacion { get; set; }
}
