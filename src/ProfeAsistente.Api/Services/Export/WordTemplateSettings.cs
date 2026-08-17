using System.Text.Json;

namespace ProfeAsistente.Api.Services.Export;

public sealed class WordTemplateSettings
{
    public string FontFamily { get; set; } = "Aptos";
    public List<string> FallbackFonts { get; set; } = ["Calibri", "Arial"];
    public int BodyFontSize { get; set; } = 11;
    public int TitleFontSize { get; set; } = 18;
    public int Heading1FontSize { get; set; } = 15;
    public int Heading2FontSize { get; set; } = 13;
    public int Heading3FontSize { get; set; } = 12;
    public string PageSize { get; set; } = "A4";
    public string Orientation { get; set; } = "Portrait";
    public double MarginTopCm { get; set; } = 2.0;
    public double MarginBottomCm { get; set; } = 2.0;
    public double MarginLeftCm { get; set; } = 2.0;
    public double MarginRightCm { get; set; } = 2.0;
    public bool ShowBorders { get; set; } = true;
    public bool ShowCurriculumFooter { get; set; } = true;

    public static WordTemplateSettings Load(string path)
    {
        if (!File.Exists(path))
            return new WordTemplateSettings();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<WordTemplateSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new WordTemplateSettings();
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(FontFamily))
            throw new WordExportException("Export template: fontFamily vacío.", "ExportTemplateInvalid", 500);
        if (BodyFontSize is < 8 or > 24)
            throw new WordExportException("Export template: bodyFontSize inválido.", "ExportTemplateInvalid", 500);
        if (MarginTopCm is < 0.5 or > 5)
            throw new WordExportException("Export template: márgenes inválidos.", "ExportTemplateInvalid", 500);
    }
}
