using ProfeAsistente.Shared.Dtos;

namespace ProfeAsistente.Api.Services.AI.ClassGeneration;

public interface IClassStructureGenerationService
{
    Task<ClassStructureGenerationResultDto> GenerateAsync(
        Guid classId,
        GenerateClassStructureRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassStructureGenerationSummaryDto>> GetGenerationsAsync(
        Guid classId,
        CancellationToken cancellationToken = default);

    Task<ClassStructureGenerationResultDto?> GetCurrentAsync(
        Guid classId,
        CancellationToken cancellationToken = default);

    Task<ClassStructureGenerationResultDto?> GetByIdAsync(
        Guid generationId,
        CancellationToken cancellationToken = default);

    Task<ClassStructureGenerationResultDto> RetryAsync(
        Guid generationId,
        CancellationToken cancellationToken = default);

    Task<ClassStructureGenerationResultDto> SetCurrentAsync(
        Guid generationId,
        CancellationToken cancellationToken = default);

    Task<ClassStructureGenerationResultDto> UpdateContentAsync(
        Guid generationId,
        UpdateClassStructureContentRequest request,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(
        Guid generationId,
        CancellationToken cancellationToken = default);

    Task<ClassGenerationContextDto> GetGenerationContextAsync(
        Guid classId,
        GenerateClassStructureRequest? request = null,
        CancellationToken cancellationToken = default);

    Task MarkOutdatedIfConfigurationChangedAsync(
        Guid classId,
        CancellationToken cancellationToken = default);
}
