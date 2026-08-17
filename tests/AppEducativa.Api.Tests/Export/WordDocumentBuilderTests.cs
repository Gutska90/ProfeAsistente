using AppEducativa.Api.Services.Export;
using Microsoft.Extensions.Logging.Abstractions;

namespace AppEducativa.Api.Tests.Export;

public class WordDocumentBuilderTests
{
    [Fact]
    public async Task Builder_CreatesValidDocx_WithCoreElements()
    {
        var root = Path.Combine(Path.GetTempPath(), $"appedu-builder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            using var builder = new WordDocumentBuilder(new WordTemplateSettings { FontFamily = "Arial" });
            builder.AddTitle("Título")
                .AddSubtitle("Sub")
                .AddHeading("H1", 1)
                .AddParagraph("Párrafo con tildes: educación")
                .AddBulletList(["Uno", "Dos"])
                .AddNumberedList(["A", "B"])
                .AddTable(["Col1", "Col2"], [new[] { "1", "2" }])
                .AddCheckbox("Opción")
                .AddAnswerSpace(2)
                .AddPageBreak()
                .AddHeader("Header")
                .AddFooter("Footer", includePageNumber: true);

            var path = Path.Combine(root, "builder-test.docx");
            await builder.SaveAsync(path, CancellationToken.None);

            var validator = new WordExportValidator(NullLogger<WordExportValidator>.Instance);
            var result = validator.ValidateFile(path);
            Assert.True(result.IsValid, string.Join("; ", result.Errors));
            Assert.True(result.SizeBytes > 0);
            Assert.False(string.IsNullOrWhiteSpace(result.Sha256));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
