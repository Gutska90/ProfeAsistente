using AppEducativa.Api.Services;
using AppEducativa.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppEducativa.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/clases")]
public class ClasesController : ControllerBase
{
    private readonly IClaseService _clases;

    public ClasesController(IClaseService clases) => _clases = clases;

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClaseDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaseDetalleDto>> GetById(Guid id, CancellationToken ct)
    {
        var clase = await _clases.ObtenerAsync(id, ct);
        return clase is null ? NotFound(new { error = "Clase no encontrada." }) : Ok(clase);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClaseDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaseDetalleDto>> Actualizar(
        Guid id, [FromBody] ActualizarClaseRequest request, CancellationToken ct)
    {
        var updated = await _clases.ActualizarAsync(id, request, ct);
        return Ok(updated);
    }
}
