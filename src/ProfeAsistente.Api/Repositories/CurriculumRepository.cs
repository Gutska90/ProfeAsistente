using ProfeAsistente.Api.Data;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Repositories;

public interface ICurriculumRepository
{
    Task<IReadOnlyList<NivelDto>> GetNivelesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AsignaturaDto>> GetAsignaturasAsync(Guid nivelId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnidadDto>> GetUnidadesAsync(Guid nivelAsignaturaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ObjetivoAprendizajeDto>> GetObjetivosPorUnidadAsync(Guid unidadId, CancellationToken cancellationToken = default);
    Task<ObjetivoAprendizajeDetalleDto?> GetObjetivoDetalleAsync(Guid objetivoId, CancellationToken cancellationToken = default);
}

public class CurriculumRepository : ICurriculumRepository
{
    private readonly ProfeAsistenteDbContext _db;

    public CurriculumRepository(ProfeAsistenteDbContext db) => _db = db;

    public async Task<IReadOnlyList<NivelDto>> GetNivelesAsync(CancellationToken cancellationToken = default)
    {
        var ids = await _db.NivelesAsignaturas.AsNoTracking()
            .Where(n => n.Activa && n.Vigente &&
                        (n.EstadoRevision == EstadoRevision.Aprobado || n.EstadoRevision == EstadoRevision.AprobadoParaPruebas))
            .Select(n => n.NivelId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await _db.Niveles.AsNoTracking()
            .Where(n => ids.Contains(n.Id))
            .OrderBy(n => n.Orden)
            .Select(n => new NivelDto
            {
                Id = n.Id,
                Codigo = n.Codigo,
                Nombre = n.Nombre,
                Ciclo = n.Ciclo,
                Orden = n.Orden
            }).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AsignaturaDto>> GetAsignaturasAsync(Guid nivelId, CancellationToken cancellationToken = default)
    {
        return await _db.NivelesAsignaturas.AsNoTracking()
            .Where(n => n.NivelId == nivelId && n.Activa && n.Vigente &&
                        (n.EstadoRevision == EstadoRevision.Aprobado || n.EstadoRevision == EstadoRevision.AprobadoParaPruebas))
            .Include(n => n.Asignatura)
            .OrderBy(n => n.NombreEnNivel)
            .Select(n => new AsignaturaDto
            {
                Id = n.Id,
                NivelId = n.NivelId,
                AsignaturaCatalogoId = n.AsignaturaId,
                Nombre = n.NombreEnNivel,
                Codigo = n.Asignatura!.Codigo
            }).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnidadDto>> GetUnidadesAsync(Guid nivelAsignaturaId, CancellationToken cancellationToken = default)
    {
        return await _db.Unidades.AsNoTracking()
            .Where(u => u.NivelAsignaturaId == nivelAsignaturaId && u.Vigente &&
                        (u.PublicationStatus == CurriculumPublicationStatus.Published
                         || u.EstadoRevision == EstadoRevision.AprobadoParaPruebas))
            .OrderBy(u => u.Orden).ThenBy(u => u.Numero)
            .Select(u => new UnidadDto
            {
                Id = u.Id,
                AsignaturaId = u.NivelAsignaturaId,
                Numero = u.Numero,
                Nombre = u.Nombre,
                Descripcion = u.Descripcion
            }).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ObjetivoAprendizajeDto>> GetObjetivosPorUnidadAsync(Guid unidadId, CancellationToken cancellationToken = default)
    {
        var objectives = await _db.UnidadObjetivos.AsNoTracking()
            .Where(uo => uo.UnidadId == unidadId)
            .Include(uo => uo.ObjetivoAprendizaje)
            .Where(uo => uo.ObjetivoAprendizaje!.Vigente &&
                         (uo.ObjetivoAprendizaje.PublicationStatus == CurriculumPublicationStatus.Published
                          || uo.ObjetivoAprendizaje.EstadoRevision == EstadoRevision.AprobadoParaPruebas))
            .OrderBy(uo => uo.Orden)
            .Select(uo => new ObjetivoAprendizajeDto
            {
                Id = uo.ObjetivoAprendizajeId,
                UnidadId = uo.UnidadId,
                NivelAsignaturaId = uo.ObjetivoAprendizaje!.NivelAsignaturaId,
                Codigo = uo.ObjetivoAprendizaje.Codigo,
                Descripcion = uo.ObjetivoAprendizaje.Descripcion,
                EsContenidoOficial = uo.ObjetivoAprendizaje.EsContenidoOficial,
                FuenteTipo = uo.ObjetivoAprendizaje.FuenteTipo
            }).ToListAsync(cancellationToken);
        // Official records take precedence when a legacy/demo record exposes the same code.
        var published = objectives.GroupBy(x => x.Codigo, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.EsContenidoOficial).First()).ToList();
        await PopulateSourcesAsync(published, cancellationToken);
        return published;
    }

    public async Task<ObjetivoAprendizajeDetalleDto?> GetObjetivoDetalleAsync(Guid objetivoId, CancellationToken cancellationToken = default)
    {
        var oa = await _db.ObjetivosAprendizaje.AsNoTracking()
            .Include(o => o.Indicadores)
            .Include(o => o.EjeCurricular)
            .Include(o => o.NivelAsignatura)!.ThenInclude(n => n!.Nivel)
            .Include(o => o.NivelAsignatura)!.ThenInclude(n => n!.Asignatura)
            .Include(o => o.NivelAsignatura)!.ThenInclude(n => n!.Habilidades)
            .FirstOrDefaultAsync(o => o.Id == objetivoId && o.Vigente &&
                (o.PublicationStatus == CurriculumPublicationStatus.Published
                 || o.EstadoRevision == EstadoRevision.AprobadoParaPruebas),
                cancellationToken);
        if (oa is null) return null;

        var unidad = await _db.UnidadObjetivos.AsNoTracking()
            .Include(u => u.Unidad)
            .Where(u => u.ObjetivoAprendizajeId == objetivoId)
            .OrderBy(u => u.Orden)
            .Select(u => u.Unidad!.Nombre)
            .FirstOrDefaultAsync(cancellationToken);

        var actitudes = await _db.Actitudes.AsNoTracking()
            .Where(a => a.NivelId == oa.NivelAsignatura!.NivelId && a.Vigente)
            .Select(a => a.Descripcion)
            .ToListAsync(cancellationToken);

        var dto = new ObjetivoAprendizajeDetalleDto
        {
            Id = oa.Id,
            NivelAsignaturaId = oa.NivelAsignaturaId,
            Codigo = oa.Codigo,
            Descripcion = oa.Descripcion,
            EsContenidoOficial = oa.EsContenidoOficial,
            FuenteTipo = oa.FuenteTipo,
            UnidadNombre = unidad ?? "",
            AsignaturaNombre = oa.NivelAsignatura!.Asignatura!.Nombre,
            NivelNombre = oa.NivelAsignatura.Nivel!.Nombre,
            EjeNombre = oa.EjeCurricular?.Nombre,
            VersionCurricular = oa.Version,
            Indicadores = oa.Indicadores.Where(i => i.Vigente).OrderBy(i => i.Orden).Select(i => i.Descripcion).ToList(),
            Habilidades = oa.NivelAsignatura.Habilidades.Where(h => h.Vigente).Select(h => h.Descripcion).ToList(),
            Actitudes = actitudes
        };
        await PopulateSourcesAsync([dto], cancellationToken);
        return dto;
    }

    private async Task PopulateSourcesAsync(IEnumerable<ObjetivoAprendizajeDto> objectives, CancellationToken ct)
    {
        var list = objectives.ToList();
        var ids = list.Select(x => x.Id).ToList();
        var records = await _db.CurriculumRecordSources.AsNoTracking()
            .Where(x => x.TipoEntidad == nameof(Models.Curriculum.ObjetivoAprendizaje) && ids.Contains(x.EntidadId))
            .Include(x => x.Document).ThenInclude(d => d!.Source)
            .ToListAsync(ct);
        foreach (var dto in list)
        {
            var record = records.FirstOrDefault(x => x.EntidadId == dto.Id);
            if (record?.Document is null) continue;
            dto.Fuente = new CurriculumFuenteDto
            {
                Titulo = record.Document.Titulo,
                Url = record.Document.UrlOriginal,
                FechaDescarga = record.Document.FechaDescarga,
                PaginaInicio = record.PaginaInicio,
                PaginaFin = record.PaginaFin,
                HashDocumento = record.Document.HashSha256
            };
        }
    }
}
