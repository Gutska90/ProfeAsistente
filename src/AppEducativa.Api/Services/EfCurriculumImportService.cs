using System.Text.Json;
using AppEducativa.Api.Data;
using AppEducativa.Api.Models.Curriculum;
using AppEducativa.CurriculumImporter.Abstractions;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api.Services;

public class EfCurriculumImportService : ICurriculumImportService
{
    private readonly AppEducativaDbContext _db;
    private readonly ICurriculumValidator _validator;
    private readonly ICurriculumDiffService _diff;
    private readonly ILogger<EfCurriculumImportService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public EfCurriculumImportService(
        AppEducativaDbContext db,
        ICurriculumValidator validator,
        ICurriculumDiffService diff,
        ILogger<EfCurriculumImportService> logger)
    {
        _db = db;
        _validator = validator;
        _diff = diff;
        _logger = logger;
    }

    public async Task<CurriculumImportResult> ImportAsync(
        CurriculumExtractionResult extraction,
        Guid? documentId = null,
        bool autoApprove = false,
        CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(extraction);
        var batch = new CurriculumImportBatch
        {
            Id = Guid.NewGuid(),
            FechaInicio = DateTime.UtcNow,
            Estado = validation.IsValid ? EstadoImportBatch.Validado : EstadoImportBatch.Error,
            CantidadFuentes = 1,
            CantidadAdvertencias = validation.Warnings.Count,
            CantidadErrores = validation.Errors.Count,
            ExtractionJson = JsonSerializer.Serialize(extraction, JsonOptions),
            CurriculumDocumentId = documentId,
            Mensaje = validation.IsValid ? "Validación OK" : string.Join("; ", validation.Errors)
        };

        if (!validation.IsValid)
        {
            batch.FechaTermino = DateTime.UtcNow;
            _db.CurriculumImportBatches.Add(batch);
            await _db.SaveChangesAsync(cancellationToken);
            return new CurriculumImportResult
            {
                BatchId = batch.Id,
                Success = false,
                Errores = validation.Errors,
                Advertencias = validation.Warnings
            };
        }

        var current = await BuildCurrentExtractionAsync(extraction.Level!.Code, extraction.Subject!.Code, cancellationToken);
        var diff = _diff.Compare(extraction, current);
        batch.DiffJson = JsonSerializer.Serialize(diff, JsonOptions);
        batch.Estado = EstadoImportBatch.DiffListo;
        batch.CantidadRegistrosNuevos = diff.Nuevos;
        batch.CantidadActualizados = diff.Modificados;
        batch.CantidadSinCambios = diff.SinCambios;
        _db.CurriculumImportBatches.Add(batch);
        await _db.SaveChangesAsync(cancellationToken);

        if (!autoApprove)
        {
            return new CurriculumImportResult
            {
                BatchId = batch.Id,
                Success = true,
                RegistrosNuevos = diff.Nuevos,
                Actualizados = diff.Modificados,
                SinCambios = diff.SinCambios,
                Advertencias = validation.Warnings
            };
        }

        return await ApproveBatchAsync(batch.Id, cancellationToken);
    }

