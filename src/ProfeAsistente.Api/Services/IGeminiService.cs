using ProfeAsistente.Api.Models;
using ProfeAsistente.Api.Models.Curriculum;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.Api.Services;

public class CurriculumGeneracionContext
{
    public required Nivel Nivel { get; init; }
    public required Asignatura Asignatura { get; init; }
    public required Unidad Unidad { get; init; }
    public required IReadOnlyList<ObjetivoAprendizaje> Objetivos { get; init; }
    public IReadOnlyList<string> Contenidos { get; init; } = [];
    public IReadOnlyList<string> Habilidades { get; init; } = [];
    public IReadOnlyList<string> Actitudes { get; init; } = [];
}

[Obsolete("Legacy Gemini text path. Use IAiProvider / EducationalDocument. Retirado del DI en P5.")]
public interface IGeminiService
{
    Task<GeminiContentDto> GenerarContenidoAsync(
        GenerarDocumentoRequest request,
        CurriculumGeneracionContext contexto,
        CancellationToken ct = default);

    Task<EstructuraClaseDto> GenerarEstructuraClaseAsync(Clase clase, CancellationToken ct = default);

    Task<GeminiContentDto> GenerarMaterialClaseAsync(
        Clase clase,
        GenerarMaterialClaseRequest request,
        IReadOnlyList<IndicadorEvaluacion> indicadores,
        CurriculumGeneracionContext contexto,
        CancellationToken ct = default);
}
