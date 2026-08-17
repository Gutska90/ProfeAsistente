using AppEducativa.Shared.Enums;

namespace AppEducativa.Maui.ViewModels;

public static class BloomChipHelper
{
    public static Color ColorFor(string? nivelBloom) => NivelBloomHelper.Normalizar(nivelBloom) switch
    {
        "Recordar" => Color.FromArgb("#5B8C5A"),
        "Comprender" => Color.FromArgb("#3D7A9E"),
        "Aplicar" => Color.FromArgb("#C47B2B"),
        "Analizar" => Color.FromArgb("#8B5E9A"),
        "Evaluar" => Color.FromArgb("#B04A4A"),
        "Crear" => Color.FromArgb("#2F6F6F"),
        _ => Color.FromArgb("#666666")
    };
}
