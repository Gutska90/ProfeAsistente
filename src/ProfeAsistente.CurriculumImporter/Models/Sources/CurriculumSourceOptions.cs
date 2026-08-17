namespace ProfeAsistente.CurriculumImporter.Models.Sources;

public sealed class CurriculumSourceOptions
{
    public string ConfigurationPath { get; set; } = Path.Combine("Configuration", "curriculum-sources.json");
}
