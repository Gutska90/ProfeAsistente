namespace ProfeAsistente.Api.Services.Export;

/// <summary>Factory de estilos tipográficos usados por <see cref="WordDocumentBuilder"/>.</summary>
public static class WordStyleFactory
{
    public static readonly string[] PreferredFonts = ["Aptos", "Calibri", "Arial"];

    public static string ResolveFont(WordTemplateSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.FontFamily))
            return settings.FontFamily;
        return settings.FallbackFonts.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f))
               ?? PreferredFonts[0];
    }
}
