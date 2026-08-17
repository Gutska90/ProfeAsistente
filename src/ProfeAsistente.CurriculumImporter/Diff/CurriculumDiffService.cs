using System.Text;
using System.Text.RegularExpressions;
using ProfeAsistente.CurriculumImporter.Abstractions;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.CurriculumImporter.Diff;

public class CurriculumDiffService : ICurriculumDiffService
{
    private static readonly Regex WordSplitter = new(@"(\s+|[^\w\sáéíóúüñÁÉÍÓÚÜÑ]+)", RegexOptions.Compiled);

    public CurriculumDiffResult Compare(CurriculumExtractionResult extraction, CurriculumExtractionResult? currentPublished)
    {
        var result = new CurriculumDiffResult();
        currentPublished ??= new CurriculumExtractionResult();

        var oldOas = currentPublished.LearningObjectives.ToDictionary(o => o.Code, o => o, StringComparer.OrdinalIgnoreCase);
        var newOas = extraction.LearningObjectives.ToDictionary(o => o.Code, o => o, StringComparer.OrdinalIgnoreCase);

        foreach (var (code, oa) in newOas)
        {
            if (!oldOas.TryGetValue(code, out var prev))
            {
                result.Items.Add(new CurriculumDiffItem
                {
                    Tipo = TipoCambioCurricular.Nuevo,
                    Entidad = "ObjetivoAprendizaje",
                    Clave = code,
                    ValorNuevo = oa.Description,
                    Observacion = TextChangeSignificance.Critical.ToString()
                });
            }
            else
            {
                var significance = ClassifySignificance(prev.Description, oa.Description);
                if (significance == TextChangeSignificance.None)
                {
                    result.Items.Add(new CurriculumDiffItem
                    {
                        Tipo = TipoCambioCurricular.SinCambios,
                        Entidad = "ObjetivoAprendizaje",
                        Clave = code
                    });
                }
                else
                {
                    result.Items.Add(new CurriculumDiffItem
                    {
                        Tipo = TipoCambioCurricular.Modificado,
                        Entidad = "ObjetivoAprendizaje",
                        Clave = code,
                        ValorAnterior = prev.Description,
                        ValorNuevo = oa.Description,
                        Observacion = $"{significance}: {DescribeWordDiff(prev.Description, oa.Description)}"
                    });
                }
            }
        }

        foreach (var (code, prev) in oldOas)
        {
            if (!newOas.ContainsKey(code))
            {
                result.Items.Add(new CurriculumDiffItem
                {
                    Tipo = TipoCambioCurricular.PosiblementeEliminado,
                    Entidad = "ObjetivoAprendizaje",
                    Clave = code,
                    ValorAnterior = prev.Description,
                    Observacion = "No aparece en la nueva extracción; al aprobar se marcará no vigente."
                });
            }
        }

        return result;
    }

    public static TextChangeSignificance ClassifySignificance(string? oldValue, string? newValue)
    {
        var oldText = oldValue ?? string.Empty;
        var newText = newValue ?? string.Empty;
        if (string.Equals(oldText, newText, StringComparison.Ordinal))
            return TextChangeSignificance.None;
        if (string.Equals(NormalizeWhitespace(oldText), NormalizeWhitespace(newText), StringComparison.Ordinal))
            return TextChangeSignificance.FormattingOnly;

        var oldCore = StripPunctuation(NormalizeWhitespace(oldText)).ToLowerInvariant();
        var newCore = StripPunctuation(NormalizeWhitespace(newText)).ToLowerInvariant();
        if (string.Equals(oldCore, newCore, StringComparison.Ordinal))
            return TextChangeSignificance.Minor;

        var negations = new[] { " no ", " sin ", " nunca ", " jamás " };
        foreach (var n in negations)
        {
            var oldHas = (" " + oldCore + " ").Contains(n, StringComparison.Ordinal);
            var newHas = (" " + newCore + " ").Contains(n, StringComparison.Ordinal);
            if (oldHas != newHas) return TextChangeSignificance.Critical;
        }

        if (Regex.IsMatch(oldText.Trim(), @"^(OA|IE)\s*\d", RegexOptions.IgnoreCase)
            || Regex.IsMatch(newText.Trim(), @"^(OA|IE)\s*\d", RegexOptions.IgnoreCase))
            return TextChangeSignificance.Critical;

        return TextChangeSignificance.Relevant;
    }

    public static IReadOnlyList<(string Type, string Text)> DiffWords(string? oldValue, string? newValue)
    {
        var a = Tokenize(oldValue);
        var b = Tokenize(newValue);
        var lcs = BuildLcs(a, b);
        var result = new List<(string Type, string Text)>();
        var i = 0;
        var j = 0;
        var k = 0;
        while (i < a.Count || j < b.Count)
        {
            if (k < lcs.Count && i < a.Count && a[i] == lcs[k] && j < b.Count && b[j] == lcs[k])
            {
                Append(result, "Unchanged", a[i]);
                i++; j++; k++;
            }
            else if (i < a.Count && (k >= lcs.Count || a[i] != lcs[k]))
            {
                Append(result, "Removed", a[i]);
                i++;
            }
            else if (j < b.Count)
            {
                Append(result, "Added", b[j]);
                j++;
            }
            else break;
        }

        return result;
    }

    private static string DescribeWordDiff(string? oldValue, string? newValue)
    {
        var parts = DiffWords(oldValue, newValue)
            .Where(p => p.Type is "Added" or "Removed")
            .Select(p => $"{p.Type}:{p.Text.Trim()}")
            .Take(6);
        return string.Join("; ", parts);
    }

    private static List<string> Tokenize(string? text) =>
        string.IsNullOrEmpty(text)
            ? []
            : WordSplitter.Split(text).Where(s => s.Length > 0).ToList();

    private static List<string> BuildLcs(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        var n = a.Count;
        var m = b.Count;
        var dp = new int[n + 1, m + 1];
        for (var i = n - 1; i >= 0; i--)
        for (var j = m - 1; j >= 0; j--)
            dp[i, j] = a[i] == b[j] ? dp[i + 1, j + 1] + 1 : Math.Max(dp[i + 1, j], dp[i, j + 1]);

        var result = new List<string>();
        var x = 0;
        var y = 0;
        while (x < n && y < m)
        {
            if (a[x] == b[y])
            {
                result.Add(a[x]);
                x++;
                y++;
            }
            else if (dp[x + 1, y] >= dp[x, y + 1]) x++;
            else y++;
        }

        return result;
    }

    private static void Append(List<(string Type, string Text)> list, string type, string text)
    {
        if (list.Count > 0 && list[^1].Type == type)
            list[^1] = (type, list[^1].Text + text);
        else
            list.Add((type, text));
    }

    private static string NormalizeWhitespace(string s) =>
        Regex.Replace(s.Replace("\r\n", "\n").Replace('\r', '\n'), @"\s+", " ").Trim();

    private static string StripPunctuation(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) || "áéíóúüñÁÉÍÓÚÜÑ".Contains(ch))
                sb.Append(ch);
        }

        return sb.ToString();
    }
}
