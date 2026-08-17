using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Repositories;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/curriculum")]
public class CurriculumController : ControllerBase
{
    private readonly ICurriculumRepository _repo;
    private readonly ProfeAsistenteDbContext _db;

    public CurriculumController(ICurriculumRepository repo, ProfeAsistenteDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    [HttpGet("niveles")]
    [ProducesResponseType(typeof(IEnumerable<NivelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NivelDto>>> GetNiveles(CancellationToken ct) =>
        Ok(await _repo.GetNivelesAsync(ct));

    [HttpGet("asignaturas")]
    [ProducesResponseType(typeof(IEnumerable<AsignaturaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<AsignaturaDto>>> GetAsignaturas([FromQuery] Guid nivelId, CancellationToken ct)
    {
        if (nivelId == Guid.Empty)
            return BadRequest(new { error = "nivelId es obligatorio." });
        return Ok(await _repo.GetAsignaturasAsync(nivelId, ct));
    }

    [HttpGet("unidades")]
    [ProducesResponseType(typeof(IEnumerable<UnidadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<UnidadDto>>> GetUnidades(
        [FromQuery] Guid? asignaturaId,
        [FromQuery] Guid? nivelAsignaturaId,
        CancellationToken ct)
    {
        var naId = nivelAsignaturaId ?? asignaturaId ?? Guid.Empty;
        if (naId == Guid.Empty)
            return BadRequest(new { error = "nivelAsignaturaId (o asignaturaId) es obligatorio." });
        return Ok(await _repo.GetUnidadesAsync(naId, ct));
    }

    [HttpGet("objetivos")]
    [ProducesResponseType(typeof(IEnumerable<ObjetivoAprendizajeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ObjetivoAprendizajeDto>>> GetObjetivos([FromQuery] Guid unidadId, CancellationToken ct)
    {
        if (unidadId == Guid.Empty)
            return BadRequest(new { error = "unidadId es obligatorio." });
        return Ok(await _repo.GetObjetivosPorUnidadAsync(unidadId, ct));
    }

    [HttpGet("objetivos/{objetivoId:guid}/detalle")]
    [ProducesResponseType(typeof(ObjetivoAprendizajeDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObjetivoAprendizajeDetalleDto>> GetDetalle(Guid objetivoId, CancellationToken ct)
    {
        var dto = await _repo.GetObjetivoDetalleAsync(objetivoId, ct);
        return dto is null ? NotFound(new { error = "OA no encontrado o no publicado." }) : Ok(dto);
    }

    [HttpGet("version")]
    [ProducesResponseType(typeof(CurriculumVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurriculumVersionDto>> GetVersion(CancellationToken ct)
    {
        var release = await _db.CurriculumReleases.AsNoTracking()
            .Where(r => r.Status == CurriculumPublicationStatus.Published)
            .OrderByDescending(r => r.PublishedAt)
            .FirstOrDefaultAsync(ct);

        if (release is not null)
        {
            var objetivos = await _db.ObjetivosAprendizaje.AsNoTracking()
                .CountAsync(o => o.Vigente && o.PublicationStatus == CurriculumPublicationStatus.Published, ct);
            return Ok(new CurriculumVersionDto
            {
                ReleaseId = release.Id,
                Version = release.Version,
                Name = release.Name,
                PublishedAt = release.PublishedAt,
                UltimaAprobacionUtc = release.PublishedAt,
                ContentHash = release.ContentHash,
                Sources = release.SourceDocumentCount,
                ObjetivosVigentes = objetivos
            });
        }

        var ultima = await _db.ObjetivosAprendizaje.AsNoTracking()
            .Where(o => o.Vigente && (o.PublicationStatus == CurriculumPublicationStatus.Published
                                      || o.EstadoRevision == EstadoRevision.AprobadoParaPruebas))
            .OrderByDescending(o => o.Version)
            .Select(o => new { o.Version })
            .FirstOrDefaultAsync(ct);
        var count = await _db.ObjetivosAprendizaje.AsNoTracking()
            .CountAsync(o => o.Vigente && (o.PublicationStatus == CurriculumPublicationStatus.Published
                                           || o.EstadoRevision == EstadoRevision.AprobadoParaPruebas), ct);
        if (count == 0)
            return NotFound(new { error = "No hay versión curricular publicada." });

        return Ok(new CurriculumVersionDto
        {
            Version = ultima?.Version ?? "demo",
            Name = "Currículum disponible",
            ObjetivosVigentes = count,
            UltimaAprobacionUtc = DateTime.UtcNow
        });
    }
}
