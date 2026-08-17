using ProfeAsistente.Api.Services.Export;
using Microsoft.Extensions.Logging.Abstractions;

namespace ProfeAsistente.Api.Tests.Export;

public class WordExportValidatorTests
{
    [Fact]
    public void Validator_RejectsMissingFile()
    {
        var validator = new WordExportValidator(NullLogger<WordExportValidator>.Instance);
        var result = validator.ValidateFile(Path.Combine(Path.GetTempPath(), $"no-existe-{Guid.NewGuid():N}.docx"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validator_AcceptsValidBuilderOutput()
    {
        var path = Path.Combine(Path.GetTempPath(), $"valid-{Guid.NewGuid():N}.docx");
        try
        {
            using (var builder = new WordDocumentBuilder(new WordTemplateSettings { FontFamily = "Calibri" }))
            {
                builder.AddTitle("Documento válido").AddParagraph("Contenido");
                await builder.SaveAsync(path, CancellationToken.None);
            }

            var validator = new WordExportValidator(NullLogger<WordExportValidator>.Instance);
            var result = validator.ValidateFile(path);
            Assert.True(result.IsValid, string.Join("; ", result.Errors));
            Assert.True(result.SizeBytes > 0);
            Assert.Equal(64, result.Sha256!.Length);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Validator_RejectsCorruptFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"corrupt-{Guid.NewGuid():N}.docx");
        try
        {
            File.WriteAllText(path, "esto-no-es-un-docx");
            var validator = new WordExportValidator(NullLogger<WordExportValidator>.Instance);
            var result = validator.ValidateFile(path);
            Assert.False(result.IsValid);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
