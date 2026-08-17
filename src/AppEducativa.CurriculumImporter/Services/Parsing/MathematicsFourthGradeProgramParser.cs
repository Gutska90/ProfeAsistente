using System.Text.Json;
using System.Text.RegularExpressions;
using AppEducativa.CurriculumImporter.Models.Extraction;
using AppEducativa.CurriculumImporter.Models.Sources;
using AppEducativa.CurriculumImporter.Services.Normalization;
using AppEducativa.Shared.Dtos;
using ExtractedDocument = AppEducativa.CurriculumImporter.Models.Extraction.CurriculumExtractionResult;

namespace AppEducativa.CurriculumImporter.Services.Parsing;

public interface IProgramStudyParser
{
    Task<ExtractedCurriculumPackage> ParseAsync(
        CurriculumSourceDefinition source, ExtractedDocument extraction, CancellationToken cancellationToken = default);
}

/// <summary>
/// Parser específico del Programa de Estudio Matemática 4° Básico (vertical: una unidad).
/// Reglas cargadas desde Configuration/ParserProfiles/matematica-4-basico.json.
/// </summary>
public sealed class MathematicsFourthGradeProgramParser : IProgramStudyParser
{
    private readonly ICurriculumTextNormalizer _normalizer;
    private readonly string _profilePath;

    public MathematicsFourthGradeProgramParser(ICurriculumTextNormalizer normalizer, string? profilePath = null)
    {
        _normalizer = normalizer;
        _profilePath = profilePath ?? Path.Combine(AppContext.BaseDirectory, "Configuration", "ParserProfiles", "matematica-4-basico.json");
    }

