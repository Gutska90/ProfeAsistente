using AppEducativa.Shared.Dtos;

namespace AppEducativa.Api.Services.AI.DocumentGeneration;

public interface IEducationalDocumentGenerationService
{
    Task<EducationalDocumentGenerationResultDto> GenerateAsync(
        Guid classId,
        GenerateEducationalDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EducationalDocumentSummaryDto>> ListByClassAsync(
        Guid classId, CancellationToken cancellationToken = default);

    Task<EducationalDocumentDetailDto?> GetAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    Task<EducationalDocumentStudentViewDto?> GetStudentViewAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EducationalItemDto>> GetItemsAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    Task<AnswerKeyDto> GetAnswerKeyAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    Task<EducationalDocumentDetailDto> UpdateAsync(
        Guid documentId, UpdateEducationalDocumentRequest request, CancellationToken cancellationToken = default);

    Task<EducationalDocumentDetailDto> UpdateStatusAsync(
        Guid documentId, UpdateEducationalDocumentStatusRequest request, CancellationToken cancellationToken = default);

    Task<EducationalDocumentGenerationResultDto> RegenerateAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    Task<EducationalDocumentDetailDto> DuplicateAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    Task<EducationalItemDto> AddItemAsync(
        Guid documentId, CreateEducationalItemRequest request, CancellationToken cancellationToken = default);

    Task<EducationalItemDto> UpdateItemAsync(
        Guid itemId, UpdateEducationalItemRequest request, CancellationToken cancellationToken = default);

    Task DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<EducationalDocumentDetailDto> ReorderItemsAsync(
        Guid documentId, ReorderEducationalItemsRequest request, CancellationToken cancellationToken = default);

    Task<EducationalItemDto> RegenerateItemAsync(
        Guid itemId, RegenerateEducationalItemRequest request, CancellationToken cancellationToken = default);

    Task<EducationalDocumentDetailDto> SetCurrentAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EducationalDocumentRevisionSummaryDto>> GetRevisionsAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    Task<EducationalDocumentValidationResultDto> ValidateAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task MarkOutdatedIfConfigurationChangedAsync(
        Guid classId, CancellationToken cancellationToken = default);
}
