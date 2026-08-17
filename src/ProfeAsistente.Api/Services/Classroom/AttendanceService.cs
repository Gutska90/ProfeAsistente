using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.Classroom;
using ProfeAsistente.Api.Services.Authorization;
using ProfeAsistente.Api.Services.DateTimeServices;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services.Classroom;

public interface IAttendanceService
{
    Task SaveAttendanceAsync(Guid classId, SaveAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(Guid classId, CancellationToken cancellationToken = default);
}

public sealed class AttendanceService : IAttendanceService
{
    private readonly ProfeAsistenteDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly ClassroomAccess _access;
    private readonly IApplicationClock _clock;

    public AttendanceService(
        ProfeAsistenteDbContext db,
        ICurrentUserService current,
        ClassroomAccess access,
        IApplicationClock clock)
    {
        _db = db;
        _current = current;
        _access = access;
        _clock = clock;
    }

    public async Task SaveAttendanceAsync(Guid classId, SaveAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomAttendance);
        var existing = await _db.AttendanceRecords.Where(a => a.ClassId == classId).ToListAsync(cancellationToken);
        _db.AttendanceRecords.RemoveRange(existing);
        var recordedAt = _clock.UtcNow;
        foreach (var e in request.Entries)
        {
            _db.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                StudentId = e.StudentId,
                Status = e.Status,
                Justification = e.Justification,
                RecordedByUserId = _current.UserId,
                RecordedAt = recordedAt
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(Guid classId, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomView);
        return await _db.AttendanceRecords.AsNoTracking()
            .Where(a => a.ClassId == classId)
            .Join(_db.Students, a => a.StudentId, s => s.Id, (a, s) => new AttendanceRecordDto
            {
                StudentId = a.StudentId,
                StudentName = s.DisplayName,
                Status = a.Status,
                Justification = a.Justification
            }).ToListAsync(cancellationToken);
    }
}
