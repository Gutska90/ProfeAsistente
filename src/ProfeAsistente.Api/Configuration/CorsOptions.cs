namespace ProfeAsistente.Api.Configuration;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Orígenes permitidos fuera de Development (p. ej. https://app.ejemplo.cl).
    /// En Development se permite cualquier origen salvo que se configure RestrictInDevelopment=true.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>Si true, también en Development solo se usan AllowedOrigins.</summary>
    public bool RestrictInDevelopment { get; set; }
}
