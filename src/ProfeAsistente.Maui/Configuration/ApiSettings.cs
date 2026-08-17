namespace ProfeAsistente.Maui.Configuration;

/// <summary>URL base de la API local (única fuente de configuración).</summary>
public sealed class ApiSettings
{
    /// <summary>Puerto preferido de desarrollo (también compatible con 5180 histórico).</summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:5180";

    public static ApiSettings Default { get; } = new();
}
