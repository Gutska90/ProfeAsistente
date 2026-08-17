namespace ProfeAsistente.Shared.Enums;

public enum TipoDocumento
{
    Guia = 0,
    Prueba = 1,
    Ejercicios = 2,
    /// <summary>Secuencia/planificación de unidad alineada al OA con progresión Bloom.</summary>
    PlanificacionUnidad = 3
}

/// <summary>Taxonomía de Bloom revisada (complejización de la habilidad).</summary>
public enum NivelBloom
{
    Recordar = 1,
    Comprender = 2,
    Aplicar = 3,
    Analizar = 4,
    Evaluar = 5,
    Crear = 6
}

public static class NivelBloomHelper
{
    public static readonly IReadOnlyList<string> Nombres =
    [
        "Recordar", "Comprender", "Aplicar", "Analizar", "Evaluar", "Crear"
    ];

    public static string? Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var v = valor.Trim();
        foreach (var n in Nombres)
        {
            if (v.Equals(n, StringComparison.OrdinalIgnoreCase))
                return n;
        }

        // sinónimos frecuentes
        return v.ToLowerInvariant() switch
        {
            "remember" or "conocimiento" or "memorizar" => "Recordar",
            "understand" or "entendimiento" or "comprension" or "comprensión" => "Comprender",
            "apply" or "aplicacion" or "aplicación" => "Aplicar",
            "analyze" or "analisis" or "análisis" => "Analizar",
            "evaluate" or "evaluacion" or "evaluación" => "Evaluar",
            "create" or "creacion" or "creación" or "sintesis" or "síntesis" => "Crear",
            _ => char.ToUpperInvariant(v[0]) + v[1..].ToLowerInvariant()
        };
    }

    public static int Orden(string? nivel)
    {
        var n = Normalizar(nivel);
        var idx = Nombres.ToList().FindIndex(x => x.Equals(n, StringComparison.OrdinalIgnoreCase));
        return idx < 0 ? 99 : idx + 1;
    }
}
