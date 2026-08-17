using System.Text;
using System.Text.RegularExpressions;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Services.Curriculum;

public static class FieldDiffHelper
{
    private static readonly Regex WordSplitter = new(@"(\s+|[^\w\sáéíóúüñÁÉÍÓÚÜÑ]+)", RegexOptions.Compiled);

    public static List<TextSegmentDiffDto> DiffWords(string? oldValue, string? newValue)
    {
        var a = Tokenize(oldValue);
        var b = Tokenize(newValue);
        var lcs = BuildLcs(a, b);
        var result = new List<TextSegmentDiffDto>();
        var i = 0;
        var j = 0;
        var k = 0;
        while (i < a.Count || j < b.Count)
        {
            if (k < lcs.Count && i < a.Count && a[i] == lcs[k] && j < b.Count && b[j] == lcs[k])
            {
                Append(result, "Unchanged", a[i]);
                i++;
                j++;
                k++;
            }
            else if (k < lcs.Count && i < a.Count && a[i] != lcs[k])
            {
                Append(result, "Removed", a[i]);
                i++;
            }
            else if (k < lcs.Count && j < b.Count && b[j] != lcs[k])
            {
                Append(result, "Added", b[j]);
                j++;
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

        if (LooksCritical(oldText, newText, oldCore, newCore))
            return TextChangeSignificance.Critical;

        return TextChangeSignificance.Relevant;
    }

    public static FieldDiffDto CompareField(string field, string? oldValue, string? newValue)
    {
        var significance = ClassifySignificance(oldValue, newValue);
        return new FieldDiffDto
        {
            Field = field,
            OldValue = oldValue,
            NewValue = newValue,
            Significance = significance.ToString(),
            Difference = significance == TextChangeSignificance.None
                ? []
                : DiffWords(oldValue, newValue)
        };
    }

    private static bool LooksCritical(string oldText, string newText, string oldCore, string newCore)
    {
        if (LooksLikeCode(oldText) || LooksLikeCode(newText))
            return !string.Equals(oldCore, newCore, StringComparison.Ordinal);

        var negations = new[] { " no ", " sin ", " nunca ", " jamás ", "nunca ", "no " };
        foreach (var n in negations)
        {
            var oldHas = (" " + oldCore + " ").Contains(n, StringComparison.Ordinal);
            var newHas = (" " + newCore + " ").Contains(n, StringComparison.Ordinal);
            if (oldHas != newHas) return true;
        }

        return false;
    }

    private static bool LooksLikeCode(string text)
    {
        var t = text.Trim();
        return Regex.IsMatch(t, @"^(OA|IE|H|A|EJE)\s*\d", RegexOptions.IgnoreCase)
               || Regex.IsMatch(t, @"^[A-Z]{1,4}\d");
    }

    private static List<string> Tokenize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return [];
        return WordSplitter.Split(text).Where(s => s.Length > 0).ToList();
    }

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

    private static void Append(List<TextSegmentDiffDto> list, string type, string text)
    {
        if (list.Count > 0 && list[^1].Type == type)
            list[^1].Text += text;
        else
            list.Add(new TextSegmentDiffDto { Type = type, Text = text });
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
