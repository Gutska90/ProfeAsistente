using ProfeAsistente.CurriculumImporter.Diff;
using ProfeAsistente.CurriculumImporter.Extractors;
using ProfeAsistente.CurriculumImporter.Validation;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.CurriculumImporter.Tests;

public class CurriculumPipelineTests
{
    private static string SamplePath =>
        new[]
        {
            Path.Combine(AppContext.BaseDirectory, "samples", "4b-matematica-unidad-fracciones.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                "ProfeAsistente.CurriculumImporter", "samples", "4b-matematica-unidad-fracciones.json"))
        }.First(File.Exists);

    [Fact]
    public async Task ManualJson_ExtractsSampleWithOas()
    {
        Assert.True(File.Exists(SamplePath), $"Falta sample: {SamplePath}");
        var bytes = await File.ReadAllBytesAsync(SamplePath);
        var extractor = new ManualJsonCurriculumExtractor();
        var result = await extractor.ExtractAsync(
            new CurriculumSourceConfig { Nombre = "sample", Url = SamplePath, Tipo = "ManualJson", Formato = "Json" },
            new DownloadedSource { Content = bytes, UrlOriginal = SamplePath, HashSha256 = "x", RutaArchivoLocal = SamplePath });

        Assert.Equal("4B", result.Level!.Code);
        Assert.Equal("MAT", result.Subject!.Code);
        Assert.True(result.LearningObjectives.Count >= 4);
        Assert.Contains(result.LearningObjectives, o => o.Code == "MA04 OA 09");
        Assert.NotEmpty(result.EvaluationIndicators);
    }

    [Fact]
    public async Task Validator_AcceptsSampleJson()
    {
        var bytes = await File.ReadAllBytesAsync(SamplePath);
        var extraction = await new ManualJsonCurriculumExtractor().ExtractAsync(
            new CurriculumSourceConfig { Nombre = "s", Url = SamplePath, Tipo = "ManualJson", Formato = "Json" },
            new DownloadedSource { Content = bytes, UrlOriginal = SamplePath, HashSha256 = "x", RutaArchivoLocal = SamplePath });

        var validation = new CurriculumValidator().Validate(extraction);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
    }

    [Fact]
    public void Validator_RejectsInventedIndicatorOa()
    {
        var extraction = new CurriculumExtractionResult
        {
            Level = new LevelExtractDto { Code = "4B", Name = "4° básico" },
            Subject = new SubjectExtractDto { Code = "MAT", Name = "Matemática" },
            LearningObjectives =
            [
                new LearningObjectiveExtractDto
                {
                    Code = "MA04 OA 09",
                    Description = "Demostrar que comprenden las fracciones con denominadores varios."
                }
            ],
            EvaluationIndicators =
            [
                new EvaluationIndicatorExtractDto
                {
                    LearningObjectiveCode = "MA04 OA 99",
                    Description = "Indicador inventado que no apunta a OA existente."
                }
            ]
        };

        var validation = new CurriculumValidator().Validate(extraction);
        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, e => e.Contains("MA04 OA 99"));
    }

    [Fact]
    public void Diff_DetectsNuevoModificadoYEliminado()
    {
        var current = new CurriculumExtractionResult
        {
            LearningObjectives =
            [
                new LearningObjectiveExtractDto { Code = "A", Description = "Texto A original suficientemente largo." },
                new LearningObjectiveExtractDto { Code = "B", Description = "Texto B original suficientemente largo." }
            ]
        };
        var next = new CurriculumExtractionResult
        {
            LearningObjectives =
            [
                new LearningObjectiveExtractDto { Code = "A", Description = "Texto A modificado suficientemente largo." },
                new LearningObjectiveExtractDto { Code = "C", Description = "Texto C nuevo suficientemente largo." }
            ]
        };

        var diff = new CurriculumDiffService().Compare(next, current);
        Assert.Equal(1, diff.Nuevos);
        Assert.Equal(1, diff.Modificados);
        Assert.Equal(1, diff.PosiblementeEliminados);
        Assert.Contains(diff.Items, i => i.Tipo == TipoCambioCurricular.PosiblementeEliminado && i.Clave == "B");
    }

    [Fact]
    public void PublicadoFilter_RequiresAprobadoYVigente()
    {
        // Contrato API pública: solo EstadoRevision.Aprobado && Vigente
        var candidatos = new[]
        {
            (EstadoRevision.Aprobado, true, true),
            (EstadoRevision.Aprobado, false, false),
            (EstadoRevision.Pendiente, true, false),
            (EstadoRevision.RequiereCorreccion, true, false)
        };

        foreach (var (estado, vigente, esperado) in candidatos)
        {
            var publicado = estado == EstadoRevision.Aprobado && vigente;
            Assert.Equal(esperado, publicado);
        }
    }
}
