using System.Text.Json;
using AppEducativa.CurriculumImporter.Abstractions;
using AppEducativa.Shared.Dtos;

namespace AppEducativa.CurriculumImporter.Extractors;

public class ManualJsonCurriculumExtractor : ICurriculumExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(CurriculumSourceConfig source) =>
        source.Formato.Equals("Json", StringComparison.OrdinalIgnoreCase) ||
        source.Tipo.Equals("ManualJson", StringComparison.OrdinalIgnoreCase) ||
        source.Url.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

    public Task<CurriculumExtractionResult> ExtractAsync(
        CurriculumSourceConfig source,
        DownloadedSource downloadedSource,
        CancellationToken cancellationToken = default)
    {
        var json = System.Text.Encoding.UTF8.GetString(downloadedSource.Content);
        var envelope = JsonSerializer.Deserialize<ManualJsonEnvelope>(json, JsonOptions)
            ?? throw new InvalidOperationException("JSON curricular inválido.");

        var result = new CurriculumExtractionResult
        {
            SourceTitle = envelope.Source?.Title ?? source.Nombre,
            SourceUrl = envelope.Source?.Url ?? source.Url,
            DocumentType = envelope.Source?.DocumentType ?? "ProgramaEstudio",
            Version = envelope.Source?.Version ?? "1",
            ConfianzaExtraccion = 1,
            ExtractedText = json,
            Level = envelope.Level,
            Subject = envelope.Subject,
            Axes = envelope.Axes ?? [],
            Units = envelope.Units ?? [],
            LearningObjectives = envelope.LearningObjectives ?? [],
            EvaluationIndicators = envelope.EvaluationIndicators ?? [],
            Skills = envelope.Skills ?? [],
            Attitudes = envelope.Attitudes ?? [],
            TransversalObjectives = envelope.TransversalObjectives ?? []
        };

        return Task.FromResult(result);
    }

    private class ManualJsonEnvelope
    {
        public SourceMeta? Source { get; set; }
        public LevelExtractDto? Level { get; set; }
        public SubjectExtractDto? Subject { get; set; }
        public List<AxisExtractDto>? Axes { get; set; }
        public List<UnitExtractDto>? Units { get; set; }
        public List<LearningObjectiveExtractDto>? LearningObjectives { get; set; }
        public List<EvaluationIndicatorExtractDto>? EvaluationIndicators { get; set; }
        public List<SkillExtractDto>? Skills { get; set; }
        public List<AttitudeExtractDto>? Attitudes { get; set; }
        public List<OatExtractDto>? TransversalObjectives { get; set; }
    }

    private class SourceMeta
    {
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? DocumentType { get; set; }
        public string? Version { get; set; }
    }
}
