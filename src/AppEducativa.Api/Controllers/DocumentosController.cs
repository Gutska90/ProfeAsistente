using System.Text.Json;
using AppEducativa.Api.Data;
using AppEducativa.Api.Models;
using AppEducativa.Api.Models.Curriculum;
using AppEducativa.Api.Repositories;
using AppEducativa.Api.Services;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api.Controllers;

/// <summary>
/// API legada de <c>Documento</c>. El flujo canónico es
/// <see cref="EducationalDocumentController"/> (<c>/api/.../educational-documents</c> y biblioteca).
/// </summary>
[ApiController]
[Authorize]
[Route("api/documentos")]
[Obsolete("Use EducationalDocument /api/clases/{id}/educational-documents y /api/biblioteca/materiales.")]
public class DocumentosController : ControllerBase
{
    private readonly IDocumentoRepository _repo;
    private readonly IGeminiService _gemini;
    private readonly IExportService _export;
    private readonly AppEducativaDbContext _db;
    private readonly ILogger<DocumentosController> _logger;

    public DocumentosController(
        IDocumentoRepository repo,
        IGeminiService gemini,
        IExportService export,
        AppEducativaDbContext db,
        ILogger<DocumentosController> logger)
    {
        _repo = repo;
        _gemini = gemini;
        _export = export;
        _db = db;
        _logger = logger;
    }

