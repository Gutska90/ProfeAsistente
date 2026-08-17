using AppEducativa.Api.Data;
using AppEducativa.Api.Models;
using AppEducativa.Api.Models.Curriculum;
using AppEducativa.Api.Repositories;
using AppEducativa.Api.Services.AI.ClassGeneration;
using AppEducativa.Api.Services.AI.DocumentGeneration;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api.Services;

public interface IClaseService
{
    Task<ClaseDetalleDto> CrearAsync(Guid planificacionId, CrearClaseRequest request, CancellationToken cancellationToken = default);
    Task<ClaseDetalleDto?> ObtenerAsync(Guid claseId, CancellationToken cancellationToken = default);
    Task<ClaseDetalleDto?> ActualizarAsync(Guid claseId, ActualizarClaseRequest request, CancellationToken cancellationToken = default);
}

public class ClaseService : IClaseService
{
    private readonly AppEducativaDbContext _db;
    private readonly IPlanificacionRepository _planes;
    private readonly IClaseRepository _clases;
    private readonly IClassStructureGenerationService? _structureGenerations;
    private readonly IEducationalDocumentGenerationService? _educationalDocuments;

    public ClaseService(
        AppEducativaDbContext db,
        IPlanificacionRepository planes,
        IClaseRepository clases,
        IClassStructureGenerationService? structureGenerations = null,
        IEducationalDocumentGenerationService? educationalDocuments = null)
    {
        _db = db;
        _planes = planes;
        _clases = clases;
        _structureGenerations = structureGenerations;
        _educationalDocuments = educationalDocuments;
    }

    public async Task<ClaseDetalleDto> CrearAsync(Guid planificacionId, CrearClaseRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _planes.GetByIdAsync(planificacionId, cancellationToken)
            ?? throw new KeyNotFoundException("Planificación no encontrada.");

        var oas = await _db.UnidadObjetivos.AsNoTracking()
            .Where(uo => uo.UnidadId == plan.UnidadId)
            .Include(uo => uo.ObjetivoAprendizaje)!.ThenInclude(o => o!.Indicadores)
            .Where(uo => uo.ObjetivoAprendizaje!.Vigente &&
                         (uo.ObjetivoAprendizaje.EstadoRevision == EstadoRevision.Aprobado
                          || uo.ObjetivoAprendizaje.EstadoRevision == EstadoRevision.AprobadoParaPruebas))
            .OrderBy(uo => uo.Orden)
            .Select(uo => uo.ObjetivoAprendizaje!)
            .ToListAsync(cancellationToken);

        if (oas.Count == 0)
            throw new ArgumentException("La unidad no tiene OA publicados; no se puede crear la clase.");

        ObjetivoAprendizaje oa;
        if (request.ObjetivoAprendizajeId.HasValue && request.ObjetivoAprendizajeId != Guid.Empty)
        {
            oa = oas.FirstOrDefault(o => o.Id == request.ObjetivoAprendizajeId.Value)
                 ?? throw new ArgumentException("El OA no pertenece a la unidad de la planificación.");
        }
        else
        {
            oa = oas[0];
        }

        var bloom = NivelBloomHelper.Normalizar(request.NivelBloom)
                    ?? PlanificacionMapper.SugerirSiguienteBloom(plan.Clases.OrderBy(c => c.Numero).LastOrDefault()?.NivelBloom);
        if (NivelBloomHelper.Normalizar(bloom) is null)
            throw new ArgumentException("NivelBloom no válido.");

        var ultima = plan.Clases.OrderBy(c => c.Numero).LastOrDefault();
        var numero = (ultima?.Numero ?? 0) + 1;
        if (numero <= 0)
            throw new ArgumentException("El número de clase debe ser mayor que cero.");
        if (plan.Clases.Any(c => c.Numero == numero))
            throw new ArgumentException("Ya existe una clase con ese número en la planificación.");

        var fecha = request.Fecha ?? (ultima is null ? plan.FechaInicio : ultima.Fecha.AddDays(2));
        if (fecha < plan.FechaInicio || fecha > plan.FechaFin)
            throw new ArgumentException("La fecha de la clase está fuera del rango de la planificación.");

        var indicadorIds = request.IndicadorEvaluacionIds?
            .Where(x => oa.Indicadores.Any(i => i.Id == x))
            .ToList();
        if (indicadorIds is null || indicadorIds.Count == 0)
            indicadorIds = oa.Indicadores.Select(i => i.Id).Take(3).ToList();

        var clase = new Clase
        {
            Id = Guid.NewGuid(),
            PlanificacionId = plan.Id,
            Numero = numero,
            Fecha = fecha,
            ObjetivoAprendizajeId = oa.Id,
            NivelBloom = bloom,
            Estado = EstadoClase.Planificada,
            Indicadores = indicadorIds.Select(indId => new ClaseIndicadorEvaluacion
            {
                ClaseId = Guid.Empty,
                IndicadorEvaluacionId = indId
            }).ToList()
        };
        foreach (var ind in clase.Indicadores)
            ind.ClaseId = clase.Id;

        var saved = await _clases.AddAsync(clase, cancellationToken);
        return PlanificacionMapper.ToDetalle(saved);
    }

