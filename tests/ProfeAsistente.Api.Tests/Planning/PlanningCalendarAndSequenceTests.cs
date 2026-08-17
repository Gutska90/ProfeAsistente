using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.Planning;
using ProfeAsistente.Api.Repositories;
using ProfeAsistente.Api.Services;
using ProfeAsistente.Api.Services.Coverage;
using ProfeAsistente.Api.Services.DateTimeServices;
using ProfeAsistente.Api.Services.PlanningCalendar;
using ProfeAsistente.Api.Services.PlanningSequence;
using ProfeAsistente.Api.Services.PlanningSuggestions;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ProfeAsistente.Api.Tests.Planning;

public class PlanningCalendarAndSequenceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"appedu-plan-{Guid.NewGuid():N}.db");
    private readonly ProfeAsistenteDbContext _db;

    public PlanningCalendarAndSequenceTests()
    {
        var options = new DbContextOptionsBuilder<ProfeAsistenteDbContext>().UseSqlite($"Data Source={_dbPath}").Options;
        _db = new ProfeAsistenteDbContext(options);
        _db.Database.Migrate();
        DemoCurriculumSeed.Seed(_db);
        _db.SaveChanges();
    }

    [Fact]
    public void Generator_CreatesMondayAndWednesday_ExcludesDate_TwoSessionsPerDay()
    {
        var generator = new PlanningCalendarGenerator();
        var config = new PlanningScheduleConfiguration
        {
            StartDate = new DateOnly(2026, 3, 2), // Monday
            EndDate = new DateOnly(2026, 3, 13)
        };
        var weekly = new List<WeeklyClassSchedule>
        {
            new() { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(8, 0), DurationMinutes = 90, SessionsPerDay = 2, IsActive = true },
            new() { DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(10, 0), DurationMinutes = 90, SessionsPerDay = 1, IsActive = true }
        };
        var excluded = new HashSet<DateOnly> { new(2026, 3, 4) }; // Wednesday

        var slots = generator.GenerateSlots(config, weekly, excluded);
        Assert.DoesNotContain(slots, s => s.Date == new DateOnly(2026, 3, 4));
        Assert.Equal(2, slots.Count(s => s.Date == new DateOnly(2026, 3, 2)));
        Assert.True(slots.Select(s => s.SessionNumber).SequenceEqual(Enumerable.Range(1, slots.Count)));
        Assert.All(slots, s => Assert.Equal(90, s.DurationMinutes));
    }

    [Fact]
    public void Bloom_Progression_ForOneTwoAndFourSessions()
    {
        var bloom = new BloomProgressionService();
        var settings = new BloomProgressionSettingsRequest
        {
            InitialLevel = NivelBloom.Recordar,
            TargetLevel = NivelBloom.Aplicar,
            MaximumLevelJump = 1
        };

        Assert.Equal([NivelBloom.Aplicar], bloom.SuggestForObjective(1, settings));
        Assert.Equal(2, bloom.SuggestForObjective(2, settings).Count);
        Assert.Equal(4, bloom.SuggestForObjective(4, settings).Count);
        Assert.True(bloom.IsExcessiveJump(NivelBloom.Recordar, NivelBloom.Evaluar, 1));
        Assert.False(bloom.IsExcessiveJump(NivelBloom.Recordar, NivelBloom.Comprender, 1));
    }

    [Fact]
    public async Task Integration_ConfigureGenerateSequenceConfirmCoverage()
    {
        var planId = await CreatePlanAsync();
        var calendar = BuildCalendar();
        var sequence = BuildSequence();
        var coverage = BuildCoverage();
        var suggestions = BuildSuggestions(calendar, coverage);

        await calendar.ConfigureAsync(planId, new ConfigurePlanningScheduleRequest
        {
            StartDate = new DateOnly(2026, 3, 2),
            EndDate = new DateOnly(2026, 4, 24),
            TimeZoneId = "America/Santiago",
            DefaultClassDurationMinutes = 90,
            WeeklySchedule =
            [
                new WeeklyScheduleRequest { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(8, 0), DurationMinutes = 90, SessionsPerDay = 1 },
                new WeeklyScheduleRequest { DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(8, 0), DurationMinutes = 90, SessionsPerDay = 1 }
            ],
            ExcludedDates =
            [
                new AddExcludedDateRequest { Date = new DateOnly(2026, 3, 18), Reason = "Feriado", ExclusionType = PlanningExclusionType.Holiday },
                new AddExcludedDateRequest { Date = new DateOnly(2026, 4, 1), Reason = "Actividad", ExclusionType = PlanningExclusionType.SchoolActivity }
            ]
        });

        var generated = await calendar.GenerateSessionsAsync(planId, new GenerateCalendarSessionsRequest());
        Assert.True(generated.AvailableSessionCount >= 8);
        Assert.DoesNotContain(generated.Sessions, s => s.ScheduledDate == new DateOnly(2026, 3, 18));

        var locked = generated.Sessions.OrderBy(s => s.SessionNumber).First();
        await calendar.LockSessionAsync(locked.Id, new LockSessionRequest { LockReason = "Confirmada con curso" });

        var proposal = await sequence.GenerateProposalAsync(planId, new GeneratePlanningSequenceRequest
        {
            Objectives =
            [
                new ObjectiveCoverageRequest { ObjectiveId = DemoCurriculumSeed.Oa1Id, MinimumSessions = 2, Priority = 1, IndicatorIds = [] },
                new ObjectiveCoverageRequest { ObjectiveId = DemoCurriculumSeed.Oa2Id, MinimumSessions = 2, Priority = 2 },
                new ObjectiveCoverageRequest { ObjectiveId = DemoCurriculumSeed.Oa3Id, MinimumSessions = 1, Priority = 3 }
            ],
            IncludeDiagnosticClass = true,
            IncludeReviewClasses = true,
            ReviewClassCount = 1,
            IncludeAssessmentClass = true,
            AssessmentClassCount = 1,
            BloomProgression = new BloomProgressionSettingsRequest
            {
                InitialLevel = NivelBloom.Recordar,
                TargetLevel = NivelBloom.Aplicar,
                MaximumLevelJump = 2
            }
        });

        Assert.Null(proposal.Deficit);
        Assert.NotEmpty(proposal.Items);
        Assert.Contains(proposal.Items, i => i.ClassType == PlanningClassType.Diagnostic);
        Assert.Contains(proposal.Items, i => i.ClassType == PlanningClassType.SummativeAssessment);

        var item = proposal.Items.First(i => i.ClassType == PlanningClassType.Practice || i.ClassType == PlanningClassType.Introduction);
        proposal = await sequence.UpdateProposalItemAsync(proposal.Id, item.Id, new UpdatePlanningSequenceItemRequest
        {
            SuggestedTitle = "Título editado",
            BloomLevel = "Comprender"
        });
        Assert.Contains(proposal.Items, i => i.WasManuallyModified && i.SuggestedTitle == "Título editado");

        var validation = await sequence.ValidateProposalAsync(proposal.Id);
        Assert.True(validation.CanConfirm, string.Join("; ", validation.Errors));

        await sequence.ConfirmProposalAsync(proposal.Id);
        var classes = await _db.Clases.Where(c => c.PlanificacionId == planId).OrderBy(c => c.Numero).ToListAsync();
        Assert.NotEmpty(classes);
        Assert.True(classes.Select(c => c.Numero).SequenceEqual(Enumerable.Range(1, classes.Count)));

        var toCancel = (await calendar.GetCalendarAsync(planId))!.Sessions.First(s => !s.IsLocked && s.Status != PlanningSessionStatus.Cancelled);
        await calendar.CancelSessionAsync(toCancel.Id, new CancelPlanningSessionRequest { Reason = "Suspensión" });

        var covPlanned = await coverage.RecalculateAsync(planId);
        Assert.NotNull(covPlanned.Matrix);
        Assert.NotEmpty(covPlanned.Objectives);

        var alerts = await coverage.GetAlertsAsync(planId);
        // may or may not have alerts depending on distribution

        var sug = await suggestions.GetSuggestionsAsync(planId);
        var restore = sug.FirstOrDefault(s => s.Code.StartsWith("RESTORE_SESSION_"));
        if (restore is not null)
            await suggestions.ApplyAsync(planId, restore.Id);

        var firstClass = classes.First();
        await coverage.CompleteClassAsync(firstClass.Id, new CompleteClassRequest
        {
            Observation = "Clase realizada",
            Evidences =
            [
                new RecordLearningEvidenceRequest
                {
                    EvidenceType = LearningEvidenceType.ExitTicket,
                    Description = "Ticket de salida"
                }
            ]
        });

        var executed = await coverage.GetCoverageAsync(planId, "Executed");
        Assert.Equal("Executed", executed.Mode);
        Assert.Contains(executed.Objectives, o => o.Worked);

        // Locked session preserved through regenerate
        var preview = await calendar.PreviewRegenerationAsync(planId);
        Assert.True(preview.Preview!.ProtectedSessions >= 1);

        await calendar.GenerateSessionsAsync(planId, new GenerateCalendarSessionsRequest { ConfirmDestructiveChanges = true });
        var after = await calendar.GetCalendarAsync(planId);
        Assert.Contains(after!.Sessions, s => s.Id == locked.Id && s.IsLocked);
    }

    [Fact]
    public async Task Sequence_DetectsDeficit_WhenMinimumExceedsSessions()
    {
        var planId = await CreatePlanAsync(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6));
        var calendar = BuildCalendar();
        await calendar.ConfigureAsync(planId, new ConfigurePlanningScheduleRequest
        {
            StartDate = new DateOnly(2026, 3, 2),
            EndDate = new DateOnly(2026, 3, 6),
            WeeklySchedule =
            [
                new WeeklyScheduleRequest { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(8, 0), DurationMinutes = 90 }
            ]
        });
        await calendar.GenerateSessionsAsync(planId, new GenerateCalendarSessionsRequest());

        var proposal = await BuildSequence().GenerateProposalAsync(planId, new GeneratePlanningSequenceRequest
        {
            Objectives =
            [
                new ObjectiveCoverageRequest { ObjectiveId = DemoCurriculumSeed.Oa1Id, MinimumSessions = 5 },
                new ObjectiveCoverageRequest { ObjectiveId = DemoCurriculumSeed.Oa2Id, MinimumSessions = 5 }
            ],
            IncludeDiagnosticClass = false,
            IncludeReviewClasses = false,
            IncludeAssessmentClass = false
        });

        Assert.NotNull(proposal.Deficit);
        Assert.True(proposal.Deficit!.Deficit > 0);
        Assert.NotEmpty(proposal.Deficit.Alternatives);
    }

    [Fact]
    public async Task Reschedule_PreservesClassId()
    {
        var planId = await CreatePlanAsync();
        var calendar = BuildCalendar();
        await calendar.ConfigureAsync(planId, new ConfigurePlanningScheduleRequest
        {
            StartDate = new DateOnly(2026, 3, 2),
            EndDate = new DateOnly(2026, 3, 31),
            WeeklySchedule =
            [
                new WeeklyScheduleRequest { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(8, 0), DurationMinutes = 90 },
                new WeeklyScheduleRequest { DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(8, 0), DurationMinutes = 90 }
            ]
        });
        var cal = await calendar.GenerateSessionsAsync(planId, new GenerateCalendarSessionsRequest());
        var session = cal.Sessions.First();

        var clases = new ClaseService(_db, new PlanificacionRepository(_db), new ClaseRepository(_db));
        var clase = await clases.CrearAsync(planId, new CrearClaseRequest
        {
            ObjetivoAprendizajeId = DemoCurriculumSeed.Oa1Id,
            Fecha = session.ScheduledDate
        });

        var entity = await _db.PlanningCalendarSessions.FirstAsync(s => s.Id == session.Id);
        entity.ClassId = clase.Id;
        entity.Status = PlanningSessionStatus.Assigned;
        await _db.SaveChangesAsync();

        var newDate = session.ScheduledDate.AddDays(2);
        var updated = await calendar.RescheduleSessionAsync(session.Id, new RescheduleSessionRequest
        {
            NewDate = newDate,
            Reason = "Cambio de horario"
        });

        Assert.Equal(clase.Id, updated.ClassId);
        Assert.Equal(newDate, updated.ScheduledDate);
        var claseDb = await _db.Clases.FirstAsync(c => c.Id == clase.Id);
        Assert.Equal(newDate, claseDb.Fecha);
        Assert.True(await _db.PlanningSessionHistories.AnyAsync(h => h.PlanningCalendarSessionId == session.Id));
    }

    private async Task<Guid> CreatePlanAsync(DateOnly? start = null, DateOnly? end = null)
    {
        var planes = new PlanificacionService(_db, new PlanificacionRepository(_db), new ProfeAsistente.Api.Tests.TestDoubles.FakeCurrentUserService(), new ProfeAsistente.Api.Tests.TestDoubles.AllowAllResourceAuthorizationService());
        var plan = await planes.CrearAsync(new CrearPlanificacionRequest
        {
            NivelId = DemoCurriculumSeed.NivelId,
            AsignaturaId = DemoCurriculumSeed.NivelAsignaturaId,
            UnidadId = DemoCurriculumSeed.UnidadId,
            Nombre = "Plan calendario",
            FechaInicio = start ?? new DateOnly(2026, 3, 2),
            FechaFin = end ?? new DateOnly(2026, 4, 24)
        });
        return plan.Id;
    }

    private IPlanningCalendarService BuildCalendar() =>
        new PlanningCalendarService(
            _db,
            new PlanningCalendarGenerator(),
            new PlanningCalendarValidator(),
            new TimeZoneService(),
            new SystemApplicationClock(),
            NullLogger<PlanningCalendarService>.Instance);

    private ICurriculumCoverageService BuildCoverage() =>
        new CurriculumCoverageService(
            _db,
            new CurriculumCoverageCalculator(),
            new CurriculumCoverageValidator(),
            new SystemApplicationClock(),
            NullLogger<CurriculumCoverageService>.Instance);

    private IPlanningSequenceService BuildSequence() =>
        new PlanningSequenceService(
            _db,
            new PlanningSequenceGenerator(new BloomProgressionService()),
            new PlanningSequenceValidator(),
            BuildCoverage(),
            new SystemApplicationClock(),
            NullLogger<PlanningSequenceService>.Instance);

    private IPlanningSuggestionService BuildSuggestions(IPlanningCalendarService calendar, ICurriculumCoverageService coverage) =>
        new PlanningSuggestionService(
            _db, coverage, calendar, new SystemApplicationClock(),
            NullLogger<PlanningSuggestionService>.Instance);

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { /* ignore */ }
    }
}
