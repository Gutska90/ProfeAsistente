using AppEducativa.Api.Services;
using AppEducativa.Api.Services.AI.ClassGeneration;
using AppEducativa.Api.Services.AI.Gemini;
using AppEducativa.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppEducativa.Api.Controllers;

[ApiController]
[Authorize]
public class ClassStructureGenerationController : ControllerBase
{
    private readonly IClassStructureGenerationService _service;
    private readonly IClaseService _clases;

    public ClassStructureGenerationController(
        IClassStructureGenerationService service,
        IClaseService clases)
    {
        _service = service;
        _clases = clases;
    }

    [HttpPost("api/clases/{classId:guid}/generate-structure")]
    [ProducesResponseType(typeof(ClassStructureGenerationResultDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Generate(Guid classId, [FromBody] GenerateClassStructureRequest? request, CancellationToken ct)
    {
        try
        {
            request ??= new GenerateClassStructureRequest();
            if (request.EvaluationIndicatorIds.Count == 0)
            {
                var clase = await _clases.ObtenerAsync(classId, ct);
                if (clase is not null)
                    request.EvaluationIndicatorIds = clase.IndicadorEvaluacionIds;
            }

            var result = await _service.GenerateAsync(classId, request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.GenerationId }, result);
        }
        catch (Exception ex) when (ex is ClassGenerationException or GeminiApiException)
        {
            return ToProblem(ex);
        }
    }

    /// <summary>Compatibilidad MAUI: genera estructura y devuelve la clase actualizada.</summary>
    [HttpPost("api/clases/{id:guid}/generar-estructura")]
    [ProducesResponseType(typeof(ClaseDetalleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerarEstructuraLegacy(Guid id, CancellationToken ct)
    {
        try
        {
            var clase = await _clases.ObtenerAsync(id, ct);
            if (clase is null)
                return NotFound(ProblemDetailsFor(404, "ClassNotFound", "Clase no encontrada."));

            var request = new GenerateClassStructureRequest
            {
                DurationMinutes = 90,
                EvaluationIndicatorIds = clase.IndicadorEvaluacionIds
            };
            await _service.GenerateAsync(id, request, ct);
            var updated = await _clases.ObtenerAsync(id, ct);
            return Ok(updated);
        }
        catch (Exception ex) when (ex is ClassGenerationException or GeminiApiException)
        {
            return ToProblem(ex);
        }
    }

    [HttpGet("api/clases/{classId:guid}/structure-generations")]
    [ProducesResponseType(typeof(IReadOnlyList<ClassStructureGenerationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid classId, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetGenerationsAsync(classId, ct));
        }
        catch (Exception ex) when (ex is ClassGenerationException or GeminiApiException)
        {
            return ToProblem(ex);
        }
    }

    [HttpGet("api/clases/{classId:guid}/structure-generations/current")]
    [ProducesResponseType(typeof(ClassStructureGenerationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent(Guid classId, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetCurrentAsync(classId, ct);
            return result is null
                ? NotFound(ProblemDetailsFor(404, "GenerationNotFound", "No hay estructura vigente para la clase."))
                : Ok(result);
        }
        catch (Exception ex) when (ex is ClassGenerationException or GeminiApiException)
        {
            return ToProblem(ex);
        }
    }

    /// <summary>Preview of curriculum context sent to Gemini. Development or CurriculumAdmin only.</summary>
    [Authorize(Policy = "CurriculumAdmin")]
    [HttpGet("api/clases/{classId:guid}/generation-context")]
    [ProducesResponseType(typeof(ClassGenerationContextDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContext(Guid classId, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetGenerationContextAsync(classId, cancellationToken: ct));
        }
        catch (Exception ex) when (ex is ClassGenerationException or GeminiApiException)
        {
            return ToProblem(ex);
        }
    }

    [HttpGet("api/structure-generations/{id:guid}")]
    [ProducesResponseType(typeof(ClassStructureGenerationResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetByIdAsync(id, ct);
            return result is null
                ? NotFound(ProblemDetailsFor(404, "GenerationNotFound", "Generación no encontrada."))
                : Ok(result);
        }
        catch (Exception ex) when (ex is ClassGenerationException or GeminiApiException)
        {
            return ToProblem(ex);
        }
    }

    [HttpPost("api/structure-generations/{id:guid}/retry")]
    [ProducesResponseType(typeof(ClassStructureGenerationResultDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _service.RetryAsync(id, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.GenerationId }, result);
        }
        catch (Exception ex) when (ex is ClassGenerationException or GeminiApiException)
        {
            return ToProblem(ex);
        }
    }

    [HttpPost("api/structure-generations/{id:guid}/set-current")]
    [ProducesResponseType(typeof(ClassStructureGenerationResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetCurrent(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.SetCurrentAsync(id, ct));
        }
        catch (Exception ex) when (ex is ClassGenerationException or GeminiApiException)
        {
            return ToProblem(ex);
        }
    }

    [HttpPut("api/structure-generations/{id:guid}/content")]
    [ProducesResponseType(typeof(ClassStructureGenerationResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateContent(
        Guid id, [FromBody] UpdateClassStructureContentRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.UpdateContentAsync(id, request, ct));
        }
        catch (Exception ex) when (ex is ClassGenerationException or GeminiApiException)
        {
            return ToProblem(ex);
        }
    }

    [HttpDelete("api/structure-generations/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.SoftDeleteAsync(id, ct);
            return NoContent();
        }
        catch (Exception ex) when (ex is ClassGenerationException or GeminiApiException)
        {
            return ToProblem(ex);
        }
    }

    private ObjectResult ToProblem(Exception ex)
    {
        var (status, code, message) = ex switch
        {
            ClassGenerationException cg => (cg.StatusCode, cg.ErrorCode, cg.Message),
            GeminiConfigurationException gc => (gc.StatusCode ?? 503, gc.ErrorCode, gc.Message),
            GeminiRateLimitException gr => (gr.StatusCode ?? 429, gr.ErrorCode, gr.Message),
            GeminiApiException ga => (ga.StatusCode ?? 502, ga.ErrorCode, ga.Message),
            _ => (500, "InternalServerError", "Ocurrió un error interno.")
        };

        return StatusCode(status, ProblemDetailsFor(status, code, message));
    }

    private ProblemDetails ProblemDetailsFor(int status, string code, string message) =>
        new()
        {
            Status = status,
            Title = code,
            Detail = message,
            Type = $"https://appeducativa.local/errors/{code}",
            Extensions = { ["error"] = code, ["traceId"] = HttpContext.TraceIdentifier }
        };
}