    public async Task<ClaseDetalleDto?> ObtenerAsync(Guid claseId, CancellationToken cancellationToken = default)
    {
        var clase = await _clases.GetByIdAsync(claseId, cancellationToken);
        return clase is null ? null : PlanificacionMapper.ToDetalle(clase);
    }

    public async Task<ClaseDetalleDto?> ActualizarAsync(Guid claseId, ActualizarClaseRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _clases.GetByIdAsync(claseId, cancellationToken)
            ?? throw new KeyNotFoundException("Clase no encontrada.");
        var plan = existing.Planificacion!;

        var previousOa = existing.ObjetivoAprendizajeId;
        var previousBloom = existing.NivelBloom;
        var previousIndicators = existing.Indicadores
            .Select(i => i.IndicadorEvaluacionId)
            .OrderBy(x => x)
            .ToList();

        if (request.Fecha.HasValue)
        {
            if (request.Fecha.Value < plan.FechaInicio || request.Fecha.Value > plan.FechaFin)
                throw new ArgumentException("La fecha de la clase está fuera del rango de la planificación.");
            existing.Fecha = request.Fecha.Value;
        }

        if (request.NivelBloom is not null)
        {
            var bloom = NivelBloomHelper.Normalizar(request.NivelBloom)
                        ?? throw new ArgumentException("NivelBloom no válido.");
            existing.NivelBloom = bloom;
        }

        if (request.DescripcionInicio is not null) existing.DescripcionInicio = request.DescripcionInicio;
        if (request.DescripcionDesarrollo is not null) existing.DescripcionDesarrollo = request.DescripcionDesarrollo;
        if (request.DescripcionCierre is not null) existing.DescripcionCierre = request.DescripcionCierre;
        if (request.Estado.HasValue) existing.Estado = request.Estado.Value;

        if (request.ObjetivoAprendizajeId.HasValue && request.ObjetivoAprendizajeId != Guid.Empty)
        {
            var oaOk = await _db.UnidadObjetivos.AsNoTracking()
                .AnyAsync(uo => uo.UnidadId == plan.UnidadId
                                && uo.ObjetivoAprendizajeId == request.ObjetivoAprendizajeId
                                && uo.ObjetivoAprendizaje!.Vigente
                                && (uo.ObjetivoAprendizaje.EstadoRevision == EstadoRevision.Aprobado
                                    || uo.ObjetivoAprendizaje.EstadoRevision == EstadoRevision.AprobadoParaPruebas),
                    cancellationToken);
            if (!oaOk)
                throw new ArgumentException("El OA no pertenece a la unidad de la planificación.");
            existing.ObjetivoAprendizajeId = request.ObjetivoAprendizajeId.Value;
        }

        if (request.IndicadorEvaluacionIds is not null)
        {
            existing.Indicadores = request.IndicadorEvaluacionIds.Select(indId => new ClaseIndicadorEvaluacion
            {
                ClaseId = claseId,
                IndicadorEvaluacionId = indId
            }).ToList();
        }

        var updated = await _clases.UpdateAsync(existing, cancellationToken);

        var configChanged = previousOa != updated!.ObjetivoAprendizajeId
                            || !string.Equals(previousBloom, updated.NivelBloom, StringComparison.OrdinalIgnoreCase)
                            || !previousIndicators.SequenceEqual(
                                updated.Indicadores.Select(i => i.IndicadorEvaluacionId).OrderBy(x => x));

        if (configChanged)
        {
            if (_structureGenerations is not null)
                await _structureGenerations.MarkOutdatedIfConfigurationChangedAsync(claseId, cancellationToken);
            if (_educationalDocuments is not null)
                await _educationalDocuments.MarkOutdatedIfConfigurationChangedAsync(claseId, cancellationToken);
        }

        return PlanificacionMapper.ToDetalle(updated);
    }
}
