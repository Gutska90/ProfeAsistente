using System.Text.Json;
using AppEducativa.Api.Data;
using AppEducativa.Api.Models;
using AppEducativa.Api.Models.AI;
using AppEducativa.Api.Models.Planning;
using AppEducativa.Api.Services.DateTimeServices;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api.Services.PlanningCalendar;

public interface IPlanningCalendarService
{
    Task<PlanningCalendarDto> ConfigureAsync(Guid planningId, ConfigurePlanningScheduleRequest request, CancellationToken cancellationToken = default);
    Task<PlanningCalendarDto> GenerateSessionsAsync(Guid planningId, GenerateCalendarSessionsRequest request, CancellationToken cancellationToken = default);
    Task<PlanningCalendarDto> PreviewRegenerationAsync(Guid planningId, CancellationToken cancellationToken = default);
    Task<PlanningCalendarDto?> GetCalendarAsync(Guid planningId, CancellationToken cancellationToken = default);
    Task<PlanningCalendarSessionDto> AddManualSessionAsync(Guid planningId, CreateManualSessionRequest request, CancellationToken cancellationToken = default);
    Task<PlanningCalendarSessionDto> RescheduleSessionAsync(Guid sessionId, RescheduleSessionRequest request, CancellationToken cancellationToken = default);
    Task CancelSessionAsync(Guid sessionId, CancelPlanningSessionRequest request, CancellationToken cancellationToken = default);
    Task RestoreSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task LockSessionAsync(Guid sessionId, LockSessionRequest request, CancellationToken cancellationToken = default);
    Task UnlockSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task UpdateSessionAsync(Guid sessionId, CreateManualSessionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetConflictsAsync(Guid planningId, CancellationToken cancellationToken = default);
    Task ImportExcludedDatesAsync(Guid planningId, ImportExcludedDatesRequest request, CancellationToken cancellationToken = default);
}

public sealed class PlanningCalendarService : IPlanningCalendarService
{
    private readonly AppEducativaDbContext _db;
    private readonly PlanningCalendarGenerator _generator;
    private readonly PlanningCalendarValidator _validator;
    private readonly ITimeZoneService _timeZones;
    private readonly IApplicationClock _clock;
    private readonly ILogger<PlanningCalendarService> _logger;

    public PlanningCalendarService(
        AppEducativaDbContext db,
        PlanningCalendarGenerator generator,
        PlanningCalendarValidator validator,
        ITimeZoneService timeZones,
        IApplicationClock clock,
        ILogger<PlanningCalendarService> logger)
    {
        _db = db;
        _generator = generator;
        _validator = validator;
        _timeZones = timeZones;
        _clock = clock;
        _logger = logger;
    }

