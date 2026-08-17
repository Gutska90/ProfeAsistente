using System.ComponentModel.DataAnnotations;

namespace AppEducativa.Api.Configuration;

public sealed class ExportOptions
{
    public const string SectionName = "Export";

    [Required]
    public string RootPath { get; set; } = "App_Data/Exports";

    [Range(1, 365)]
    public int KeepFilesForDays { get; set; } = 30;

    [Range(1, 500)]
    public int MaximumFileSizeMb { get; set; } = 50;

    public bool DeleteExpiredFiles { get; set; } = true;

    public bool UseBackgroundQueue { get; set; }

    [Range(10, 600)]
    public int SynchronousTimeoutSeconds { get; set; } = 90;

    public bool AllowOutdatedDocuments { get; set; }

    public bool RequireConfirmationForEmptyPlanning { get; set; } = true;

    public string DefaultFontFamily { get; set; } = "Aptos";

    public string TemplateSettingsPath { get; set; } = "Templates/Word/default-template-settings.json";
}
