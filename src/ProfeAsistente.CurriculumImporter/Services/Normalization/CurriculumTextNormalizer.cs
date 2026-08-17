using System.Text;
using System.Text.RegularExpressions;

namespace ProfeAsistente.CurriculumImporter.Services.Normalization;

public interface ICurriculumTextNormalizer
{
    string Normalize(string text);
    string NormalizeOaCode(string code);
}

public sealed class CurriculumTextNormalizer : ICurriculumTextNormalizer
{
    private static readonly Regex SafeLineHyphen = new(@"(?<=\p{L})-\s*\r?\n\s*(?=\p{Ll})", RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex OaCode = new(@"^(?<prefix>MA\d{2}\s*)?OA\s*0*(?<number>\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Normalize(string text) =>
        Whitespace.Replace(SafeLineHyphen.Replace(text.Normalize(NormalizationForm.FormC).Replace('\u00A0', ' '), string.Empty), " ").Trim();

    public string NormalizeOaCode(string code)
    {
        var match = OaCode.Match(Normalize(code));
        if (!match.Success) return Normalize(code);
        var prefix = match.Groups["prefix"].Value.Replace(" ", string.Empty).ToUpperInvariant();
        return string.IsNullOrEmpty(prefix)
            ? $"OA {int.Parse(match.Groups["number"].Value)}"
            : $"{prefix[..2]}{prefix[2..]} OA {int.Parse(match.Groups["number"].Value)}";
    }
}
