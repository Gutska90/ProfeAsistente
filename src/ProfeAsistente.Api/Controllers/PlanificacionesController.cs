using ProfeAsistente.Api.Services;
using ProfeAsistente.Api.Services.Authorization;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProfeAsistente.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/planificaciones")]
public class PlanificacionesController : ControllerBase
{
    private readonly IPlanificacionService _planes;
    private readonly IClaseService _clases;
    private readonly IExportService _export;
    private readonly Repositories.IPlanificacionRepository _repo;
    private readonly IResourceAuthorizationService _authz;

    public PlanificacionesController(
        IPlanificacionService planes,
        IClaseService clases,
        IExportService export,
        Repositories.IPlanificacionRepository repo,
        IResourceAuthorizationService authz)
    {
        _planes = planes;
        _clases = clases;
        _export = export;
        _repo = repo;
        _authz = authz;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PlanificacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PlanificacionDto>>> GetAll(CancellationToken ct) =>
        Ok(await _planes.ListarAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlanificacionDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanificacionDetalleDto>> GetById(Guid id, CancellationToken ct)
    {
        var plan = await _planes.ObtenerAsync(id, ct);
        return plan is null ? NotFound(new { error = "Planificación no encontrada." }) : Ok(plan);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.CanCreatePlanning)]
    [ProducesResponseType(typeof(PlanificacionDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanificacionDetalleDto>> Crear(
        [FromBody] CrearPlanificacionRequest request, CancellationToken ct)
    {
        var saved = await _planes.CrearAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = saved.Id }, saved);
    }

    [HttpPost("{id:guid}/clases")]
    [ProducesResponseType(typeof(ClaseDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaseDetalleDto>> AgregarClase(
        Guid id, [FromBody] CrearClaseRequest? request, CancellationToken ct)
    {
        var saved = await _clases.CrearAsync(id, request ?? new CrearClaseRequest(), ct);
        return CreatedAtAction(nameof(ClasesController.GetById), "Clases", new { id = saved.Id }, saved);
    }

    [HttpPost("{id:guid}/exportar")]
    [Authorize(Policy = AppPolicies.CanExportMaterials)]
    public async Task<IActionResult> Exportar(Guid id, CancellationToken ct)
    {
        await _authz.EnsureCanAccessPlanningAsync(id, "export", ct);
        var plan = await _repo.GetByIdAsync(id, ct);
        if (plan is null) return NotFound(new { error = "Planificación no encontrada." });
        var bytes = _export.ExportarPlanificacionDocx(plan);
        var asig = plan.NivelAsignatura?.NombreEnNivel ?? "plan";
        var name = $"{asig}_{plan.Nombre}_{plan.Id:N}.docx";
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", name);
    }
}
