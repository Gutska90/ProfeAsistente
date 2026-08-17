using System.Security.Cryptography;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

namespace AppEducativa.Api.Services.Export;

public sealed class WordExportValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public long SizeBytes { get; init; }
    public string? Sha256 { get; init; }
}

public interface IWordExportValidator
{
    WordExportValidationResult ValidateFile(string filePath);
    string ComputeSha256(string filePath);
}

public sealed class WordExportValidator : IWordExportValidator
{
    private readonly ILogger<WordExportValidator> _logger;

    public WordExportValidator(ILogger<WordExportValidator> logger) => _logger = logger;

    public WordExportValidationResult ValidateFile(string filePath)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (!File.Exists(filePath))
            return Fail(["El archivo exportado no existe."]);

        var info = new FileInfo(filePath);
        if (info.Length <= 0)
            return Fail(["El archivo exportado está vacío."]);

        try
        {
            using var doc = WordprocessingDocument.Open(filePath, false);
            if (doc.MainDocumentPart?.Document?.Body is null)
                return Fail(["El documento no tiene cuerpo."]);

            var body = doc.MainDocumentPart.Document.Body;
            if (!body.ChildElements.Any())
                return Fail(["El documento está vacío."]);

            if (!body.Elements<DocumentFormat.OpenXml.Wordprocessing.SectionProperties>().Any()
                && doc.MainDocumentPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.SectionProperties>().Any() == false)
            {
                warnings.Add("El documento no declara SectionProperties explícitas.");
            }

            var validator = new OpenXmlValidator();
            foreach (var err in validator.Validate(doc))
            {
                var msg = $"{err.Description} ({err.Path?.XPath})";
                if (msg.Contains("unexpected", StringComparison.OrdinalIgnoreCase))
                    warnings.Add(msg);
                else
                    errors.Add(msg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DOCX validation failed for {Path}", Path.GetFileName(filePath));
            return Fail([$"Archivo DOCX inválido o corrupto: {ex.Message}"]);
        }

        if (errors.Count > 0)
            return new WordExportValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings,
                SizeBytes = info.Length
            };

        return new WordExportValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = warnings,
            SizeBytes = info.Length,
            Sha256 = ComputeSha256(filePath)
        };
    }

    public string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static WordExportValidationResult Fail(List<string> errors) =>
        new() { IsValid = false, Errors = errors };
}
