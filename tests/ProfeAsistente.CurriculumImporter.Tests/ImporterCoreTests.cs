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

public class ImporterCoreTests
{
    [Fact]
    public async Task Download_RejectsHttpAndWrongDomain()
    {
        var downloader = CreateDownloader(new StaticHandler(HttpStatusCode.OK, "ok"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => downloader.DownloadAsync(Source("http://www.curriculumnacional.cl/a.pdf")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => downloader.DownloadAsync(Source("https://example.org/a.pdf")));
    }

    [Fact]
    public async Task Download_StreamsShaAndUses304Cache()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var bytes = Encoding.UTF8.GetBytes("contenido curricular");
        var handler = new SequenceHandler(
            Response(HttpStatusCode.OK, bytes, "\"v1\""),
            Response(HttpStatusCode.NotModified, [], null));
        var downloader = CreateDownloader(handler, temp);

        var first = await downloader.DownloadAsync(Source());
        var second = await downloader.DownloadAsync(Source());

        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), first.Sha256);
        Assert.True(second.WasNotModified);
        Assert.True(File.Exists(first.LocalFilePath));
    }

    [Fact]
    public async Task Download_RejectsOversizedContent()
    {
        var downloader = CreateDownloader(new StaticHandler(HttpStatusCode.OK, "12345"), maxSize: 4);
        await Assert.ThrowsAsync<SourceDownloadException>(() => downloader.DownloadAsync(Source()));
    }

    [Fact]
    public async Task PdfExtractor_ExtractsSyntheticPdfPage()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "tiny.pdf");
        Directory.CreateDirectory(root);
        await File.WriteAllBytesAsync(path, BuildPdf("OA 1 texto"));
        var extractor = new PdfProgramStudyExtractor(new CurriculumTextNormalizer(), new CurriculumFileStorage(root));
        var result = await extractor.ExtractAsync(Source(), new ProfeAsistente.CurriculumImporter.Models.Download.DownloadedSource { LocalFilePath = path });

        Assert.Single(result.Pages);
        Assert.Contains("OA 1", result.Pages[0].OriginalText);
        Assert.False(result.RequiresManualReview);
    }

    [Fact]
    public async Task Parser_DetectsUnitAndTwoOas()
    {
        var profile = Path.Combine(AppContext.BaseDirectory, "Configuration", "ParserProfiles", "matematica-4-basico.json");
        var extraction = new CurriculumExtractionResult();
        var fixture = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "fixtures", "matematica-4-basico-parser.txt"));
        extraction.Pages.Add(new ExtractedPage(55, fixture, ""));
        var package = await new MathematicsFourthGradeProgramParser(new CurriculumTextNormalizer(), profile).ParseAsync(Source(), extraction);

        Assert.Single(package.Units);
        Assert.Equal(2, package.LearningObjectives.Count);
        Assert.Equal("OA 1", package.LearningObjectives[0].Code);
        Assert.Contains(package.Indicators, i => i.LearningObjectiveCode == "OA 1");
    }

    [Fact]
    public void Validator_FlagsDuplicatesOrphansAndMissingUnits()
    {
        var package = new ExtractedCurriculumPackage();
        package.LearningObjectives.Add(new() { Code = "MA04 OA 1" });
        package.LearningObjectives.Add(new() { Code = "MA04 OA 1" });
        package.Indicators.Add(new() { LearningObjectiveCode = "MA04 OA 99", Description = "Sin objetivo" });

        var issues = new CurriculumExtractionValidator().Validate(package);

        Assert.Contains(issues, i => i.Code == "duplicate-oa");
        Assert.Contains(issues, i => i.Code == "orphan-indicator");
        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Blocking);
    }

    private static HttpSourceDownloader CreateDownloader(HttpMessageHandler handler, string? cache = null, long? maxSize = null) =>
        new(new HttpClient(handler), new DownloaderOptions { CacheDirectory = cache ?? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), DelayMsBetweenRequests = 0, MaxDownloadSizeBytes = maxSize ?? 25 * 1024 * 1024 },
            NullLogger<HttpSourceDownloader>.Instance);

    private static CurriculumSourceDefinition Source(string url = "https://www.curriculumnacional.cl/a.pdf") => new()
    {
        Id = "matematica-4-basico-programa", Nombre = "Matemática", Url = url, DominioPermitido = "www.curriculumnacional.cl",
        TipoFuente = "ProgramaEstudio", Formato = "Pdf", NivelCodigo = "4B", AsignaturaCodigo = "MAT"
    };

    private static HttpResponseMessage Response(HttpStatusCode status, byte[] body, string? etag)
    {
        var response = new HttpResponseMessage(status) { Content = new ByteArrayContent(body) };
        if (etag is not null) response.Headers.ETag = new EntityTagHeaderValue(etag);
        return response;
    }

    private sealed class StaticHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(Response(status, Encoding.UTF8.GetBytes(body), null));
    }

    private sealed class SequenceHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_responses.Dequeue());
    }

    private static byte[] BuildPdf(string text)
    {
        var stream = $"BT /F1 12 Tf 72 720 Td ({text}) Tj ET";
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
}