    public async Task<ExtractedCurriculumPackage> ParseAsync(
        CurriculumSourceDefinition source, ExtractedDocument extraction, CancellationToken cancellationToken = default)
    {
        var profile = JsonSerializer.Deserialize<ParserProfile>(await File.ReadAllTextAsync(_profilePath, cancellationToken),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ParserProfile();

        var result = new ExtractedCurriculumPackage
        {
            SourceId = source.Id,
            RequiresManualReview = extraction.RequiresManualReview
        };

        var unitExpressions = Compile(profile.UnitHeadingPatterns);
        var oaExpressions = Compile(profile.LearningObjectivePatterns);
        var indicatorHeadings = Compile(profile.IndicatorHeadingPatterns);
        var skillHeadings = Compile(profile.SkillsHeadings);
        var attitudeHeadings = Compile(profile.AttitudesHeadings);
        var stopExpressions = Compile(profile.StopUnitPatterns);
        var hoursRegex = string.IsNullOrWhiteSpace(profile.HoursPattern)
            ? null
            : new Regex(profile.HoursPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var startPage = profile.PageRange?.Start ?? 1;
        var endPage = profile.PageRange?.End ?? int.MaxValue;
        var targetUnit = profile.TargetUnitNumber ?? 1;

        ExtractedUnitCandidate? currentUnit = null;
        ExtractedLearningObjectiveCandidate? currentOa = null;
        var inIndicators = false;
        string? listMode = null;
        var capturingUnit = false;

        foreach (var page in extraction.Pages.Where(p => p.PageNumber >= startPage && p.PageNumber <= endPage))
        {
            // El PDF a menudo pierde saltos; insertamos anclas antes de OA/Unidad.
            var pageText = SoftSplit(page.OriginalText);
            foreach (var rawLine in pageText.Split('\n'))
            {
                var line = _normalizer.Normalize(rawLine);
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (stopExpressions.Any(r => r.IsMatch(line)) && capturingUnit)
                    goto Finalize;

                var unitMatch = unitExpressions.Select(r => r.Match(line)).FirstOrDefault(m => m.Success);
                // Tras SoftSplit, "Unidad 1" y "PROPÓSITO..." pueden quedar en líneas separadas.
                if (unitMatch is null || !unitMatch.Success)
                {
                    var loose = Regex.Match(line, @"Unidad\s+(?<number>[1-4])(?!\d)\s*$", RegexOptions.IgnoreCase);
                    if (loose.Success) unitMatch = loose;
                }

                if (unitMatch?.Success == true &&
                    int.TryParse(unitMatch.Groups["number"].Value, out var number) &&
                    number == targetUnit)
                {
                    capturingUnit = true;
                    if (currentUnit is null)
                    {
                        currentUnit = new ExtractedUnitCandidate
                        {
                            Number = number,
                            Name = $"Unidad {number}",
                            Source = new SourceReference(page.PageNumber, page.PageNumber, Truncate(line, 180))
                        };
                        result.Units.Add(currentUnit);
                    }

                    currentOa = null;
                    inIndicators = false;
                    listMode = null;
                    continue;
                }

                if (!capturingUnit) continue;

                if (currentUnit is not null &&
                    (line.StartsWith("PROPÓSITO", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("PROPOSITO", StringComparison.OrdinalIgnoreCase) ||
                     line.Contains("PROPÓSITO", StringComparison.OrdinalIgnoreCase)))
                {
                    var purpose = ExtractAfter(line, "PROPÓSITO") ?? ExtractAfter(line, "PROPOSITO") ?? line;
                    purpose = purpose.Trim();
                    if (purpose.Length > 20)
                        currentUnit.Name = Truncate(purpose, 120);
                    continue;
                }

                if (hoursRegex is not null)
                {
                    var hoursMatch = hoursRegex.Match(line);
                    if (hoursMatch.Success && int.TryParse(hoursMatch.Groups["hours"].Value, out var hours) && currentUnit is not null)
                        currentUnit.SuggestedHours = hours;
                }

                if (indicatorHeadings.Any(r => r.IsMatch(line)))
                {
                    inIndicators = true;
                    listMode = null;
                    continue;
                }

                if (skillHeadings.Any(r => r.IsMatch(line)))
                {
                    listMode = "skills";
                    inIndicators = false;
                    continue;
                }

                if (attitudeHeadings.Any(r => r.IsMatch(line)))
                {
                    listMode = "attitudes";
                    inIndicators = false;
                    continue;
                }

                if (listMode is not null && IsBullet(rawLine))
                {
                    var item = CleanBullet(line)
                        .Replace("Resumen de la unidad", "", StringComparison.OrdinalIgnoreCase)
                        .Trim(' ', '.', '›');
                    if (item.Length >= 8)
                        (listMode == "skills" ? result.Skills : result.Attitudes).Add(item);
                    continue;
                }

                var oaMatch = oaExpressions.Select(r => r.Match(line)).FirstOrDefault(m => m.Success);
                if (oaMatch?.Success == true)
                {
                    var numberText = oaMatch.Groups["number"].Success
                        ? oaMatch.Groups["number"].Value
                        : oaMatch.Groups["code"].Value;
                    if (!int.TryParse(Regex.Replace(numberText, @"\D", ""), out var oaNumber))
                        continue;

                    // Evitar OA transversales de habilidades (OA a, OA e, OA l…) y códigos de evaluación OA_5 en ejemplos.
                    if (oaNumber is < 1 or > 27) continue;
                    if (line.Contains("Observaciones al docente", StringComparison.OrdinalIgnoreCase)) continue;
                    if (Regex.IsMatch(line, @"\bOA\s*[a-z]\b", RegexOptions.IgnoreCase) &&
                        !Regex.IsMatch(line, @"\bOA\s*_?\d", RegexOptions.IgnoreCase))
                        continue;

                    var code = $"{profile.NormalizeOaPrefix ?? "OA"} {oaNumber}";
                    var description = oaMatch.Groups["description"].Success
                        ? oaMatch.Groups["description"].Value.Trim()
                        : line[(oaMatch.Index + oaMatch.Length)..].Trim(" :-".ToCharArray());

                    // Ignorar menciones en visión global "(OA 1)" sin descripción propia.
                    if (description.Length < 20) continue;
                    if (description.StartsWith(')') || description.StartsWith('_')) continue;

                    currentOa = new ExtractedLearningObjectiveCandidate
                    {
                        Code = code,
                        Description = description,
                        Source = new SourceReference(page.PageNumber, page.PageNumber, Truncate(line, 220))
                    };
                    result.LearningObjectives.Add(currentOa);
                    if (currentUnit is not null && !currentUnit.LearningObjectiveCodes.Contains(code))
                        currentUnit.LearningObjectiveCodes.Add(code);
                    inIndicators = true; // en este programa los indicadores siguen al OA en la misma tabla
                    listMode = null;
                    continue;
                }

                if (currentOa is not null && IsBullet(rawLine))
                {
                    var bullet = CleanBullet(line);
                    if (bullet.Length < 8) continue;

                    // Indicadores suelen iniciar con verbo en mayúscula; viñetas del OA en minúscula/gerundio.
                    if (inIndicators && LooksLikeIndicator(bullet))
                    {
                        result.Indicators.Add(new ExtractedIndicatorCandidate
                        {
                            LearningObjectiveCode = currentOa.Code,
                            Description = bullet,
                            Source = new SourceReference(page.PageNumber, page.PageNumber, Truncate(bullet, 220))
                        });
                    }
                    else if (!inIndicators || !LooksLikeIndicator(bullet))
                    {
                        currentOa.Description = $"{currentOa.Description} › {bullet}".Trim();
                        currentOa.Source = currentOa.Source with { PageEnd = page.PageNumber };
                    }

                    continue;
                }

                if (currentOa is not null && !IsNoiseLine(line))
                {
                    // Continuar descripción multilínea del OA hasta un encabezado fuerte.
                    if (line.Contains("OBJETIVOS DE APRENDIZAJE", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("Ejemplos de actividades", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("Observaciones al docente", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!LooksLikeIndicator(line))
                    {
                        currentOa.Description = $"{currentOa.Description} {line}".Trim();
                        currentOa.Source = currentOa.Source with { PageEnd = page.PageNumber };
                    }
                }
            }
        }

    Finalize:
        DeduplicateObjectives(result);
        DeduplicateIndicators(result);
        DeduplicateList(result.Skills);
        DeduplicateList(result.Attitudes);

        if (result.Units.Count == 0 || result.LearningObjectives.Count == 0)
            result.RequiresManualReview = true;

        return result;
    }

    private static void DeduplicateObjectives(ExtractedCurriculumPackage package)
    {
        var best = new Dictionary<string, ExtractedLearningObjectiveCandidate>(StringComparer.OrdinalIgnoreCase);
        foreach (var oa in package.LearningObjectives)
        {
            if (string.IsNullOrWhiteSpace(oa.Code)) continue;
            if (!best.TryGetValue(oa.Code, out var existing) || oa.Description.Length > existing.Description.Length)
                best[oa.Code] = oa;
        }

        package.LearningObjectives.Clear();
        package.LearningObjectives.AddRange(best.Values.OrderBy(o => ExtractOaNumber(o.Code)));
        foreach (var unit in package.Units)
            unit.LearningObjectiveCodes = package.LearningObjectives.Select(o => o.Code).Distinct().ToList();
    }

    private static void DeduplicateIndicators(ExtractedCurriculumPackage package)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unique = new List<ExtractedIndicatorCandidate>();
        foreach (var indicator in package.Indicators)
        {
            var key = $"{indicator.LearningObjectiveCode}|{NormalizeKey(indicator.Description)}";
            if (!seen.Add(key)) continue;
            unique.Add(indicator);
        }

        package.Indicators.Clear();
        package.Indicators.AddRange(unique);
    }

    private static void DeduplicateList(List<string> items)
    {
        var unique = items
            .Select(i => i.Trim())
            .Where(i => i.Length >= 8)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        items.Clear();
        items.AddRange(unique);
    }

    private static string SoftSplit(string text)
    {
        // Inserta saltos antes de anclas frecuentes del programa.
        var soft = Regex.Replace(text, @"(?<!^)(?=Unidad\s+[1-4](?!\d))", "\n", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        soft = Regex.Replace(soft, @"(?<!^)(?=(?<![A-Za-z_])OA\s*_?\d{1,2}\s*[A-ZÁÉÍÓÚÑ])", "\n", RegexOptions.IgnoreCase);
        soft = Regex.Replace(soft, @"›", "\n›");
        soft = Regex.Replace(soft, @"(?<!^)(?=INDICADORES?\s+DE\s+EVALUACI)", "\n", RegexOptions.IgnoreCase);
        soft = Regex.Replace(soft, @"(?<!^)(?=HABILIDADES)", "\n", RegexOptions.IgnoreCase);
        soft = Regex.Replace(soft, @"(?<!^)(?=ACTITUDES)", "\n", RegexOptions.IgnoreCase);
        soft = Regex.Replace(soft, @"(?<!^)(?=PROP[ÓO]SITO)", "\n", RegexOptions.IgnoreCase);
        return soft;
    }

    private static bool IsBullet(string raw) =>
        raw.TrimStart().StartsWith('›') || raw.TrimStart().StartsWith('•') || raw.TrimStart().StartsWith('-');

    private static string CleanBullet(string line) =>
        line.TrimStart('›', '•', '-', ' ').Trim();

    private static bool LooksLikeIndicator(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 12) return false;
        var first = text.Trim()[0];
        return char.IsUpper(first) || first is 'Á' or 'É' or 'Í' or 'Ó' or 'Ú' or 'Ñ';
    }

    private static bool IsNoiseLine(string line) =>
        line.Contains("Programa de Estudio", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Matemática", StringComparison.OrdinalIgnoreCase) && line.Length < 40 ||
        Regex.IsMatch(line, @"^Página\s+\d+", RegexOptions.IgnoreCase);

    private static string? ExtractAfter(string line, string marker)
    {
        var idx = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        return line[(idx + marker.Length)..].Trim();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    private static string NormalizeKey(string value) =>
        Regex.Replace(value.Trim().ToLowerInvariant(), @"\s+", " ");

    private static int ExtractOaNumber(string code)
    {
        var m = Regex.Match(code, @"\d+");
        return m.Success && int.TryParse(m.Value, out var n) ? n : int.MaxValue;
    }

    private static Regex[] Compile(IEnumerable<string> patterns) =>
        patterns.Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled)).ToArray();

    private sealed class ParserProfile
    {
        public int? TargetUnitNumber { get; set; }
        public PageRangeConfig? PageRange { get; set; }
        public List<string> UnitHeadingPatterns { get; set; } = [];
        public List<string> LearningObjectivePatterns { get; set; } = [];
        public List<string> IndicatorHeadingPatterns { get; set; } = [];
        public List<string> SkillsHeadings { get; set; } = [];
        public List<string> AttitudesHeadings { get; set; } = [];
        public List<string> StopUnitPatterns { get; set; } = [];
        public string? HoursPattern { get; set; }
        public string? NormalizeOaPrefix { get; set; }
    }

    private sealed class PageRangeConfig
    {
        public int Start { get; set; }
        public int End { get; set; }
    }
}

public static class ExtractionPackageAdapters
{
    public static AppEducativa.Shared.Dtos.CurriculumExtractionResult ToSharedDto(
        this ExtractedCurriculumPackage package, CurriculumSourceDefinition source)
    {
        return new AppEducativa.Shared.Dtos.CurriculumExtractionResult
        {
            SourceTitle = source.Nombre,
            SourceUrl = source.Url,
            DocumentType = source.TipoFuente,
            ConfianzaExtraccion = package.RequiresManualReview ? 0.45 : 0.75,
            Level = new LevelExtractDto { Code = source.NivelCodigo ?? string.Empty, Name = "4° básico" },
            Subject = new SubjectExtractDto { Code = source.AsignaturaCodigo ?? string.Empty, Name = "Matemática" },
            Units = package.Units.Select(u => new UnitExtractDto
            {
                Number = u.Number,
                Name = u.Name,
                LearningObjectiveCodes = u.LearningObjectiveCodes
            }).ToList(),
            LearningObjectives = package.LearningObjectives.Select(o => new LearningObjectiveExtractDto
            {
                Code = o.Code,
                Description = o.Description
            }).ToList(),
            EvaluationIndicators = package.Indicators.Select((i, index) => new EvaluationIndicatorExtractDto
            {
                LearningObjectiveCode = i.LearningObjectiveCode,
                Description = i.Description,
                Orden = index + 1
            }).ToList(),
            Skills = package.Skills.Select(s => new SkillExtractDto { Description = s }).ToList(),
            Attitudes = package.Attitudes.Select(a => new AttitudeExtractDto { Description = a }).ToList()
        };
    }
}
