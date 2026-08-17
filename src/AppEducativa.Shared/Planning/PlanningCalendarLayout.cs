using System.Globalization;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;

namespace AppEducativa.Shared.Planning;

public sealed class CalendarDayCell
{
    public DateOnly Date { get; init; }
    public int DayNumber { get; init; }
    public string WeekdayShort { get; init; } = string.Empty;
    public string WeekdayName { get; init; } = string.Empty;
    public bool IsCurrentMonth { get; init; }
    public bool IsToday { get; init; }
    public bool IsSelected { get; init; }
    public bool HasSessions { get; init; }
    public int SessionCount { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
}

public static class PlanningCalendarLayout
{
    public static readonly string[] WeekdayHeaders = ["L", "M", "X", "J", "V", "S", "D"];
    private static readonly string[] WeekdayNames =
        ["lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo"];

    public static DateOnly StartOfWeekMonday(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    public static IReadOnlyList<CalendarDayCell> BuildMonth(
        DateOnly month,
        IReadOnlyList<PlanningCalendarSessionDto> sessions,
        DateOnly selected,
        DateOnly today)
    {
        var first = new DateOnly(month.Year, month.Month, 1);
        var start = StartOfWeekMonday(first);
        var byDate = GroupActive(sessions);
        var cells = new List<CalendarDayCell>(42);
        for (var i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            cells.Add(BuildCell(date, byDate, selected, today, date.Month == month.Month));
        }

        return cells;
    }

    public static IReadOnlyList<CalendarDayCell> BuildWeek(
        DateOnly anyDayInWeek,
        IReadOnlyList<PlanningCalendarSessionDto> sessions,
        DateOnly selected,
        DateOnly today)
    {
        var start = StartOfWeekMonday(anyDayInWeek);
        var byDate = GroupActive(sessions);
        return Enumerable.Range(0, 7)
            .Select(i =>
            {
                var date = start.AddDays(i);
                return BuildCell(date, byDate, selected, today, isCurrentMonth: true);
            })
            .ToList();
    }

    public static string StatusLabel(PlanningSessionStatus status) => status switch
    {
        PlanningSessionStatus.Available => "Disponible",
        PlanningSessionStatus.Assigned => "Con clase",
        PlanningSessionStatus.Planned => "Planificada",
        PlanningSessionStatus.Completed => "Realizada",
        PlanningSessionStatus.Cancelled => "Cancelada",
        PlanningSessionStatus.Rescheduled => "Reprogramada",
        PlanningSessionStatus.Excluded => "Excluida",
        _ => status.ToString()
    };

    public static string SessionHeadline(PlanningCalendarSessionDto session)
    {
        var time = session.StartTime is TimeOnly t ? t.ToString("HH:mm") : "sin hora";
        var oa = string.IsNullOrWhiteSpace(session.ObjectiveCode) ? "sin OA" : session.ObjectiveCode;
        return $"#{session.SessionNumber} · {time} · {oa} · {StatusLabel(session.Status)}";
    }

    public static string MonthTitle(DateOnly month)
        => month.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", new CultureInfo("es-CL"));

    public static string WeekTitle(DateOnly anyDayInWeek)
    {
        var start = StartOfWeekMonday(anyDayInWeek);
        var end = start.AddDays(6);
        var culture = new CultureInfo("es-CL");
        if (start.Month == end.Month)
            return $"{start.Day} – {end.Day} {start.ToDateTime(TimeOnly.MinValue).ToString("MMM yyyy", culture)}";
        return $"{start.Day} {start.ToDateTime(TimeOnly.MinValue).ToString("MMM", culture)} – {end.Day} {end.ToDateTime(TimeOnly.MinValue).ToString("MMM yyyy", culture)}";
    }

    private static Dictionary<DateOnly, List<PlanningCalendarSessionDto>> GroupActive(
        IReadOnlyList<PlanningCalendarSessionDto> sessions)
        => sessions
            .Where(s => s.Status is not PlanningSessionStatus.Cancelled and not PlanningSessionStatus.Excluded)
            .GroupBy(s => s.ScheduledDate)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.StartTime).ThenBy(x => x.SessionNumber).ToList());

    private static CalendarDayCell BuildCell(
        DateOnly date,
        IReadOnlyDictionary<DateOnly, List<PlanningCalendarSessionDto>> byDate,
        DateOnly selected,
        DateOnly today,
        bool isCurrentMonth)
    {
        byDate.TryGetValue(date, out var list);
        list ??= [];
        var weekdayIndex = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return new CalendarDayCell
        {
            Date = date,
            DayNumber = date.Day,
            WeekdayShort = WeekdayHeaders[weekdayIndex],
            WeekdayName = WeekdayNames[weekdayIndex],
            IsCurrentMonth = isCurrentMonth,
            IsToday = date == today,
            IsSelected = date == selected,
            HasSessions = list.Count > 0,
            SessionCount = list.Count,
            Summary = list.Count == 0 ? string.Empty : list.Count == 1 ? "1 clase" : $"{list.Count} clases",
            Detail = string.Join("\n", list.Select(SessionHeadline))
        };
    }
}
