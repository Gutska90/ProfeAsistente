using System.Text;
using System.Text.RegularExpressions;
using AppEducativa.Api.Models.AI.Responses;

namespace AppEducativa.Api.Services.AI.DocumentGeneration;

public interface IEducationalItemSimilarityService
{
    IReadOnlyList<string> DetectDuplicates(IReadOnlyList<GeneratedEducationalItem> items);
}

public sealed class EducationalItemSimilarityService : IEducationalItemSimilarityService
{
    private static readonly Regex NonWord = new(@"[^\p{L}\p{N}\s]+", RegexOptions.Compiled);

    public IReadOnlyList<string> DetectDuplicates(IReadOnlyList<GeneratedEducationalItem> items)
    {
        var warnings = new List<string>();
        for (var i = 0; i < items.Count; i++)
        {
            var a = Normalize(items[i].Statement);
            if (string.IsNullOrWhiteSpace(a)) continue;
            for (var j = i + 1; j < items.Count; j++)
            {
                var b = Normalize(items[j].Statement);
                if (string.IsNullOrWhiteSpace(b)) continue;
                if (a == b)
                {
                    warnings.Add($"Ítems {items[i].Order} y {items[j].Order} tienen enunciados idénticos.");
                    continue;
                }

                var sim = Jaccard(a, b);
                if (sim >= 0.85)
                    warnings.Add($"Ítems {items[i].Order} y {items[j].Order} son demasiado similares ({sim:P0}).");
            }

            var optionTexts = (items[i].Options ?? [])
                .Select(o => Normalize(o.Text))
                .Where(t => t.Length > 0)
                .ToList();
            if (optionTexts.Count != optionTexts.Distinct(StringComparer.Ordinal).Count())
                warnings.Add($"Ítem {items[i].Order}: opciones repetidas.");
        }

        return warnings;
    }

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var cleaned = NonWord.Replace(text.Trim().ToLowerInvariant(), " ");
        return Regex.Replace(cleaned, @"\s+", " ").Trim();
    }

    private static double Jaccard(string a, string b)
    {
        var setA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var setB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        if (setA.Count == 0 || setB.Count == 0) return 0;
        var inter = setA.Intersect(setB).Count();
        var union = setA.Union(setB).Count();
        return union == 0 ? 0 : (double)inter / union;
    }
}
