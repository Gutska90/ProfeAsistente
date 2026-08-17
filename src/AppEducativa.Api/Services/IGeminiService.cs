using AppEducativa.Api.Models;
using AppEducativa.Api.Models.Curriculum;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Services;

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