    public async Task<CurriculumImportResult> ApproveBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var batch = await _db.CurriculumImportBatches.FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch is null)
            return new CurriculumImportResult { BatchId = batchId, Success = false, Errores = ["Lote no encontrado."] };
        if (string.IsNullOrWhiteSpace(batch.ExtractionJson))
            return new CurriculumImportResult { BatchId = batchId, Success = false, Errores = ["Lote sin extracción."] };

        var extraction = JsonSerializer.Deserialize<CurriculumExtractionResult>(batch.ExtractionJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("No se pudo leer la extracción del lote.");

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var version = extraction.Version;
            var nivel = await UpsertNivelAsync(extraction.Level!, cancellationToken);
            var asignatura = await UpsertAsignaturaAsync(extraction.Subject!, cancellationToken);
            var na = await UpsertNivelAsignaturaAsync(nivel, asignatura, cancellationToken);

            var ejes = new Dictionary<string, EjeCurricular>(StringComparer.OrdinalIgnoreCase);
            foreach (var axis in extraction.Axes)
            {
                var eje = await _db.EjesCurriculares.FirstOrDefaultAsync(
                    e => e.NivelAsignaturaId == na.Id && e.Nombre == axis.Name, cancellationToken);
                if (eje is null)
                {
                    eje = new EjeCurricular
                    {
                        Id = Guid.NewGuid(),
                        NivelAsignaturaId = na.Id,
                        Codigo = axis.Code,
                        Nombre = axis.Name,
                        Descripcion = axis.Description,
                        EstadoRevision = EstadoRevision.Aprobado,
                        Vigente = true
                    };
                    _db.EjesCurriculares.Add(eje);
                }
                else
                {
                    eje.Descripcion = axis.Description;
                    eje.Codigo = axis.Code;
                    eje.EstadoRevision = EstadoRevision.Aprobado;
                    eje.Vigente = true;
                }
                ejes[axis.Name] = eje;
            }

            var oaByCode = new Dictionary<string, ObjetivoAprendizaje>(StringComparer.OrdinalIgnoreCase);
            var nuevos = 0;
            var actualizados = 0;

            foreach (var oaDto in extraction.LearningObjectives)
            {
                var existing = await _db.ObjetivosAprendizaje.FirstOrDefaultAsync(
                    o => o.NivelAsignaturaId == na.Id && o.Codigo == oaDto.Code && o.Version == version, cancellationToken);

                Guid? ejeId = null;
                if (!string.IsNullOrWhiteSpace(oaDto.AxisName) && ejes.TryGetValue(oaDto.AxisName, out var eje))
                    ejeId = eje.Id;

                if (existing is null)
                {
                    existing = new ObjetivoAprendizaje
                    {
                        Id = Guid.NewGuid(),
                        NivelAsignaturaId = na.Id,
                        EjeCurricularId = ejeId,
                        Codigo = oaDto.Code.Trim(),
                        Numero = oaDto.Number,
                        Descripcion = oaDto.Description.Trim(),
                        Tipo = ParseTipo(oaDto.Tipo),
                        EsObligatorio = oaDto.EsObligatorio,
                        Vigente = true,
                        Version = version,
                        EstadoRevision = EstadoRevision.Aprobado,
                        ConfianzaExtraccion = extraction.ConfianzaExtraccion,
                        EsContenidoOficial = true,
                        FuenteTipo = "ProgramaEstudioOficial",
                        PublicationStatus = CurriculumPublicationStatus.Draft
                    };
                    _db.ObjetivosAprendizaje.Add(existing);
                    nuevos++;
                }
                else
                {
                    if (!string.Equals(existing.Descripcion, oaDto.Description.Trim(), StringComparison.Ordinal))
                        actualizados++;
                    existing.Descripcion = oaDto.Description.Trim();
                    existing.EjeCurricularId = ejeId;
                    existing.Numero = oaDto.Number;
                    existing.EsObligatorio = oaDto.EsObligatorio;
                    existing.Vigente = true;
                    existing.EstadoRevision = EstadoRevision.Aprobado;
                    existing.EsContenidoOficial = true;
                    existing.FuenteTipo = "ProgramaEstudioOficial";
                    if (existing.PublicationStatus != CurriculumPublicationStatus.Published)
                        existing.PublicationStatus = CurriculumPublicationStatus.Draft;
                }

                oaByCode[existing.Codigo] = existing;
            }

            // Marcar OA vigentes de otras versiones/códigos ausentes como no vigentes (misma NA)
            var codes = oaByCode.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var previos = await _db.ObjetivosAprendizaje
                .Where(o => o.NivelAsignaturaId == na.Id && o.Vigente)
                .ToListAsync(cancellationToken);
            foreach (var prev in previos)
            {
                if (!codes.Contains(prev.Codigo))
                {
                    prev.Vigente = false;
                    _logger.LogInformation("OA marcado no vigente: {Codigo}", prev.Codigo);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            foreach (var uDto in extraction.Units.OrderBy(u => u.Number))
            {
                var unidad = await _db.Unidades.FirstOrDefaultAsync(
                    u => u.NivelAsignaturaId == na.Id && u.Numero == uDto.Number, cancellationToken);
                if (unidad is null)
                {
                    unidad = new Unidad
                    {
                        Id = Guid.NewGuid(),
                        NivelAsignaturaId = na.Id,
                        Numero = uDto.Number,
                        Nombre = uDto.Name,
                        Descripcion = uDto.Description,
                        HorasPedagogicasSugeridas = uDto.SuggestedHours,
                        Orden = uDto.Number,
                        EstadoRevision = EstadoRevision.Aprobado,
                        Vigente = true,
                        EsContenidoOficial = true,
                        FuenteTipo = "ProgramaEstudioOficial",
                        PublicationStatus = CurriculumPublicationStatus.Draft
                    };
                    _db.Unidades.Add(unidad);
                    await _db.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    unidad.Nombre = uDto.Name;
                    unidad.Descripcion = uDto.Description;
                    unidad.HorasPedagogicasSugeridas = uDto.SuggestedHours;
                    unidad.EstadoRevision = EstadoRevision.Aprobado;
                    unidad.Vigente = true;
                    unidad.EsContenidoOficial = true;
                    unidad.FuenteTipo = "ProgramaEstudioOficial";
                    if (unidad.PublicationStatus != CurriculumPublicationStatus.Published)
                        unidad.PublicationStatus = CurriculumPublicationStatus.Draft;
                }

                var existingLinks = await _db.UnidadObjetivos.Where(x => x.UnidadId == unidad.Id).ToListAsync(cancellationToken);
                _db.UnidadObjetivos.RemoveRange(existingLinks);
                var orden = 1;
                foreach (var code in uDto.LearningObjectiveCodes)
                {
                    if (!oaByCode.TryGetValue(code, out var oa)) continue;
                    _db.UnidadObjetivos.Add(new UnidadObjetivoAprendizaje
                    {
                        UnidadId = unidad.Id,
                        ObjetivoAprendizajeId = oa.Id,
                        Orden = orden++,
                        EsPrincipal = true
                    });
                }
            }

            // Indicadores: reemplazar por OA
            foreach (var oa in oaByCode.Values)
            {
                var oldInds = await _db.IndicadoresEvaluacion.Where(i => i.ObjetivoAprendizajeId == oa.Id).ToListAsync(cancellationToken);
                _db.IndicadoresEvaluacion.RemoveRange(oldInds);
            }

            foreach (var ind in extraction.EvaluationIndicators)
            {
                if (!oaByCode.TryGetValue(ind.LearningObjectiveCode, out var oa)) continue;
                _db.IndicadoresEvaluacion.Add(new IndicadorEvaluacion
                {
                    Id = Guid.NewGuid(),
                    ObjetivoAprendizajeId = oa.Id,
                    Codigo = ind.Code,
                    Descripcion = ind.Description.Trim(),
                    EsSugerido = ind.EsSugerido,
                    Orden = ind.Orden,
                    Vigente = true,
                    EstadoRevision = EstadoRevision.Aprobado
                });
            }

            var oldHab = await _db.Habilidades.Where(h => h.NivelAsignaturaId == na.Id).ToListAsync(cancellationToken);
            _db.Habilidades.RemoveRange(oldHab);
            foreach (var sk in extraction.Skills)
            {
                _db.Habilidades.Add(new Habilidad
                {
                    Id = Guid.NewGuid(),
                    NivelAsignaturaId = na.Id,
                    Codigo = sk.Code,
                    Descripcion = sk.Description,
                    Vigente = true,
                    EstadoRevision = EstadoRevision.Aprobado
                });
            }

            var oldAct = await _db.Actitudes.Where(a => a.NivelId == nivel.Id).ToListAsync(cancellationToken);
            _db.Actitudes.RemoveRange(oldAct);
            foreach (var at in extraction.Attitudes)
            {
                _db.Actitudes.Add(new Actitud
                {
                    Id = Guid.NewGuid(),
                    NivelId = nivel.Id,
                    NivelAsignaturaId = na.Id,
                    Codigo = at.Code,
                    Descripcion = at.Description,
                    Vigente = true,
                    EstadoRevision = EstadoRevision.Aprobado
                });
            }

            foreach (var oat in extraction.TransversalObjectives)
            {
                var existing = await _db.Oats.FirstOrDefaultAsync(
                    o => o.NivelId == nivel.Id && o.Codigo == oat.Code && o.Version == version, cancellationToken);
                if (existing is null)
                {
                    _db.Oats.Add(new ObjetivoAprendizajeTransversal
                    {
                        Id = Guid.NewGuid(),
                        NivelId = nivel.Id,
                        Codigo = oat.Code,
                        Dimension = oat.Dimension,
                        Descripcion = oat.Description,
                        Vigente = true,
                        Version = version,
                        EstadoRevision = EstadoRevision.Aprobado
                    });
                }
                else
                {
                    existing.Descripcion = oat.Description;
                    existing.Dimension = oat.Dimension;
                    existing.Vigente = true;
                    existing.EstadoRevision = EstadoRevision.Aprobado;
                }
            }

            if (batch.CurriculumDocumentId is Guid docId)
            {
                foreach (var oa in oaByCode.Values)
                {
                    _db.CurriculumRecordSources.Add(new CurriculumRecordSource
                    {
                        Id = Guid.NewGuid(),
                        CurriculumDocumentId = docId,
                        TipoEntidad = nameof(ObjetivoAprendizaje),
                        EntidadId = oa.Id,
                        FragmentoFuente = oa.Codigo,
                        FechaVigenciaDesde = DateTime.UtcNow
                    });
                }
            }

            batch.Estado = EstadoImportBatch.Aprobado;
            batch.Status = CurriculumImportStatus.Imported;
            batch.FechaTermino = DateTime.UtcNow;
            batch.CantidadRegistrosNuevos = nuevos;
            batch.CantidadActualizados = actualizados;
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new CurriculumImportResult
            {
                BatchId = batch.Id,
                Success = true,
                RegistrosNuevos = nuevos,
                Actualizados = actualizados,
                Advertencias = extraction.Advertencias
            };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error aprobando lote {BatchId}", batchId);
            batch.Estado = EstadoImportBatch.Error;
            batch.Mensaje = ex.Message;
            batch.FechaTermino = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return new CurriculumImportResult
            {
                BatchId = batchId,
                Success = false,
                Errores = [ex.Message]
            };
        }
    }

    private async Task<CurriculumExtractionResult?> BuildCurrentExtractionAsync(string levelCode, string subjectCode, CancellationToken ct)
    {
        var na = await _db.NivelesAsignaturas.AsNoTracking()
            .Include(n => n.Nivel)
            .Include(n => n.Asignatura)
            .Include(n => n.Objetivos.Where(o => o.Vigente && o.EstadoRevision == EstadoRevision.Aprobado))
            .FirstOrDefaultAsync(n => n.Nivel!.Codigo == levelCode && n.Asignatura!.Codigo == subjectCode, ct);
        if (na is null) return null;

        return new CurriculumExtractionResult
        {
            Level = new LevelExtractDto { Code = na.Nivel!.Codigo, Name = na.Nivel.Nombre },
            Subject = new SubjectExtractDto { Code = na.Asignatura!.Codigo, Name = na.Asignatura.Nombre },
            LearningObjectives = na.Objetivos.Select(o => new LearningObjectiveExtractDto
            {
                Code = o.Codigo,
                Description = o.Descripcion
            }).ToList()
        };
    }

    private async Task<Nivel> UpsertNivelAsync(LevelExtractDto dto, CancellationToken ct)
    {
        var nivel = await _db.Niveles.FirstOrDefaultAsync(n => n.Codigo == dto.Code, ct);
        if (nivel is null)
        {
            nivel = new Nivel
            {
                Id = Guid.NewGuid(),
                Codigo = dto.Code,
                Nombre = dto.Name,
                Ciclo = dto.Cycle,
                Orden = dto.Order
            };
            _db.Niveles.Add(nivel);
        }
        else
        {
            nivel.Nombre = dto.Name;
            nivel.Ciclo = dto.Cycle;
            if (dto.Order > 0) nivel.Orden = dto.Order;
        }
        await _db.SaveChangesAsync(ct);
        return nivel;
    }

    private async Task<Asignatura> UpsertAsignaturaAsync(SubjectExtractDto dto, CancellationToken ct)
    {
        var a = await _db.Asignaturas.FirstOrDefaultAsync(x => x.Codigo == dto.Code, ct);
        if (a is null)
        {
            a = new Asignatura { Id = Guid.NewGuid(), Codigo = dto.Code, Nombre = dto.Name };
            _db.Asignaturas.Add(a);
        }
        else a.Nombre = dto.Name;
        await _db.SaveChangesAsync(ct);
        return a;
    }

    private async Task<NivelAsignatura> UpsertNivelAsignaturaAsync(Nivel nivel, Asignatura asignatura, CancellationToken ct)
    {
        var na = await _db.NivelesAsignaturas.FirstOrDefaultAsync(
            x => x.NivelId == nivel.Id && x.AsignaturaId == asignatura.Id, ct);
        if (na is null)
        {
            na = new NivelAsignatura
            {
                Id = Guid.NewGuid(),
                NivelId = nivel.Id,
                AsignaturaId = asignatura.Id,
                NombreEnNivel = asignatura.Nombre,
                Activa = true,
                EstadoRevision = EstadoRevision.Aprobado,
                Vigente = true
            };
            _db.NivelesAsignaturas.Add(na);
        }
        else
        {
            na.Activa = true;
            na.EstadoRevision = EstadoRevision.Aprobado;
            na.Vigente = true;
            na.NombreEnNivel = asignatura.Nombre;
        }
        await _db.SaveChangesAsync(ct);
        return na;
    }

    private static TipoObjetivoAprendizaje ParseTipo(string? tipo) => tipo?.ToLowerInvariant() switch
    {
        "basal" => TipoObjetivoAprendizaje.Basal,
        "complementario" => TipoObjetivoAprendizaje.Complementario,
        "otro" => TipoObjetivoAprendizaje.Otro,
        _ => TipoObjetivoAprendizaje.NoClasificado
    };
}
