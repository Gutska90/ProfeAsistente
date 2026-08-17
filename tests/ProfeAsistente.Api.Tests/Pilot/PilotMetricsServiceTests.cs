using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.AI;
using ProfeAsistente.Api.Services.Pilot;
using ProfeAsistente.Api.Tests.TestDoubles;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Tests.Pilot;

public class PilotMetricsServiceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"pilot-{Guid.NewGuid():N}.db");
    private readonly ProfeAsistenteDbContext _db;

    public PilotMetricsServiceTests()
    {
        var opts = new DbContextOptionsBuilder<ProfeAsistenteDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _db = new ProfeAsistenteDbContext(opts);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Metrics_IncludesAiAndSessionReports()
    {
        var user = Guid.Parse("11111111-1111-1111-1111-111111111111");
        _db.AiUsageRecords.Add(new AiUsageRecord
        {
            Id = Guid.NewGuid(),
            OperationType = "EducationalDocument",
            Purpose = "Guide",
            PromptId = "learning-guide",
            PromptVersion = "v1",
            Provider = "Gemini",
            Model = "gemini-2.5-flash",
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Success = true,
            InputTokens = 1000,
            OutputTokens = 500,
            LatencyMilliseconds = 1200,
            EstimatedCostUsd = 0.001m,
            UserId = user
        });
        await _db.SaveChangesAsync();

        var svc = new PilotMetricsService(_db, new FakeCurrentUserService
        {
            UserId = user,
            Roles = [nameof(ApplicationRole.SystemAdministrator)]
        });

        var report = await svc.SubmitSessionReportAsync(new SubmitPilotSessionReportRequest
        {
            MinutesSavedEstimate = 25,
            WouldUseAgain = true,
            MaterialsUsedInClass = true,
            Comment = "Sirvió para la guía"
        });
        Assert.Equal(25, report.MinutesSavedEstimate);

        var metrics = await svc.GetMetricsAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        Assert.Equal(1, metrics.AiGenerations);
        Assert.Equal(1, metrics.SessionReports);
        Assert.Equal(25, metrics.AvgMinutesSavedReported);
        Assert.Contains("min ahorrados", metrics.SummaryLine);
    }

    [Fact]
    public async Task SubmitSessionReport_RejectsOutOfRangeMinutes()
    {
        var svc = new PilotMetricsService(_db, new FakeCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = [nameof(ApplicationRole.SystemAdministrator)]
        });
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            svc.SubmitSessionReportAsync(new SubmitPilotSessionReportRequest { MinutesSavedEstimate = 999 }));
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { /* ignore */ }
    }
}
