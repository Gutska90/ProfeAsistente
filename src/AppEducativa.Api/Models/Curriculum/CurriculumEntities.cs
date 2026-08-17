using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Models.Curriculum;

public class Nivel
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Ciclo { get; set; } = string.Empty;
    public int Orden { get; set; }

    public ICollection<NivelAsignatura> NivelAsignaturas { get; set; } = [];
    public ICollection<ObjetivoAprendizajeTransversal> Oats { get; set; } = [];
    public ICollection<Actitud> Actitudes { get; set; } = [];
}

public class Asignatura
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    public ICollection<NivelAsignatura> NivelAsignaturas { get; set; } = [];
}

public class NivelAsignatura
{
    public Guid Id { get; set; }
    public Guid NivelId { get; set; }
    public Guid AsignaturaId { get; set; }
    public string NombreEnNivel { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;
    public EstadoRevision EstadoRevision { get; set; } = EstadoRevision.Pendiente;
    public bool Vigente { get; set; } = true;
    public double ConfianzaExtraccion { get; set; } = 1;
    public string FuenteTipo { get; set; } = "Desconocida";
    public bool EsContenidoOficial { get; set; }

    public Nivel? Nivel { get; set; }
    public Asignatura? Asignatura { get; set; }
    public ICollection<EjeCurricular> Ejes { get; set; } = [];
    public ICollection<Unidad> Unidades { get; set; } = [];
    public ICollection<ObjetivoAprendizaje> Objetivos { get; set; } = [];
    public ICollection<Habilidad> Habilidades { get; set; } = [];
}

public class EjeCurricular
{
    public Guid Id { get; set; }
    public Guid NivelAsignaturaId { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public EstadoRevision EstadoRevision { get; set; } = EstadoRevision.Pendiente;
    public bool Vigente { get; set; } = true;

    public NivelAsignatura? NivelAsignatura { get; set; }
    public ICollection<ObjetivoAprendizaje> Objetivos { get; set; } = [];
}

public class Unidad
{
    public Guid Id { get; set; }
    public Guid NivelAsignaturaId { get; set; }
    public int Numero { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? HorasPedagogicasSugeridas { get; set; }
    public int Orden { get; set; }
    public EstadoRevision EstadoRevision { get; set; } = EstadoRevision.Pendiente;
    public bool Vigente { get; set; } = true;
    public string FuenteTipo { get; set; } = "Desconocida";
    public bool EsContenidoOficial { get; set; }
    public CurriculumPublicationStatus PublicationStatus { get; set; } = CurriculumPublicationStatus.Published;
    public Guid? CurriculumReleaseId { get; set; }

    public NivelAsignatura? NivelAsignatura { get; set; }
    public ICollection<UnidadObjetivoAprendizaje> Objetivos { get; set; } = [];
}

public class ObjetivoAprendizaje
{
    public Guid Id { get; set; }
    public Guid NivelAsignaturaId { get; set; }
    public Guid? EjeCurricularId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int? Numero { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public TipoObjetivoAprendizaje Tipo { get; set; } = TipoObjetivoAprendizaje.Basal;
    public bool EsObligatorio { get; set; } = true;
    public bool Vigente { get; set; } = true;
    public string Version { get; set; } = "1";
    public EstadoRevision EstadoRevision { get; set; } = EstadoRevision.Pendiente;
    public double ConfianzaExtraccion { get; set; } = 1;
    public string? ObservacionRevision { get; set; }
    public string FuenteTipo { get; set; } = "Desconocida";
    public bool EsContenidoOficial { get; set; }
    public CurriculumPublicationStatus PublicationStatus { get; set; } = CurriculumPublicationStatus.Published;
    public Guid? CurriculumReleaseId { get; set; }

    public NivelAsignatura? NivelAsignatura { get; set; }
    public EjeCurricular? EjeCurricular { get; set; }
    public ICollection<IndicadorEvaluacion> Indicadores { get; set; } = [];
    public ICollection<UnidadObjetivoAprendizaje> Unidades { get; set; } = [];
}

public class UnidadObjetivoAprendizaje
{
    public Guid UnidadId { get; set; }
    public Guid ObjetivoAprendizajeId { get; set; }
    public int Orden { get; set; }
    public bool EsPrincipal { get; set; } = true;

    public Unidad? Unidad { get; set; }
    public ObjetivoAprendizaje? ObjetivoAprendizaje { get; set; }
}

public class IndicadorEvaluacion
{
    public Guid Id { get; set; }
    public Guid ObjetivoAprendizajeId { get; set; }
    public Guid? UnidadId { get; set; }
    public string? Codigo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool EsSugerido { get; set; } = true;
    public int Orden { get; set; }
    public bool Vigente { get; set; } = true;
    public EstadoRevision EstadoRevision { get; set; } = EstadoRevision.Pendiente;

    public ObjetivoAprendizaje? ObjetivoAprendizaje { get; set; }
}

public class Habilidad
{
    public Guid Id { get; set; }
    public Guid NivelAsignaturaId { get; set; }
    public Guid? EjeCurricularId { get; set; }
    public string? Codigo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool Vigente { get; set; } = true;
    public EstadoRevision EstadoRevision { get; set; } = EstadoRevision.Pendiente;

    public NivelAsignatura? NivelAsignatura { get; set; }
}

public class Actitud
{
    public Guid Id { get; set; }
    public Guid? NivelAsignaturaId { get; set; }
    public Guid? NivelId { get; set; }
    public string? Codigo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool Vigente { get; set; } = true;
    public EstadoRevision EstadoRevision { get; set; } = EstadoRevision.Pendiente;

    public Nivel? Nivel { get; set; }
}

public class ObjetivoAprendizajeTransversal
{
    public Guid Id { get; set; }
    public Guid? NivelId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? Dimension { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool Vigente { get; set; } = true;
    public string Version { get; set; } = "1";
    public EstadoRevision EstadoRevision { get; set; } = EstadoRevision.Pendiente;

    public Nivel? Nivel { get; set; }
}

/// <summary>Legacy join kept for Documento historial; new material uses Clase.</summary>
public class DocumentoObjetivoAprendizaje
{
    public Guid DocumentoId { get; set; }
    public Guid ObjetivoAprendizajeId { get; set; }

    public Models.Documento? Documento { get; set; }
    public ObjetivoAprendizaje? ObjetivoAprendizaje { get; set; }
}
