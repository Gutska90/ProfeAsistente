using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.Classroom;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services.Classroom;

public interface IClassDuaService
{
    Task<ClassDuaStrategyDto> AddDuaStrategyAsync(Guid classId, AddClassDuaStrategyRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassDuaStrategyDto>> ListDuaAsync(Guid classId, CancellationToken cancellationToken = default);
}

public sealed class ClassDuaService : IClassDuaService
{
    private readonly ProfeAsistenteDbContext _db;
    private readonly ClassroomAccess _access;

    public ClassDuaService(ProfeAsistenteDbContext db, ClassroomAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task<ClassDuaStrategyDto> AddDuaStrategyAsync(Guid classId, AddClassDuaStrategyRequest request, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomSupportPlans, AppPermissions.PlanningUpdateOwn);
        var entity = new ClassDuaStrategy
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            Principle = request.Principle,
            Strategy = request.Strategy.Trim(),
            Notes = request.Notes
        };
        _db.ClassDuaStrategies.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new ClassDuaStrategyDto { Id = entity.Id, Principle = entity.Principle, Strategy = entity.Strategy };
    }

    public async Task<IReadOnlyList<ClassDuaStrategyDto>> ListDuaAsync(Guid classId, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomView);
        return await _db.ClassDuaStrategies.AsNoTracking()
            .Where(d => d.ClassId == classId)
            .Select(d => new ClassDuaStrategyDto { Id = d.Id, Principle = d.Principle, Strategy = d.Strategy })
            .ToListAsync(cancellationToken);
    }
}
