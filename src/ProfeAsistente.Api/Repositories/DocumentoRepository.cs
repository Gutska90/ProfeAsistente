using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Repositories;

public interface IDocumentoRepository
{
    Task<IReadOnlyList<Documento>> GetAllAsync(CancellationToken ct = default);
    Task<Documento?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Documento> AddAsync(Documento documento, CancellationToken ct = default);
    Task<Documento?> UpdateAsync(Documento documento, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public class DocumentoRepository : IDocumentoRepository
{
    private readonly ProfeAsistenteDbContext _db;

    public DocumentoRepository(ProfeAsistenteDbContext db) => _db = db;

    public async Task<IReadOnlyList<Documento>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Documentos
            .AsNoTracking()
            .Include(d => d.Items.OrderBy(i => i.Orden))
            .Include(d => d.Sesiones.OrderBy(s => s.Numero))
            .Include(d => d.ObjetivosSeleccionados)
            .OrderByDescending(d => d.FechaCreacion)
            .ToListAsync(ct);
    }

    public async Task<Documento?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Documentos
            .Include(d => d.Items.OrderBy(i => i.Orden))
                .ThenInclude(i => i.IndicadorEvaluacion)
            .Include(d => d.Sesiones.OrderBy(s => s.Numero))
            .Include(d => d.ObjetivosSeleccionados)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<Documento> AddAsync(Documento documento, CancellationToken ct = default)
    {
        _db.Documentos.Add(documento);
        await _db.SaveChangesAsync(ct);
        return documento;
    }

    public async Task<Documento?> UpdateAsync(Documento documento, CancellationToken ct = default)
    {
        var existing = await _db.Documentos
            .Include(d => d.Items)
            .Include(d => d.Sesiones)
            .FirstOrDefaultAsync(d => d.Id == documento.Id, ct);

        if (existing is null)
            return null;

        existing.Tema = documento.Tema;
        existing.ObjetivoAprendizaje = documento.ObjetivoAprendizaje;
        existing.ContenidoGenerado = documento.ContenidoGenerado;
        existing.Instrucciones = documento.Instrucciones;
        existing.Estado = documento.Estado;

        _db.Items.RemoveRange(existing.Items);
        existing.Items = documento.Items;

        _db.SesionesPlanificadas.RemoveRange(existing.Sesiones);
        existing.Sesiones = documento.Sesiones;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(existing.Id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _db.Documentos.FindAsync([id], ct);
        if (existing is null)
            return false;
        _db.Documentos.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
