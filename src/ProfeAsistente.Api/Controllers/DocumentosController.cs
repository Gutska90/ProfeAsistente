using ProfeAsistente.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProfeAsistente.Api.Controllers;

/// <summary>
/// API legada retirada (P5). Use materiales educativos y biblioteca.
/// </summary>
[ApiController]
[Authorize]
[Route("api/documentos")]
[Obsolete("Retirado. Use /api/clases/{id}/educational-documents y /api/biblioteca/materiales.")]
public class DocumentosController : ControllerBase
{
    private static IActionResult Gone() => new ObjectResult(new
    {
        error = "DocumentoLegacyRetired",
        message = "El flujo Documento legado fue retirado. Use materiales educativos (Guía/Actividad/Prueba) y la Biblioteca."
    })
    { StatusCode = StatusCodes.Status410Gone };

    [HttpPost("generar")]
    public IActionResult Generar([FromBody] GenerarDocumentoRequest? _) => Gone();

    [HttpGet]
    public IActionResult List() => Gone();

    [HttpGet("{id:guid}")]
    public IActionResult Get(Guid id) => Gone();

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] ActualizarDocumentoRequest? _) => Gone();

    [HttpPost("{id:guid}/duplicar")]
    public IActionResult Duplicate(Guid id) => Gone();

    [HttpGet("{id:guid}/exportar")]
    public IActionResult Export(Guid id) => Gone();

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id) => Gone();
}