    [HttpPost("generar")]
    [ProducesResponseType(typeof(DocumentoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<DocumentoDto>> Generar([FromBody] GenerarDocumentoRequest request, CancellationToken ct)
    {
        Response.Headers.Append("Deprecation", "true");
        Response.Headers.Append("Link", "</api/biblioteca/materiales>; rel=\"successor-version\"");
        if (request.NivelId == Guid.Empty || request.AsignaturaId == Guid.Empty || request.UnidadId == Guid.Empty)
            return BadRequest(new { error = "NivelId, AsignaturaId y UnidadId son obligatorios." });

        if (request.ObjetivoAprendizajeIds is null || request.ObjetivoAprendizajeIds.Count == 0)
            return BadRequest(new { error = "Debes seleccionar al menos un Objetivo de Aprendizaje (OA)." });

        if (request.Tipo == TipoDocumento.PlanificacionUnidad)
        {
            var n = request.CantidadSesiones ?? request.CantidadItems;
            n = Math.Clamp(n <= 0 ? 6 : n, 2, 12);
            request.CantidadSesiones = n;
            request.CantidadItems = n;
        }
        else
        {
            request.CantidadItems = Math.Clamp(request.CantidadItems <= 0 ? 5 : request.CantidadItems, 1, 30);
        }

        // AsignaturaId puede ser NivelAsignaturaId (selectores) o Asignatura global (legacy docs)
        var nivel = await _db.Niveles.AsNoTracking().FirstOrDefaultAsync(n => n.Id == request.NivelId, ct);
        var nivelAsignatura = await _db.NivelesAsignaturas.AsNoTracking()
            .Include(n => n.Asignatura)
            .FirstOrDefaultAsync(n => n.Id == request.AsignaturaId
                && n.Vigente && n.EstadoRevision == EstadoRevision.Aprobado, ct);
        Asignatura? asignatura = nivelAsignatura?.Asignatura;
        if (asignatura is null)
            asignatura = await _db.Asignaturas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.AsignaturaId, ct);

        var unidad = await _db.Unidades.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UnidadId
                && u.Vigente && u.EstadoRevision == EstadoRevision.Aprobado, ct);

        if (nivel is null || asignatura is null || unidad is null)
            return BadRequest(new { error = "Nivel, asignatura o unidad no encontrados en el currículum." });

        if (nivelAsignatura is not null)
        {
            if (nivelAsignatura.NivelId != nivel.Id || unidad.NivelAsignaturaId != nivelAsignatura.Id)
                return BadRequest(new { error = "La selección Nivel → Asignatura → Unidad no es coherente." });
        }
        else if (unidad.NivelAsignaturaId == Guid.Empty)
        {
            return BadRequest(new { error = "La selección Nivel → Asignatura → Unidad no es coherente." });
        }

        var naId = nivelAsignatura?.Id ?? unidad.NivelAsignaturaId;
        var oas = await _db.UnidadObjetivos.AsNoTracking()
            .Where(uo => uo.UnidadId == unidad.Id && request.ObjetivoAprendizajeIds.Contains(uo.ObjetivoAprendizajeId))
            .Include(uo => uo.ObjetivoAprendizaje)!.ThenInclude(o => o!.Indicadores)
            .Where(uo => uo.ObjetivoAprendizaje!.Vigente
                         && uo.ObjetivoAprendizaje.EstadoRevision == EstadoRevision.Aprobado)
            .Select(uo => uo.ObjetivoAprendizaje!)
            .ToListAsync(ct);

        if (oas.Count == 0)
            return BadRequest(new { error = "Los OA seleccionados no pertenecen a la unidad indicada." });

        var habilidades = await _db.Habilidades.AsNoTracking()
            .Where(h => h.NivelAsignaturaId == naId && h.Vigente)
            .Select(h => h.Descripcion)
            .ToListAsync(ct);
        var actitudes = await _db.Actitudes.AsNoTracking()
            .Where(a => a.NivelId == nivel.Id && a.Vigente)
            .Select(a => a.Descripcion)
            .ToListAsync(ct);

        var contexto = new CurriculumGeneracionContext
        {
            Nivel = nivel,
            Asignatura = asignatura,
            Unidad = unidad,
            Objetivos = oas,
            Contenidos = string.IsNullOrWhiteSpace(unidad.Descripcion) ? [] : [unidad.Descripcion],
            Habilidades = habilidades,
            Actitudes = actitudes
        };

        try
        {
            var content = await _gemini.GenerarContenidoAsync(request, contexto, ct);

            var indicadorLookup = oas
                .SelectMany(o => o.Indicadores)
                .ToDictionary(i => i.Descripcion, i => i.Id, StringComparer.OrdinalIgnoreCase);

            var documento = DocumentoMapper.FromGemini(request, content, nivel, asignatura, unidad, oas, indicadorLookup);
            documento.ContenidoGenerado = DocumentoMapper.FormatearContenidoEditable(
                    content, documento.Unidad, documento.ObjetivoAprendizaje)
                + "\n\n---\n" + documento.ContenidoGenerado;

            await _repo.AddAsync(documento, ct);
            var saved = await _repo.GetByIdAsync(documento.Id, ct);
            return CreatedAtAction(nameof(GetById), new { id = documento.Id }, DocumentoMapper.ToDto(saved!));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error generando documento con Gemini");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentoDto>>> GetAll(CancellationToken ct)
    {
        var docs = await _repo.GetAllAsync(ct);
        return Ok(docs.Select(DocumentoMapper.ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentoDto>> GetById(Guid id, CancellationToken ct)
    {
        var doc = await _repo.GetByIdAsync(id, ct);
        if (doc is null)
            return NotFound(new { error = "Documento no encontrado." });
        return Ok(DocumentoMapper.ToDto(doc));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DocumentoDto>> Update(Guid id, [FromBody] ActualizarDocumentoRequest request, CancellationToken ct)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null)
            return NotFound(new { error = "Documento no encontrado." });

        if (request.ContenidoGenerado is not null)
            existing.ContenidoGenerado = request.ContenidoGenerado;
        if (request.Instrucciones is not null)
            existing.Instrucciones = request.Instrucciones;
        if (request.Estado.HasValue)
            existing.Estado = request.Estado.Value;
        if (request.Tema is not null)
            existing.Tema = request.Tema;

        if (request.Items is not null)
        {
            existing.Items = request.Items.Select((item, index) => new Item
            {
                Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id,
                DocumentoId = id,
                Tipo = item.Tipo,
                Enunciado = item.Enunciado,
                AlternativasJson = JsonSerializer.Serialize(item.Alternativas),
                RespuestaCorrecta = item.RespuestaCorrecta,
                Puntaje = item.Puntaje,
                Orden = item.Orden > 0 ? item.Orden : index + 1,
                IndicadorEvaluacionId = item.IndicadorEvaluacionId,
                NivelBloom = NivelBloomHelper.Normalizar(item.NivelBloom),
                VerboBloom = item.VerboBloom
            }).ToList();
        }

        if (request.Sesiones is not null)
        {
            existing.Sesiones = request.Sesiones.Select((s, index) => new SesionPlanificada
            {
                Id = s.Id == Guid.Empty ? Guid.NewGuid() : s.Id,
                DocumentoId = id,
                Numero = s.Numero > 0 ? s.Numero : index + 1,
                Descripcion = s.Descripcion,
                Actividades = s.Actividades,
                NivelBloom = NivelBloomHelper.Normalizar(s.NivelBloom),
                VerboBloom = s.VerboBloom,
                ObjetivoAprendizajeId = s.ObjetivoAprendizajeId,
                IndicadorEvaluacion = s.IndicadorEvaluacion,
                CriterioLogro = s.CriterioLogro,
                MinutosEstimados = s.MinutosEstimados
            }).ToList();
        }

        var updated = await _repo.UpdateAsync(existing, ct);
        return Ok(DocumentoMapper.ToDto(updated!));
    }

    [HttpPost("{id:guid}/exportar")]
    public async Task<IActionResult> Exportar(Guid id, [FromBody] ExportarDocumentoRequest? request, CancellationToken ct)
    {
        var formato = (request?.Formato ?? "docx").Trim().ToLowerInvariant();
        if (formato is not ("docx" or "pdf"))
            return BadRequest(new { error = "Formatos soportados: 'docx' y 'pdf'." });

        var doc = await _repo.GetByIdAsync(id, ct);
        if (doc is null)
            return NotFound(new { error = "Documento no encontrado." });

        var incluirClave = request?.IncluirClave ?? (doc.Tipo == TipoDocumento.Prueba);

        if (formato == "pdf")
        {
            var pdfBytes = _export.ExportarPdf(doc, incluirClave);
            var pdfName = Sanitizar($"{doc.Asignatura}_{doc.Tema}_{doc.Id:N}.pdf");
            return File(pdfBytes, "application/pdf", pdfName);
        }

        var bytes = _export.ExportarDocx(doc, incluirClave);
        var fileName = Sanitizar($"{doc.Asignatura}_{doc.Tema}_{doc.Id:N}.docx");
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
    }

    [HttpPost("{id:guid}/duplicar")]
    public async Task<ActionResult<DocumentoDto>> Duplicar(Guid id, CancellationToken ct)
    {
        var original = await _repo.GetByIdAsync(id, ct);
        if (original is null)
            return NotFound(new { error = "Documento no encontrado." });

        var nuevoId = Guid.NewGuid();
        var copia = new Documento
        {
            Id = nuevoId,
            Tipo = original.Tipo,
            NivelId = original.NivelId,
            AsignaturaId = original.AsignaturaId,
            UnidadId = original.UnidadId,
            Nivel = original.Nivel,
            Asignatura = original.Asignatura,
            Unidad = original.Unidad,
            Tema = $"{original.Tema} (copia)",
            ObjetivoAprendizaje = original.ObjetivoAprendizaje,
            Instrucciones = original.Instrucciones,
            ContenidoGenerado = original.ContenidoGenerado,
            FechaCreacion = DateTime.UtcNow,
            Estado = EstadoDocumento.Borrador,
            ObjetivosSeleccionados = original.ObjetivosSeleccionados.Select(o => new DocumentoObjetivoAprendizaje
            {
                DocumentoId = nuevoId,
                ObjetivoAprendizajeId = o.ObjetivoAprendizajeId
            }).ToList(),
            Items = original.Items.Select(i => new Item
            {
                Id = Guid.NewGuid(),
                DocumentoId = nuevoId,
                Tipo = i.Tipo,
                Enunciado = i.Enunciado,
                AlternativasJson = i.AlternativasJson,
                RespuestaCorrecta = i.RespuestaCorrecta,
                Puntaje = i.Puntaje,
                Orden = i.Orden,
                IndicadorEvaluacionId = i.IndicadorEvaluacionId,
                NivelBloom = i.NivelBloom,
                VerboBloom = i.VerboBloom
            }).ToList(),
            Sesiones = original.Sesiones.Select(s => new SesionPlanificada
            {
                Id = Guid.NewGuid(),
                DocumentoId = nuevoId,
                Numero = s.Numero,
                Descripcion = s.Descripcion,
                Actividades = s.Actividades,
                NivelBloom = s.NivelBloom,
                VerboBloom = s.VerboBloom,
                ObjetivoAprendizajeId = s.ObjetivoAprendizajeId,
                IndicadorEvaluacion = s.IndicadorEvaluacion,
                CriterioLogro = s.CriterioLogro,
                MinutosEstimados = s.MinutosEstimados
            }).ToList()
        };

        await _repo.AddAsync(copia, ct);
        var saved = await _repo.GetByIdAsync(nuevoId, ct);
        return CreatedAtAction(nameof(GetById), new { id = nuevoId }, DocumentoMapper.ToDto(saved!));
    }

    private static string Sanitizar(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
