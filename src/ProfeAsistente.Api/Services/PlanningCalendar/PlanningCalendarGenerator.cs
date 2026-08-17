using ProfeAsistente.Api.Models.Planning;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Services.PlanningCalendar;

public sealed class GeneratedSessionSlot
{
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public int DurationMinutes { get; init; }
    public int SessionNumber { get; set; }
}

public sealed class PlanningCalendarGenerator
{
    public IReadOnlyList<GeneratedSessionSlot> GenerateSlots(
        PlanningScheduleConfiguration configuration,
        IReadOnlyList<WeeklyClassSchedule> weekly,
        IReadOnlySet<DateOnly> excluded)
    {
        var active = weekly.Where(w => w.IsActive && w.SessionsPerDay > 0)
            .OrderBy(w => w.Order)
            .ThenBy(w => w.DayOfWeek)
            .ThenBy(w => w.StartTime)
            .ToList();

        var slots = new List<GeneratedSessionSlot>();
        for (var date = configuration.StartDate; date <= configuration.EndDate; date = date.AddDays(1))
        {
            if (excluded.Contains(date))
                continue;

            var daySlots = active.Where(w => w.DayOfWeek == date.DayOfWeek).ToList();
            foreach (var schedule in daySlots)
            {
                var start = schedule.StartTime;
                for (var i = 0; i < schedule.SessionsPerDay; i++)
                {
                    slots.Add(new GeneratedSessionSlot
                    {
                        Date = date,
                        StartTime = start.AddMinutes(i * schedule.DurationMinutes),
                        DurationMinutes = schedule.DurationMinutes
                    });
                }
            }
        }

        var numbered = slots
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select((s, idx) => new GeneratedSessionSlot
            {
                Date = s.Date,
                StartTime = s.StartTime,
                DurationMinutes = s.DurationMinutes,
                SessionNumber = idx + 1
            })
            .ToList();

        return numbered;
    }

    public PlanningCalendarRegenerationPreviewDto BuildPreview(
        IReadOnlyList<PlanningCalendarSession> existing,
        IReadOnlyList<GeneratedSessionSlot> proposed,
        Func<PlanningCalendarSession, SessionProtectionLevel> classify)
    {
        var protectedCount = 0;
        var conflictCount = 0;
        var removable = 0;
        var unchanged = 0;
        var messages = new List<string>();

        foreach (var session in existing)
        {
            var level = classify(session);
            switch (level)
            {
                case SessionProtectionLevel.Protected:
                    protectedCount++;
                    break;
                case SessionProtectionLevel.Conflict:
                    conflictCount++;
                    messages.Add($"Sesión {session.SessionNumber} ({session.ScheduledDate:yyyy-MM-dd}) tiene contenido y requiere decisión.");
                    break;
                case SessionProtectionLevel.Removable:
                    removable++;
                    break;
            }
        }

        // Match by date+time for unchanged estimate
        var existingKeys = existing
            .Where(s => s.Status != PlanningSessionStatus.Cancelled)
            .Select(s => (s.ScheduledDate, s.StartTime ?? default, s.DurationMinutes))
            .ToHashSet();
        unchanged = proposed.Count(p => existingKeys.Contains((p.Date, p.StartTime, p.DurationMinutes)));
        var newCount = Math.Max(0, proposed.Count - unchanged);
        var moved = Math.Max(0, existing.Count(s => s.Status != PlanningSessionStatus.Cancelled) - unchanged - protectedCount);

        return new PlanningCalendarRegenerationPreviewDto
        {
            NewSessions = newCount,
            UnchangedSessions = unchanged,
            MovedSessions = moved,
            RemovableSessions = removable,
            ConflictSessions = conflictCount,
            ProtectedSessions = protectedCount,
            Messages = messages,
            CanApplySafely = conflictCount == 0
        };
    }
}

public enum SessionProtectionLevel
{
    Removable,
    Movable,
    Conflict,
    Protected
}

public sealed class PlanningCalendarValidator
{
    public const int MaxRangeDays = 400;
    public const int MinDuration = 30;
    public const int MaxDuration = 240;

    public void ValidateConfigure(ConfigurePlanningScheduleRequest request)
    {
        if (request.EndDate < request.StartDate)
            throw new PlanningCalendarException("VALIDATION", "La fecha inicial debe ser menor o igual a la fecha final.");

        var days = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
        if (days > MaxRangeDays)
            throw new PlanningCalendarException("VALIDATION", $"El rango máximo permitido es {MaxRangeDays} días.");

        if (request.DefaultClassDurationMinutes is < MinDuration or > MaxDuration)
            throw new PlanningCalendarException("VALIDATION", $"La duración debe estar entre {MinDuration} y {MaxDuration} minutos.");

        if (request.WeeklySchedule.Count == 0 || request.WeeklySchedule.All(w => !w.IsActive))
            throw new PlanningCalendarException("VALIDATION", "Debe configurar al menos un día activo de clases.");

        foreach (var w in request.WeeklySchedule)
        {
            if (w.DurationMinutes is < MinDuration or > MaxDuration)
                throw new PlanningCalendarException("VALIDATION", $"Duración inválida para {w.DayOfWeek}.");
            if (w.SessionsPerDay is < 1 or > 5)
                throw new PlanningCalendarException("VALIDATION", "Las sesiones por día deben estar entre 1 y 5.");
        }

        DetectOverlaps(request.WeeklySchedule.Where(w => w.IsActive).ToList());

        var excluded = request.ExcludedDates.Select(x => x.Date).ToList();
        if (excluded.Count != excluded.Distinct().Count())
            throw new PlanningCalendarException("VALIDATION", "Hay fechas excluidas duplicadas.");

        foreach (var ex in request.ExcludedDates)
        {
            if (ex.Date < request.StartDate || ex.Date > request.EndDate)
                throw new PlanningCalendarException("VALIDATION", $"La fecha excluida {ex.Date:yyyy-MM-dd} está fuera del rango.");
        }
    }

    private static void DetectOverlaps(IReadOnlyList<WeeklyScheduleRequest> weekly)
    {
        foreach (var group in weekly.GroupBy(w => w.DayOfWeek))
        {
            var ordered = group.OrderBy(w => w.StartTime).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var prevEnd = prev.StartTime.AddMinutes(prev.DurationMinutes * prev.SessionsPerDay);
                if (ordered[i].StartTime < prevEnd)
                    throw new PlanningCalendarException("OVERLAP", $"Horarios superpuestos el {group.Key}.");
            }
        }
    }
}

public sealed class PlanningCalendarException : Exception
{
    public string ErrorCode { get; }

    public PlanningCalendarException(string code, string message) : base(message)
    {
        ErrorCode = code;
    }
}
