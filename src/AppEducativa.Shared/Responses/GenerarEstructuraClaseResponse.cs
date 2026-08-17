namespace AppEducativa.Shared.Responses;

public class GenerarEstructuraClaseResponse
{
    public Guid ClaseId { get; set; }
    public string Inicio { get; set; } = string.Empty;
    public string Desarrollo { get; set; } = string.Empty;
    public string Cierre { get; set; } = string.Empty;
    public string? VerboBloom { get; set; }
    public string? PropositoClase { get; set; }
}
