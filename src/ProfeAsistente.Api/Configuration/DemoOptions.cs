namespace ProfeAsistente.Api.Configuration;

/// <summary>Datos demo / seed (solo Development).</summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    /// <summary>Habilita seed curricular/escolar y admin de prueba. Ignorado fuera de Development.</summary>
    public bool Enabled { get; set; }
}
