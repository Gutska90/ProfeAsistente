using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using ProfeAsistente.CurriculumImporter.Download;
using ProfeAsistente.CurriculumImporter.Models.Extraction;
using ProfeAsistente.CurriculumImporter.Models.Sources;
using ProfeAsistente.CurriculumImporter.Services.Download;
using ProfeAsistente.CurriculumImporter.Services.Extraction;
using ProfeAsistente.CurriculumImporter.Services.Normalization;
using ProfeAsistente.CurriculumImporter.Services.Parsing;
using ProfeAsistente.CurriculumImporter.Services.Storage;
using ProfeAsistente.CurriculumImporter.Services.Validation;
using Microsoft.Extensions.Logging.Abstractions;

namespace ProfeAsistente.CurriculumImporter.Tests;

/// <summary>
/// Integración offline: descarga (HttpMessageHandler falso) → PDF → extract → parse → validate.
/// No depende de internet.
/// </summary>
public class OfficialImportIntegrationTests
{
    [Fact]
    public async Task OfflinePipeline_DownloadExtractParseValidate_DetectsUnitAndTwoOas()
    {
        var root = Path.Combine(Path.GetTempPath(), "cn-int-" + Guid.NewGuid().ToString("N"));
        var cache = Path.Combine(root, "cache");
        Directory.CreateDirectory(root);

        var fixture = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "fixtures", "matematica-4-basico-parser.txt"));
        var pdfBytes = BuildPdf("Programa Matemática 4B " + fixture.Replace("\n", " ").Replace("(", "[").Replace(")", "]"));

        var handler = new FixedPdfHandler(pdfBytes);
        var downloader = new HttpSourceDownloader(
            new HttpClient(handler),
            new DownloaderOptions
            {
                CacheDirectory = cache,
                DelayMsBetweenRequests = 0,
                MaxDownloadSizeBytes = 5_000_000
            },
            NullLogger<HttpSourceDownloader>.Instance);

        var source = Source();
        var downloaded = await downloader.DownloadAsync(source);
        Assert.True(File.Exists(downloaded.LocalFilePath));
        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(pdfBytes)).ToLowerInvariant(),
            downloaded.Sha256);

        var storage = new CurriculumFileStorage(root);
        var extractor = new PdfProgramStudyExtractor(new CurriculumTextNormalizer(), storage);
        var extraction = await extractor.ExtractAsync(source, downloaded);
        Assert.NotEmpty(extraction.Pages);
        Assert.Equal(1, extraction.Pages[0].PageNumber);

        // El PDF sintético pierde saltos de línea; el parser usa el fixture como extracción determinista.
        var parseInput = new CurriculumExtractionResult();
        parseInput.Pages.Add(new ExtractedPage(55, fixture, ""));
        var profile = Path.Combine(AppContext.BaseDirectory, "Configuration", "ParserProfiles", "matematica-4-basico.json");
        Assert.True(File.Exists(profile), "Falta el perfil matematica-4-basico.json en salida de tests.");

        var package = await new MathematicsFourthGradeProgramParser(new CurriculumTextNormalizer(), profile)
            .ParseAsync(source, parseInput);

        Assert.NotEmpty(package.Units);
        Assert.True(package.LearningObjectives.Count >= 2);
        Assert.Equal("OA 1", package.LearningObjectives[0].Code);

        var issues = new CurriculumExtractionValidator().Validate(package);
        Assert.DoesNotContain(issues, i => i.Severity == ValidationSeverity.Blocking);
    }

    private static CurriculumSourceDefinition Source() => new()
    {
        Id = "matematica-4-basico-programa",
        Nombre = "Programa demo test",
        Url = "https://www.curriculumnacional.cl/test/fixture.pdf",
        DominioPermitido = "www.curriculumnacional.cl",
        TipoFuente = "ProgramaEstudio",
        Formato = "Pdf",
        NivelCodigo = "4B",
        AsignaturaCodigo = "MAT",
        Activo = true,
        IntervaloSolicitudesMs = 0
    };

    private static byte[] BuildPdf(string text)
    {
        if (text.Length > 180) text = text[..180];
        text = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var stream = $"BT /F1 10 Tf 40 740 Td ({text}) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        var builder = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xref = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) builder.Append($"{offset:D10} 00000 n \n");
        builder.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private sealed class FixedPdfHandler(byte[] bytes) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            return Task.FromResult(response);
        }
    }
}
