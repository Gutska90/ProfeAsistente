namespace AppEducativa.Shared.Dtos;

public class NivelDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Ciclo { get; set; } = string.Empty;
    public int Orden { get; set; }
}

/// <summary>
/// En la API pública, Id corresponde a NivelAsignatura (vínculo nivel↔asignatura aprobado).
/// </summary>
public class AsignaturaDto
{
    public Guid Id { get; set; }
    public Guid NivelId { get; set; }
    public Guid AsignaturaCatalogoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
}

public class EjeDto
{
    public Guid Id { get; set; }
    public Guid NivelAsignaturaId { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public class UnidadDto
{
    public Guid Id { get; set; }
    /// <summary>NivelAsignaturaId (compat: nombre histórico AsignaturaId en clientes).</summary>
    public Guid AsignaturaId { get; set; }
    public int Numero { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string NombreMostrar => Numero > 0 ? $"{Numero}. {Nombre}" : Nombre;
}

public class ObjetivoAprendizajeDto
{
    public Guid Id { get; set; }
    public Guid? UnidadId { get; set; }
    public Guid NivelAsignaturaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public bool EsContenidoOficial { get; set; }
    public string FuenteTipo { get; set; } = string.Empty;
    public CurriculumFuenteDto? Fuente { get; set; }
}

/// <summary>Public provenance metadata; local storage paths are intentionally excluded.</summary>
public class CurriculumFuenteDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime FechaDescarga { get; set; }
    public int? PaginaInicio { get; set; }
    public int? PaginaFin { get; set; }
    public string HashDocumento { get; set; } = string.Empty;
}

public class IndicadorEvaluacionDto
{
    public Guid Id { get; set; }
    public Guid ObjetivoAprendizajeId { get; set; }
    public string? Codigo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool EsSugerido { get; set; }
    public int Orden { get; set; }
}

public class ObjetivoAprendizajeDetalleDto : ObjetivoAprendizajeDto
{
    public string UnidadNombre { get; set; } = string.Empty;
    public string AsignaturaNombre { get; set; } = string.Empty;
    public string NivelNombre { get; set; } = string.Empty;
    public string? EjeNombre { get; set; }
    public string VersionCurricular { get; set; } = string.Empty;
    public List<string> Indicadores { get; set; } = [];
    public List<string> Habilidades { get; set; } = [];
    public List<string> Actitudes { get; set; } = [];
}

public class OatDto
{
    public Guid Id { get; set; }
    public Guid? NivelId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? Dimension { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public class CurriculumVersionDto
{
    public Guid? ReleaseId { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? Name { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? UltimaAprobacionUtc { get; set; }
    public int ObjetivosVigentes { get; set; }
    public string? ContentHash { get; set; }
    public int Sources { get; set; }
}

public class CurriculumAdminSourceDto
{
    public Guid Id { get; set; }
    public string? ExternalId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Dominio { get; set; } = string.Empty;
    public string TipoFuente { get; set; } = string.Empty;
    public string Formato { get; set; } = string.Empty;
    public string? NivelEsperado { get; set; }
    public string? AsignaturaEsperada { get; set; }
    public bool Activo { get; set; }
}

public class CurriculumAdminBatchDto
{
    public Guid Id { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaTermino { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int CantidadUnidades { get; set; }
    public int CantidadOA { get; set; }
    public int CantidadIndicadores { get; set; }
    public int CantidadRegistrosNuevos { get; set; }
    public int CantidadActualizados { get; set; }
    public int CantidadSinCambios { get; set; }
    public int CantidadAdvertencias { get; set; }
    public int CantidadErrores { get; set; }
    public string? Mensaje { get; set; }
    public string? SourceExternalId { get; set; }
}

public class CurriculumImportPreviewDto
{
    public Guid BatchId { get; set; }
    public string? SourceExternalId { get; set; }
    public string Status { get; set; } = string.Empty;
    public double ConfianzaPromedio { get; set; }
    public List<CurriculumUnitPreviewDto> Units { get; set; } = [];
    public List<CurriculumObjectivePreviewDto> Objectives { get; set; } = [];
    public List<CurriculumIndicatorPreviewDto> Indicators { get; set; } = [];
    public List<string> Skills { get; set; } = [];
    public List<string> Attitudes { get; set; } = [];
}

public class CurriculumUnitPreviewDto
{
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> LearningObjectiveCodes { get; set; } = [];
}

public class CurriculumObjectivePreviewDto
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AxisName { get; set; }
}

public class CurriculumIndicatorPreviewDto
{
    public string LearningObjectiveCode { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ValidationIssueDto
{
    public string Severity { get; set; } = "Warning";
    public bool Blocking { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ImportSummaryDto
{
    public Guid BatchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Units { get; set; }
    public int Objectives { get; set; }
    public int Indicators { get; set; }
    public int Skills { get; set; }
    public int Attitudes { get; set; }
    public int Warnings { get; set; }
    public int Errors { get; set; }
}
