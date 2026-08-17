using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.Classroom;
using ProfeAsistente.Api.Services.Authorization;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services.Classroom;

public interface IStudentSupportService
{
    Task<SupportPlanDto> AddSupportPlanAsync(Guid studentId, CreateSupportPlanRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportPlanDto>> ListSupportPlansAsync(Guid studentId, CancellationToken cancellationToken = default);
}

public sealed class StudentSupportService : IStudentSupportService
{
    private readonly ProfeAsistenteDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly ClassroomAccess _access;

    public StudentSupportService(ProfeAsistenteDbContext db, ICurrentUserService current, ClassroomAccess access)
    {
        _db = db;
        _current = current;
        _access = access;
    }

    public async Task<SupportPlanDto> AddSupportPlanAsync(Guid studentId, CreateSupportPlanRequest request, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomSupportPlans);
        var student = await _db.Students.FirstAsync(s => s.Id == studentId && !s.IsDeleted, cancellationToken);
        _access.EnsureInstitution(student.InstitutionId);
        var plan = new StudentSupportPlan
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            InstitutionId = student.InstitutionId,
            PlanType = request.PlanType,
            NeedType = request.NeedType,
            Title = request.Title.Trim(),
            Strategies = request.Strategies.Trim(),
            AccessAdjustments = request.AccessAdjustments,
            ObjectiveAdjustments = request.ObjectiveAdjustments,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedByUserId = _current.UserId
        };
        _db.StudentSupportPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);
        return MapPlan(plan);
    }

    public async Task<IReadOnlyList<SupportPlanDto>> ListSupportPlansAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomView);
        var list = await _db.StudentSupportPlans.AsNoTracking()
            .Where(p => p.StudentId == studentId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        return list.Select(MapPlan).ToList();
    }

    private static SupportPlanDto MapPlan(StudentSupportPlan p) => new()
    {
        Id = p.Id,
        StudentId = p.StudentId,
        PlanType = p.PlanType,
        NeedType = p.NeedType,
        Title = p.Title,
        Strategies = p.Strategies,
        AccessAdjustments = p.AccessAdjustments,
        ObjectiveAdjustments = p.ObjectiveAdjustments,
        IsActive = p.IsActive
    };
}
