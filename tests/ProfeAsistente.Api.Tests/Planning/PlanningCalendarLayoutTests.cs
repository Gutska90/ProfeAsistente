using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using ProfeAsistente.Shared.Planning;

namespace ProfeAsistente.Api.Tests.Planning;

public class PlanningCalendarLayoutTests
{
    [Fact]
    public void StartOfWeekMonday_FromWednesday_ReturnsMonday()
    {
        var wednesday = new DateOnly(2026, 3, 4);
        Assert.Equal(new DateOnly(2026, 3, 2), PlanningCalendarLayout.StartOfWeekMonday(wednesday));
    }

    [Fact]
    public void BuildMonth_Has42Cells_MondayFirst_CountsSessions()
    {
        var sessions = new List<PlanningCalendarSessionDto>
        {
            Session(1, new DateOnly(2026, 3, 2)),
            Session(2, new DateOnly(2026, 3, 2)),
            Session(3, new DateOnly(2026, 3, 4), PlanningSessionStatus.Cancelled)
        };

        var cells = PlanningCalendarLayout.BuildMonth(
            new DateOnly(2026, 3, 1), sessions, new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 2));

        Assert.Equal(42, cells.Count);
        Assert.Equal("L", cells[0].WeekdayShort);
        var monday = cells.First(c => c.Date == new DateOnly(2026, 3, 2));
        Assert.Equal(2, monday.SessionCount);
        Assert.True(monday.IsSelected);
        Assert.True(monday.IsToday);
        var wednesday = cells.First(c => c.Date == new DateOnly(2026, 3, 4));
        Assert.Equal(0, wednesday.SessionCount);
    }

    [Fact]
    public void BuildWeek_SevenDays_WithSpanishHeadline()
    {
        var sessions = new[]
        {
            new PlanningCalendarSessionDto
            {
                Id = Guid.NewGuid(),
                PlanningId = Guid.NewGuid(),
                ScheduledDate = new DateOnly(2026, 3, 2),
                StartTime = new TimeOnly(8, 0),
                SessionNumber = 1,
                Status = PlanningSessionStatus.Planned,
                ObjectiveCode = "OA 01"
            }
        };

        var week = PlanningCalendarLayout.BuildWeek(
            new DateOnly(2026, 3, 5), sessions, new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 1));

        Assert.Equal(7, week.Count);
        Assert.Equal("lunes", week[0].WeekdayName);
        Assert.Equal("domingo", week[6].WeekdayName);
        Assert.Contains("OA 01", week[0].Detail);
        Assert.Contains("Planificada", week[0].Detail);
        Assert.Contains("08:00", week[0].Detail);
    }

    private static PlanningCalendarSessionDto Session(int n, DateOnly date, PlanningSessionStatus status = PlanningSessionStatus.Planned)
        => new()
        {
            Id = Guid.NewGuid(),
            PlanningId = Guid.NewGuid(),
            ScheduledDate = date,
            SessionNumber = n,
            Status = status
        };
}
