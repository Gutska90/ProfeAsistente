using AppEducativa.Api.Data;
using AppEducativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api.Repositories;

public interface IPlanificacionRepository
{
    Task<IReadOnlyList<Planificacion>> GetAllAsync(CancellationToken ct = default);
    Task<Planificacion?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Planificacion> AddAsync(Planificacion planificacion, CancellationToken ct = default);
    Task<Planificacion?> UpdateAsync(Planificacion planificacion, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IClaseRepository
{
    Task<Clase?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Clase> AddAsync(Clase clase, CancellationToken ct = default);
    Task<Clase?> UpdateAsync(Clase clase, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public class PlanificacionRepository : IPlanificacionRepository
{
    private readonly AppEducativaDbContext _db;

    public PlanificacionRepository(AppEducativaDbContext db) => _db = db;

    public async Task<IReadOnlyList<Planificacion>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Planificaciones
            .AsNoTracking()
            .Include(p => p.Nivel)
            .Include(p => p.NivelAsignatura)!.ThenInclude(n => n!.Asignatura)
            .Include(p => p.Unidad)
            .Include(p => p.Clases)
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync(ct);
    }

    public async Task<Planificacion?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Planificaciones
            .Include(p => p.Nivel)
            .Include(p => p.NivelAsignatura)!.ThenInclude(n => n!.Asignatura)
            .Include(p => p.Unidad)
            .Include(p => p.Clases.OrderBy(c => c.Numero))
                .ThenInclude(c => c.ObjetivoAprendizaje)
            .Include(p => p.Clases)
                .ThenInclude(c => c.Documentos)
            .Include(p => p.Clases)
                .ThenInclude(c => c.Indicadores)
                    .ThenInclude(i => i.IndicadorEvaluacion)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Planificacion> AddAsync(Planificacion planificacion, CancellationToken ct = default)
    {
        _db.Planificaciones.Add(planificacion);
        await _db.SaveChangesAsync(ct);
        return (await GetByIdAsync(planificacion.Id, ct))!;
    }

    public async Task<Planificacion?> UpdateAsync(Planificacion planificacion, CancellationToken ct = default)
    {
        var existing = await _db.Planificaciones.FirstOrDefaultAsync(p => p.Id == planificacion.Id, ct);
        if (existing is null) return null;

        existing.Nombre = planificacion.Nombre;
        existing.FechaInicio = planificacion.FechaInicio;
        existing.FechaFin = planificacion.FechaFin;
        existing.Estado = planificacion.Estado;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(existing.Id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _db.Planificaciones.FindAsync([id], ct);
        if (existing is null) return false;
        _db.Planificaciones.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class ClaseRepository : IClaseRepository
{
    private readonly AppEducativaDbContext _db;

    public ClaseRepository(AppEducativaDbContext db) => _db = db;

    public async Task<Clase?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Clases
            .Include(c => c.ObjetivoAprendizaje)!.ThenInclude(o => o!.Indicadores)
            .Include(c => c.Indicadores).ThenInclude(i => i.IndicadorEvaluacion)
            .Include(c => c.Documentos).ThenInclude(d => d.Items)
            .Include(c => c.Documentos).ThenInclude(d => d.ObjetivosSeleccionados)
            .Include(c => c.CurriculumSnapshot)
            .Include(c => c.Planificacion)!.ThenInclude(p => p!.Nivel)
            .Include(c => c.Planificacion)!.ThenInclude(p => p!.NivelAsignatura)!.ThenInclude(n => n!.Asignatura)
            .Include(c => c.Planificacion)!.ThenInclude(p => p!.Unidad)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Clase> AddAsync(Clase clase, CancellationToken ct = default)
    {
        _db.Clases.Add(clase);
        await _db.SaveChangesAsync(ct);
        return (await GetByIdAsync(clase.Id, ct))!;
    }

    public async Task<Clase?> UpdateAsync(Clase clase, CancellationToken ct = default)
    {
        var existing = await _db.Clases
            .Include(c => c.Indicadores)
            .FirstOrDefaultAsync(c => c.Id == clase.Id, ct);
        if (existing is null) return null;

        existing.Fecha = clase.Fecha;
        existing.ObjetivoAprendizajeId = clase.ObjetivoAprendizajeId;
        existing.NivelBloom = clase.NivelBloom;
        existing.DescripcionInicio = clase.DescripcionInicio;
        existing.DescripcionDesarrollo = clase.DescripcionDesarrollo;
        existing.DescripcionCierre = clase.DescripcionCierre;
        existing.Estado = clase.Estado;
        existing.Numero = clase.Numero;

        _db.ClaseIndicadores.RemoveRange(existing.Indicadores);
        existing.Indicadores = clase.Indicadores;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(existing.Id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _db.Clases.FindAsync([id], ct);
        if (existing is null) return false;
        _db.Clases.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
