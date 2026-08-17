namespace ProfeAsistente.CurriculumImporter.Models.Sources;

public sealed class CurriculumSourceDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string DominioPermitido { get; init; } = string.Empty;
    public string TipoFuente { get; init; } = "ProgramaEstudio";
    public string Formato { get; init; } = "Pdf";
    public string? NivelCodigo { get; init; }
    public string? AsignaturaCodigo { get; init; }
    public bool Activo { get; init; } = true;
    public int IntervaloSolicitudesMs { get; init; }
}