    public async Task<PlanningCalendarDto> ConfigureAsync(Guid planningId, ConfigurePlanningScheduleRequest request, CancellationToken cancellationToken = default)
    {
        _validator.ValidateConfigure(request);
        if (!_timeZones.TryResolve(request.TimeZoneId, out _))
            throw new PlanningCalendarException("TIMEZONE", $"Zona horaria no válida: {request.TimeZoneId}");

        var plan = await _db.Planificaciones.FirstOrDefaultAsync(p => p.Id == planningId, cancellationToken)
            ?? throw new PlanningCalendarException("NOT_FOUND", "Planificación no encontrada.");

        if (request.UpdatePlanningDates)
        {
            plan.FechaInicio = request.StartDate;
            plan.FechaFin = request.EndDate;
        }

        var config = await _db.PlanningScheduleConfigurations
            .Include(c => c.WeeklySchedules)
            .Include(c => c.ExcludedDates)
            .FirstOrDefaultAsync(c => c.PlanningId == planningId, cancellationToken);

        if (config is null)
        {
            config = new PlanningScheduleConfiguration { Id = Guid.NewGuid(), PlanningId = planningId };
            _db.PlanningScheduleConfigurations.Add(config);
        }
        else
        {
            _db.WeeklyClassSchedules.RemoveRange(config.WeeklySchedules);
            _db.PlanningExcludedDates.RemoveRange(config.ExcludedDates);
        }

        config.TimeZoneId = _timeZones.NormalizeId(request.TimeZoneId);
        config.DefaultClassDurationMinutes = request.DefaultClassDurationMinutes;
        config.StartDate = request.StartDate;
        config.EndDate = request.EndDate;
        config.UpdatedAt = _clock.UtcNow;

        var order = 0;
        foreach (var w in request.WeeklySchedule.Where(x => x.IsActive))
        {
            _db.WeeklyClassSchedules.Add(new WeeklyClassSchedule
            {
                Id = Guid.NewGuid(),
                PlanningScheduleConfigurationId = config.Id,
                DayOfWeek = w.DayOfWeek,
                StartTime = w.StartTime,
                DurationMinutes = w.DurationMinutes > 0 ? w.DurationMinutes : request.DefaultClassDurationMinutes,
                SessionsPerDay = w.SessionsPerDay,
                IsActive = true,
                Order = order++
            });
        }

        foreach (var ex in request.ExcludedDates)
        {
            _db.PlanningExcludedDates.Add(new PlanningExcludedDate
            {
                Id = Guid.NewGuid(),
                PlanningScheduleConfigurationId = config.Id,
                Date = ex.Date,
                Reason = Truncate(ex.Reason, 200),
                ExclusionType = ex.ExclusionType,
                IsRecurring = ex.IsRecurring,
                CreatedAt = _clock.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ScheduleConfigured planning={PlanningId}", planningId);
        return (await GetCalendarAsync(planningId, cancellationToken))!;
    }

    public async Task<PlanningCalendarDto> PreviewRegenerationAsync(Guid planningId, CancellationToken cancellationToken = default)
    {
        var (config, weekly, excluded, slots) = await BuildSlotsAsync(planningId, cancellationToken);
        var existing = await _db.PlanningCalendarSessions
            .Where(s => s.PlanningId == planningId)
            .ToListAsync(cancellationToken);

        var preview = _generator.BuildPreview(existing, slots, Classify);
        var dto = await MapCalendarAsync(planningId, config, weekly, excluded, existing, cancellationToken);
        dto.Preview = preview;
        dto.Warnings = preview.Messages;
        return dto;
    }

    public async Task<PlanningCalendarDto> GenerateSessionsAsync(Guid planningId, GenerateCalendarSessionsRequest request, CancellationToken cancellationToken = default)
    {
        var (config, weekly, excluded, slots) = await BuildSlotsAsync(planningId, cancellationToken);
        var existing = await _db.PlanningCalendarSessions
            .Include(s => s.Class)
            .Where(s => s.PlanningId == planningId)
            .ToListAsync(cancellationToken);

        var preview = _generator.BuildPreview(existing, slots, Classify);
        if (request.PreviewOnly)
        {
            var previewDto = await MapCalendarAsync(planningId, config, weekly, excluded, existing, cancellationToken);
            previewDto.Preview = preview;
            previewDto.Warnings = preview.Messages;
            return previewDto;
        }

        if (preview.ConflictSessions > 0 && !request.ConfirmDestructiveChanges)
            throw new PlanningCalendarException("CONFLICT", "Hay sesiones con contenido. Confirme la regeneración o resuelva conflictos.");

        // Keep protected and conflict sessions; remove removable automatic sessions without class content
        foreach (var session in existing.ToList())
        {
            var level = Classify(session);
            if (level == SessionProtectionLevel.Protected && request.PreserveLockedSessions)
                continue;
            if (level == SessionProtectionLevel.Conflict)
                continue;
            if (session.Source == PlanningSessionSource.Manual && request.PreserveManualSessions)
                continue;
            if (level == SessionProtectionLevel.Removable)
            {
                _db.PlanningCalendarSessions.Remove(session);
                existing.Remove(session);
            }
        }

        var keepKeys = existing
            .Where(s => s.Status != PlanningSessionStatus.Cancelled)
            .Select(s => (s.ScheduledDate, s.StartTime ?? default))
            .ToHashSet();

        foreach (var slot in slots)
        {
            if (keepKeys.Contains((slot.Date, slot.StartTime)))
                continue;

            _db.PlanningCalendarSessions.Add(new PlanningCalendarSession
            {
                Id = Guid.NewGuid(),
                PlanningId = planningId,
                ScheduledDate = slot.Date,
                StartTime = slot.StartTime,
                DurationMinutes = slot.DurationMinutes,
                SessionNumber = 0,
                Status = PlanningSessionStatus.Available,
                Source = PlanningSessionSource.Automatic,
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await RenumberSessionsAsync(planningId, cancellationToken);
        _logger.LogInformation("CalendarSessionsGenerated planning={PlanningId}", planningId);
        return (await GetCalendarAsync(planningId, cancellationToken))!;
    }

    public async Task<PlanningCalendarDto?> GetCalendarAsync(Guid planningId, CancellationToken cancellationToken = default)
    {
        var planExists = await _db.Planificaciones.AnyAsync(p => p.Id == planningId, cancellationToken);
        if (!planExists) return null;

        var config = await _db.PlanningScheduleConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PlanningId == planningId, cancellationToken);

        List<WeeklyClassSchedule> weekly = [];
        List<PlanningExcludedDate> excluded = [];
        if (config is not null)
        {
            weekly = await _db.WeeklyClassSchedules.AsNoTracking()
                .Where(w => w.PlanningScheduleConfigurationId == config.Id).OrderBy(w => w.Order).ToListAsync(cancellationToken);
            excluded = await _db.PlanningExcludedDates.AsNoTracking()
                .Where(e => e.PlanningScheduleConfigurationId == config.Id).OrderBy(e => e.Date).ToListAsync(cancellationToken);
        }

        var sessions = await _db.PlanningCalendarSessions.AsNoTracking()
            .Where(s => s.PlanningId == planningId)
            .OrderBy(s => s.SessionNumber)
            .ToListAsync(cancellationToken);

        return await MapCalendarAsync(planningId, config, weekly, excluded, sessions, cancellationToken);
    }

    public async Task<PlanningCalendarSessionDto> AddManualSessionAsync(Guid planningId, CreateManualSessionRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _db.Planificaciones.FirstOrDefaultAsync(p => p.Id == planningId, cancellationToken)
            ?? throw new PlanningCalendarException("NOT_FOUND", "Planificación no encontrada.");

        if (request.ScheduledDate < plan.FechaInicio || request.ScheduledDate > plan.FechaFin)
            throw new PlanningCalendarException("OUT_OF_RANGE", "La sesión está fuera del rango de la planificación.");

        var session = new PlanningCalendarSession
        {
            Id = Guid.NewGuid(),
            PlanningId = planningId,
            ScheduledDate = request.ScheduledDate,
            StartTime = request.StartTime,
            DurationMinutes = request.DurationMinutes,
            Status = PlanningSessionStatus.Available,
            Source = PlanningSessionSource.Manual,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };
        _db.PlanningCalendarSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
        await RenumberSessionsAsync(planningId, cancellationToken);
        _logger.LogInformation("SessionAdded planning={PlanningId} session={SessionId}", planningId, session.Id);
        var refreshed = await _db.PlanningCalendarSessions.AsNoTracking().FirstAsync(s => s.Id == session.Id, cancellationToken);
        return MapSession(refreshed, null, null, null, null);
    }

    public async Task UpdateSessionAsync(Guid sessionId, CreateManualSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        if (session.IsLocked)
            throw new PlanningCalendarException("LOCKED", "La sesión está bloqueada.");
        session.ScheduledDate = request.ScheduledDate;
        session.StartTime = request.StartTime;
        session.DurationMinutes = request.DurationMinutes;
        session.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await RenumberSessionsAsync(session.PlanningId, cancellationToken);
    }

    public async Task<PlanningCalendarSessionDto> RescheduleSessionAsync(Guid sessionId, RescheduleSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        if (session.IsLocked)
            throw new PlanningCalendarException("LOCKED", "La sesión está bloqueada.");

        var plan = await _db.Planificaciones.FirstAsync(p => p.Id == session.PlanningId, cancellationToken);
        if (request.NewDate < plan.FechaInicio || request.NewDate > plan.FechaFin)
            throw new PlanningCalendarException("OUT_OF_RANGE", "Nueva fecha fuera de rango.");

        _db.PlanningSessionHistories.Add(new PlanningSessionHistory
        {
            Id = Guid.NewGuid(),
            PlanningCalendarSessionId = session.Id,
            PreviousDate = session.ScheduledDate,
            NewDate = request.NewDate,
            PreviousStartTime = session.StartTime,
            NewStartTime = request.NewStartTime ?? session.StartTime,
            Reason = request.Reason,
            ChangedAt = _clock.UtcNow
        });

        session.ScheduledDate = request.NewDate;
        session.StartTime = request.NewStartTime ?? session.StartTime;
        session.Status = session.Status == PlanningSessionStatus.Cancelled
            ? PlanningSessionStatus.Rescheduled
            : session.Status;
        if (session.Status == PlanningSessionStatus.Rescheduled && session.ClassId is not null)
            session.Status = PlanningSessionStatus.Assigned;
        session.UpdatedAt = _clock.UtcNow;

        if (session.ClassId is Guid classId)
        {
            var clase = await _db.Clases.FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
            if (clase is not null)
            {
                clase.Fecha = request.NewDate;
                clase.StartTime = session.StartTime;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await RenumberSessionsAsync(session.PlanningId, cancellationToken);
        _logger.LogInformation("SessionRescheduled session={SessionId}", sessionId);
        var refreshed = await _db.PlanningCalendarSessions.AsNoTracking().FirstAsync(s => s.Id == sessionId, cancellationToken);
        return MapSession(refreshed, null, null, null, null);
    }

    public async Task CancelSessionAsync(Guid sessionId, CancelPlanningSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        if (session.IsLocked)
            throw new PlanningCalendarException("LOCKED", "La sesión está bloqueada.");
        session.Status = PlanningSessionStatus.Cancelled;
        session.CancelReason = Truncate(request.Reason, 300);
        session.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("SessionCancelled session={SessionId}", sessionId);
    }

    public async Task RestoreSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        session.Status = session.ClassId is null ? PlanningSessionStatus.Available : PlanningSessionStatus.Assigned;
        session.CancelReason = null;
        session.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("SessionRestored session={SessionId}", sessionId);
    }

    public async Task LockSessionAsync(Guid sessionId, LockSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        session.IsLocked = true;
        session.LockReason = Truncate(request.LockReason, 200);
        session.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("SessionLocked session={SessionId}", sessionId);
    }

    public async Task UnlockSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        session.IsLocked = false;
        session.LockReason = null;
        session.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetConflictsAsync(Guid planningId, CancellationToken cancellationToken = default)
    {
        var sessions = await _db.PlanningCalendarSessions.AsNoTracking()
            .Where(s => s.PlanningId == planningId && s.Status != PlanningSessionStatus.Cancelled)
            .OrderBy(s => s.ScheduledDate).ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        var conflicts = new List<string>();
        for (var i = 1; i < sessions.Count; i++)
        {
            var prev = sessions[i - 1];
            var cur = sessions[i];
            if (prev.ScheduledDate != cur.ScheduledDate || prev.StartTime is null || cur.StartTime is null)
                continue;
            var prevEnd = prev.StartTime.Value.AddMinutes(prev.DurationMinutes);
            if (cur.StartTime < prevEnd)
                conflicts.Add($"Superposición entre sesión {prev.SessionNumber} y {cur.SessionNumber} el {cur.ScheduledDate:yyyy-MM-dd}.");
        }

        return conflicts;
    }

    public async Task ImportExcludedDatesAsync(Guid planningId, ImportExcludedDatesRequest request, CancellationToken cancellationToken = default)
    {
        var config = await _db.PlanningScheduleConfigurations
            .Include(c => c.ExcludedDates)
            .FirstOrDefaultAsync(c => c.PlanningId == planningId, cancellationToken)
            ?? throw new PlanningCalendarException("NOT_FOUND", "Configure el horario antes de importar exclusiones.");

        var existing = config.ExcludedDates.Select(e => e.Date).ToHashSet();
        foreach (var d in request.Dates)
        {
            if (existing.Contains(d.Date)) continue;
            if (d.Date < config.StartDate || d.Date > config.EndDate) continue;
            _db.PlanningExcludedDates.Add(new PlanningExcludedDate
            {
                Id = Guid.NewGuid(),
                PlanningScheduleConfigurationId = config.Id,
                Date = d.Date,
                Reason = Truncate(d.Reason, 200),
                ExclusionType = d.ExclusionType,
                IsRecurring = d.IsRecurring,
                CreatedAt = _clock.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<(PlanningScheduleConfiguration config, List<WeeklyClassSchedule> weekly, List<PlanningExcludedDate> excluded, IReadOnlyList<GeneratedSessionSlot> slots)>
        BuildSlotsAsync(Guid planningId, CancellationToken cancellationToken)
    {
        var config = await _db.PlanningScheduleConfigurations
            .FirstOrDefaultAsync(c => c.PlanningId == planningId, cancellationToken)
            ?? throw new PlanningCalendarException("NOT_CONFIGURED", "Configure el horario antes de generar sesiones.");

        var weekly = await _db.WeeklyClassSchedules
            .Where(w => w.PlanningScheduleConfigurationId == config.Id).ToListAsync(cancellationToken);
        var excluded = await _db.PlanningExcludedDates
            .Where(e => e.PlanningScheduleConfigurationId == config.Id).ToListAsync(cancellationToken);
        var slots = _generator.GenerateSlots(config, weekly, excluded.Select(e => e.Date).ToHashSet());
        return (config, weekly, excluded, slots);
    }

    private async Task RenumberSessionsAsync(Guid planningId, CancellationToken cancellationToken)
    {
        var sessions = await _db.PlanningCalendarSessions
            .Where(s => s.PlanningId == planningId)
            .OrderBy(s => s.ScheduledDate)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        var n = 1;
        foreach (var s in sessions.Where(x => x.Status != PlanningSessionStatus.Cancelled))
        {
            s.SessionNumber = n++;
            s.UpdatedAt = _clock.UtcNow;
        }

        foreach (var s in sessions.Where(x => x.Status == PlanningSessionStatus.Cancelled))
            s.SessionNumber = 0;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<PlanningCalendarSession> LoadSessionAsync(Guid sessionId, CancellationToken cancellationToken) =>
        await _db.PlanningCalendarSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
        ?? throw new PlanningCalendarException("NOT_FOUND", "Sesión no encontrada.");

    private SessionProtectionLevel Classify(PlanningCalendarSession session)
    {
        if (session.IsLocked || session.Status == PlanningSessionStatus.Completed)
            return SessionProtectionLevel.Protected;

        if (session.ClassId is null)
            return SessionProtectionLevel.Removable;

        var clase = session.Class ?? _db.Clases.AsNoTracking().FirstOrDefault(c => c.Id == session.ClassId);
        if (clase is null)
            return SessionProtectionLevel.Removable;

        if (clase.Estado == EstadoClase.Realizada)
            return SessionProtectionLevel.Protected;

        var hasStructure = !string.IsNullOrWhiteSpace(clase.DescripcionInicio)
                           || !string.IsNullOrWhiteSpace(clase.DescripcionDesarrollo)
                           || !string.IsNullOrWhiteSpace(clase.DescripcionCierre)
                           || _db.ClassStructureGenerations.AsNoTracking().Any(g => g.ClassId == clase.Id);
        var hasMaterials = _db.EducationalDocuments.AsNoTracking().Any(d => d.ClassId == clase.Id)
                           || _db.Documentos.AsNoTracking().Any(d => d.ClaseId == clase.Id);

        if (hasStructure || hasMaterials)
            return SessionProtectionLevel.Conflict;

        return SessionProtectionLevel.Movable;
    }

    private async Task<PlanningCalendarDto> MapCalendarAsync(
        Guid planningId,
        PlanningScheduleConfiguration? config,
        IReadOnlyList<WeeklyClassSchedule> weekly,
        IReadOnlyList<PlanningExcludedDate> excluded,
        IReadOnlyList<PlanningCalendarSession> sessions,
        CancellationToken cancellationToken)
    {
        var classIds = sessions.Where(s => s.ClassId.HasValue).Select(s => s.ClassId!.Value).ToList();
        var classes = await _db.Clases.AsNoTracking()
            .Include(c => c.ObjetivoAprendizaje)
            .Where(c => classIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        PlanningScheduleConfigurationDto? configDto = null;
        if (config is not null)
        {
            configDto = new PlanningScheduleConfigurationDto
            {
                Id = config.Id,
                PlanningId = planningId,
                TimeZoneId = config.TimeZoneId,
                DefaultClassDurationMinutes = config.DefaultClassDurationMinutes,
                StartDate = config.StartDate,
                EndDate = config.EndDate,
                WeeklySchedule = weekly.Select(w => new WeeklyScheduleDto
                {
                    Id = w.Id,
                    DayOfWeek = w.DayOfWeek,
                    StartTime = w.StartTime,
                    DurationMinutes = w.DurationMinutes,
                    SessionsPerDay = w.SessionsPerDay,
                    IsActive = w.IsActive
                }).ToList(),
                ExcludedDates = excluded.Select(e => new ExcludedDateDto
                {
                    Id = e.Id,
                    Date = e.Date,
                    Reason = e.Reason,
                    ExclusionType = e.ExclusionType
                }).ToList()
            };
        }

        var sessionDtos = sessions.Select(s =>
        {
            classes.TryGetValue(s.ClassId ?? Guid.Empty, out var clase);
            return MapSession(s, clase?.Titulo, clase?.ObjetivoAprendizaje?.Codigo, clase?.NivelBloom, clase?.ClassType);
        }).ToList();

        return new PlanningCalendarDto
        {
            PlanningId = planningId,
            Configuration = configDto,
            Sessions = sessionDtos,
            AvailableSessionCount = sessionDtos.Count(s => s.Status is PlanningSessionStatus.Available or PlanningSessionStatus.Assigned or PlanningSessionStatus.Planned),
            AssignedSessionCount = sessionDtos.Count(s => s.ClassId.HasValue),
            CancelledSessionCount = sessionDtos.Count(s => s.Status == PlanningSessionStatus.Cancelled)
        };
    }

    private static PlanningCalendarSessionDto MapSession(
        PlanningCalendarSession s,
        string? title,
        string? oaCode,
        string? bloom,
        PlanningClassType? classType) =>
        new()
        {
            Id = s.Id,
            PlanningId = s.PlanningId,
            ScheduledDate = s.ScheduledDate,
            StartTime = s.StartTime,
            DurationMinutes = s.DurationMinutes,
            SessionNumber = s.SessionNumber,
            Status = s.Status,
            Source = s.Source,
            ClassId = s.ClassId,
            IsLocked = s.IsLocked,
            LockReason = s.LockReason,
            Title = title,
            ObjectiveCode = oaCode,
            BloomLevel = bloom,
            ClassType = classType,
            RowVersion = s.RowVersion
        };

    private static string Truncate(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : (value.Length <= max ? value : value[..max]);
}
