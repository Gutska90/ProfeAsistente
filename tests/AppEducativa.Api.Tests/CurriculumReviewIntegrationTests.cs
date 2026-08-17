using System.Text.Json;
using AppEducativa.Api.Data;
using AppEducativa.Api.Models.Curriculum;
using AppEducativa.Api.Repositories;
using AppEducativa.Api.Services;
using AppEducativa.Api.Services.Curriculum;
using AppEducativa.CurriculumImporter.Diff;
using AppEducativa.CurriculumImporter.Validation;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace AppEducativa.Api.Tests;

public class CurriculumReviewIntegrationTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"appedu-review-{Guid.NewGuid():N}.db");
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), $"appedu-root-{Guid.NewGuid():N}");
    private readonly AppEducativaDbContext _db;
    private readonly CurriculumReviewService _review;
    private readonly CurriculumRepository _curriculum;

    public CurriculumReviewIntegrationTests()
    {
        Directory.CreateDirectory(_contentRoot);
        var options = new DbContextOptionsBuilder<AppEducativaDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _db = new AppEducativaDbContext(options);
        _db.Database.Migrate();
        DemoCurriculumSeed.Seed(_db);
        _db.SaveChanges();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Curriculum:StorageRoot"] = Path.Combine(_contentRoot, "App_Data", "Curriculum")
            })
            .Build();
        var env = new FakeWebHostEnvironment { ContentRootPath = _contentRoot };
        var validator = new CurriculumValidator();
        _review = new CurriculumReviewService(
            _db, validator, config, env, NullLogger<CurriculumReviewService>.Instance);
        _curriculum = new CurriculumRepository(_db);
    }

    [Fact]
    public async Task FullReviewFlow_CorrectMoveAcceptImportPublish_PreservesManualEdit()
    {
        var batch = await SeedPendingBatchAsync();
        var session = await _review.StartReviewAsync(batch.Id, "revisor-test");
        Assert.Equal(nameof(CurriculumReviewStatus.InProgress), session.Status);

        var package = await _review.GetReviewPackageAsync(batch.Id);
        Assert.NotNull(package);
        var oa = package!.Objectives[0];
        var row = package.RowVersion;

        const string corrected = "Descripción corregida manualmente del OA con suficiente longitud.";
        package = await _review.UpdateObjectiveAsync(batch.Id, oa.TemporaryId, new UpdateReviewObjectiveRequest
        {
            Description = corrected,
            Decision = CurriculumRecordDecision.Corrected,
            Reason = "Ajuste de redacción",
            RowVersion = row
        });
        row = package.RowVersion;

        var indicator = package.Indicators.First();
        var otherOa = package.Objectives.First(o => o.TemporaryId != oa.TemporaryId);
        package = await _review.UpdateIndicatorAsync(batch.Id, indicator.TemporaryId, new UpdateReviewIndicatorRequest
        {
            ObjectiveTemporaryId = otherOa.TemporaryId,
            Decision = CurriculumRecordDecision.Corrected,
            Reason = "Indicador pertenecía a otro OA",
            RowVersion = row
        });
        row = package.RowVersion;

        package = await _review.BulkDecideAsync(batch.Id, new BulkDecisionRequest
        {
            EntityType = "Unit",
            TemporaryIds = package.Units.Select(u => u.TemporaryId).ToList(),
            Decision = CurriculumRecordDecision.Accepted,
            RowVersion = row
        });
        row = package.RowVersion;

        package = await _review.BulkDecideAsync(batch.Id, new BulkDecisionRequest
        {
            EntityType = "LearningObjective",
            TemporaryIds = package.Objectives.Select(o => o.TemporaryId).ToList(),
            Decision = CurriculumRecordDecision.Accepted,
            RowVersion = row
        });
        row = package.RowVersion;
        package = await _review.UpdateObjectiveAsync(batch.Id, oa.TemporaryId, new UpdateReviewObjectiveRequest
        {
            Decision = CurriculumRecordDecision.Corrected,
            RowVersion = row
        });
        row = package.RowVersion;

        package = await _review.BulkDecideAsync(batch.Id, new BulkDecisionRequest
        {
            EntityType = "EvaluationIndicator",
            TemporaryIds = package.Indicators.Select(i => i.TemporaryId).ToList(),
            Decision = CurriculumRecordDecision.Accepted,
            RowVersion = row
        });

        var validation = await _review.RevalidateAsync(batch.Id);
        Assert.DoesNotContain(validation.Issues, i => i.Blocking);

        await _review.GetRichDiffAsync(batch.Id);
        await _review.MarkReadyForApprovalAsync(batch.Id, "revisor-test");
        await _review.ApproveFromReviewAsync(batch.Id, "aprobador");

        batch = await _db.CurriculumImportBatches.FirstAsync(b => b.Id == batch.Id);
        Assert.Equal(CurriculumImportStatus.Approved, batch.Status);
        Assert.False(string.IsNullOrWhiteSpace(batch.FinalReviewJson));

        batch.ExtractionJson = batch.FinalReviewJson;
        await _db.SaveChangesAsync();

        var importer = new EfCurriculumImportService(
            _db, new CurriculumValidator(), new CurriculumDiffService(),
            NullLogger<EfCurriculumImportService>.Instance);
        var importResult = await importer.ApproveBatchAsync(batch.Id);
        Assert.True(importResult.Success, string.Join("; ", importResult.Errores));

        batch = await _db.CurriculumImportBatches.FirstAsync(b => b.Id == batch.Id);
        batch.Status = CurriculumImportStatus.Imported;
        await _db.SaveChangesAsync();

        await _review.PublishAsync(batch.Id, "publicador");

        var official = await _db.ObjetivosAprendizaje.AsNoTracking()
            .Where(o => o.EsContenidoOficial && o.PublicationStatus == CurriculumPublicationStatus.Published)
            .ToListAsync();
        Assert.Contains(official, o => o.Descripcion.Contains("corregida manualmente", StringComparison.OrdinalIgnoreCase));

        var changes = await _review.GetChangesAsync(batch.Id);
        Assert.NotEmpty(changes);

        var unidadId = await _db.UnidadObjetivos.AsNoTracking()
            .Where(uo => official.Select(o => o.Id).Contains(uo.ObjetivoAprendizajeId))
            .Select(uo => uo.UnidadId)
            .FirstAsync();
        var publicOas = await _curriculum.GetObjetivosPorUnidadAsync(unidadId);
        Assert.Contains(publicOas, o => o.EsContenidoOficial && o.Descripcion.Contains("corregida manualmente"));
    }

    [Fact]
    public async Task MarkReady_Fails_WhenPendingDecisions()
    {
        var batch = await SeedPendingBatchAsync();
        await _review.StartReviewAsync(batch.Id, "r");
        await Assert.ThrowsAsync<CurriculumReviewException>(() =>
            _review.MarkReadyForApprovalAsync(batch.Id, "r"));
    }

    [Fact]
    public async Task ConcurrentEdit_ReturnsConflict()
    {
        var batch = await SeedPendingBatchAsync();
        await _review.StartReviewAsync(batch.Id, "r");
        var package = await _review.GetReviewPackageAsync(batch.Id);
        var oa = package!.Objectives[0];
        var stale = package.RowVersion;

        await _review.UpdateObjectiveAsync(batch.Id, oa.TemporaryId, new UpdateReviewObjectiveRequest
        {
            Description = "Primera edición con texto suficientemente largo.",
            RowVersion = stale
        });

        var ex = await Assert.ThrowsAsync<CurriculumReviewException>(() =>
            _review.UpdateObjectiveAsync(batch.Id, oa.TemporaryId, new UpdateReviewObjectiveRequest
            {
                Description = "Segunda edición concurrente con texto largo.",
                RowVersion = stale
            }));
        Assert.Equal(409, ex.StatusCode);
    }

    private async Task<CurriculumImportBatch> SeedPendingBatchAsync()
    {
        var extraction = new CurriculumExtractionResult
        {
            SourceTitle = "Fixture Programa Matemática 4B",
            SourceUrl = "https://www.curriculumnacional.cl/fixture.pdf",
            ConfianzaExtraccion = 0.8,
            Level = new LevelExtractDto { Code = "4B", Name = "4° básico" },
            Subject = new SubjectExtractDto { Code = "MAT", Name = "Matemática" },
            Units =
            [
                new UnitExtractDto
                {
                    Number = 1,
                    Name = "Números",
                    LearningObjectiveCodes = ["OA 1", "OA 2"]
                }
            ],
            LearningObjectives =
            [
                new LearningObjectiveExtractDto
                {
                    Code = "OA 1",
                    Description = "Representar y describir números naturales hasta diez mil."
                },
                new LearningObjectiveExtractDto
                {
                    Code = "OA 2",
                    Description = "Describir y aplicar estrategias de cálculo mental básicas."
                }
            ],
            EvaluationIndicators =
            [
                new EvaluationIndicatorExtractDto
                {
                    LearningObjectiveCode = "OA 1",
                    Description = "Expresan números en palabras y cifras correctamente."
                },
                new EvaluationIndicatorExtractDto
                {
                    LearningObjectiveCode = "OA 2",
                    Description = "Aplican descomposición en el cálculo mental diario."
                }
            ]
        };

        var json = JsonSerializer.Serialize(extraction);
        var batch = new CurriculumImportBatch
        {
            Id = Guid.NewGuid(),
            SourceExternalId = "matematica-4-basico-programa",
            Status = CurriculumImportStatus.PendingReview,
            Estado = EstadoImportBatch.Validado,
            FechaInicio = DateTime.UtcNow,
            OriginalExtractionJson = json,
            ExtractionJson = json,
            CantidadUnidades = 1,
            CantidadOA = 2,
            CantidadIndicadores = 2
        };
        _db.CurriculumImportBatches.Add(batch);
        await _db.SaveChangesAsync();
        return batch;
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { /* ignore */ }
        try { Directory.Delete(_contentRoot, true); } catch { /* ignore */ }
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Tests";
        public string WebRootPath { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
