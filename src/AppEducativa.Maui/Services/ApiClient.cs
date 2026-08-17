using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppEducativa.Shared.Dtos;

namespace AppEducativa.Maui.Services;

public interface IApiClient
{
    Task<IReadOnlyList<NivelDto>> GetNivelesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AsignaturaDto>> GetAsignaturasAsync(Guid nivelId, CancellationToken ct = default);
    Task<IReadOnlyList<UnidadDto>> GetUnidadesAsync(Guid asignaturaId, CancellationToken ct = default);
    Task<IReadOnlyList<ObjetivoAprendizajeDto>> GetObjetivosAsync(Guid unidadId, CancellationToken ct = default);
    Task<ObjetivoAprendizajeDetalleDto?> GetObjetivoDetalleAsync(Guid oaId, CancellationToken ct = default);

    Task<IReadOnlyList<PlanificacionResumenDto>> GetPlanificacionesAsync(CancellationToken ct = default);
    Task<PlanificacionDetalleDto?> GetPlanificacionAsync(Guid id, CancellationToken ct = default);
    Task<PlanificacionDetalleDto> CrearPlanificacionAsync(CrearPlanificacionRequest request, CancellationToken ct = default);
    Task<(byte[] Bytes, string FileName)> ExportarPlanificacionAsync(Guid id, CancellationToken ct = default);
    Task<ClaseDetalleDto> AgregarClaseAsync(Guid planificacionId, CrearClaseRequest? request = null, CancellationToken ct = default);

    Task<ClaseDetalleDto?> GetClaseAsync(Guid id, CancellationToken ct = default);
    Task<ClaseDetalleDto> ActualizarClaseAsync(Guid id, ActualizarClaseRequest request, CancellationToken ct = default);
    Task EliminarClaseAsync(Guid id, CancellationToken ct = default);
    Task<ClaseDetalleDto> GenerarEstructuraClaseAsync(Guid id, CancellationToken ct = default);
    Task<DocumentoDto> GenerarMaterialClaseAsync(Guid id, GenerarMaterialClaseRequest request, CancellationToken ct = default);

    // Class structure generation (Prompt 5)
    Task<ClassStructureGenerationResultDto> GenerateClassStructureAsync(
        Guid classId, GenerateClassStructureRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ClassStructureGenerationSummaryDto>> GetStructureGenerationsAsync(
        Guid classId, CancellationToken ct = default);
    Task<ClassStructureGenerationResultDto?> GetCurrentStructureAsync(Guid classId, CancellationToken ct = default);
    Task<ClassStructureGenerationResultDto?> GetStructureGenerationAsync(Guid generationId, CancellationToken ct = default);
    Task<ClassGenerationContextDto?> GetGenerationContextAsync(Guid classId, CancellationToken ct = default);
    Task<ClassStructureGenerationResultDto> UpdateStructureContentAsync(
        Guid generationId, UpdateClassStructureContentRequest request, CancellationToken ct = default);
    Task<ClassStructureGenerationResultDto> SetCurrentStructureAsync(Guid generationId, CancellationToken ct = default);
    Task<ClassStructureGenerationResultDto> RetryStructureGenerationAsync(Guid generationId, CancellationToken ct = default);

    // Educational documents (Prompt 6)
    Task<EducationalDocumentGenerationResultDto> GenerateEducationalDocumentAsync(
        Guid classId, GenerateEducationalDocumentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<EducationalDocumentSummaryDto>> GetEducationalDocumentsAsync(
        Guid classId, CancellationToken ct = default);
    Task<IReadOnlyList<EducationalDocumentSummaryDto>> GetMaterialLibraryAsync(
        Guid? courseId = null, string? type = null, string? search = null, CancellationToken ct = default);
    Task<EducationalDocumentDetailDto?> GetEducationalDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<EducationalDocumentStudentViewDto?> GetEducationalDocumentStudentViewAsync(
        Guid documentId, CancellationToken ct = default);
    Task<EducationalDocumentDetailDto> UpdateEducationalDocumentAsync(
        Guid documentId, UpdateEducationalDocumentRequest request, CancellationToken ct = default);
    Task<EducationalDocumentDetailDto> UpdateEducationalDocumentStatusAsync(
        Guid documentId, UpdateEducationalDocumentStatusRequest request, CancellationToken ct = default);
    Task<EducationalItemDto> UpdateEducationalItemAsync(
        Guid itemId, UpdateEducationalItemRequest request, CancellationToken ct = default);
    Task<EducationalItemDto> RegenerateEducationalItemAsync(
        Guid itemId, RegenerateEducationalItemRequest request, CancellationToken ct = default);
    Task<EducationalDocumentDetailDto> ReorderEducationalItemsAsync(
        Guid documentId, ReorderEducationalItemsRequest request, CancellationToken ct = default);
    Task<AnswerKeyDto> GetEducationalAnswerKeyAsync(Guid documentId, CancellationToken ct = default);
    Task<EducationalDocumentValidationResultDto> ValidateEducationalDocumentAsync(
        Guid documentId, CancellationToken ct = default);

    Task<DocumentoDto?> GetDocumentoAsync(Guid id, CancellationToken ct = default);
    Task<DocumentoDto> ActualizarDocumentoAsync(Guid id, ActualizarDocumentoRequest request, CancellationToken ct = default);
    Task<(byte[] Bytes, string FileName)> ExportarDocumentoAsync(Guid id, string formato = "docx", CancellationToken ct = default);

    // Word exports (Prompt 7)
    Task<ExportResultDto> ExportPlanningAsync(Guid planningId, CreateExportRequest request, CancellationToken ct = default);
    Task<ExportResultDto> ExportPlanningPackageAsync(Guid planningId, CreateExportRequest request, CancellationToken ct = default);
    Task<ExportResultDto> ExportClassAsync(Guid classId, CreateExportRequest request, CancellationToken ct = default);
    Task<ExportResultDto> ExportEducationalDocumentAsync(Guid documentId, CreateExportRequest request, CancellationToken ct = default);
    Task<ExportResultDto> ExportAnswerKeyAsync(Guid documentId, CreateExportRequest request, CancellationToken ct = default);
    Task<ExportResultDto> ExportSpecificationTableAsync(Guid documentId, CreateExportRequest request, CancellationToken ct = default);
    Task<ExportResultDto?> GetExportAsync(Guid exportId, CancellationToken ct = default);
    Task<IReadOnlyList<ExportSummaryDto>> GetExportsAsync(CancellationToken ct = default);
    Task<(byte[] Bytes, string FileName)> DownloadExportAsync(Guid exportId, CancellationToken ct = default);
    Task DeleteExportAsync(Guid exportId, CancellationToken ct = default);

    Task<IReadOnlyList<CurriculumAdminSourceDto>> GetCurriculumSourcesAsync(CancellationToken ct = default);
    Task ReloadCurriculumSourcesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CurriculumAdminBatchDto>> GetCurriculumBatchesAsync(CancellationToken ct = default);
    Task<ImportSummaryDto> CreateCurriculumImportAsync(string sourceIdOrExternalId, CancellationToken ct = default);
    Task<ImportSummaryDto> ProcessCurriculumImportAsync(Guid batchId, CancellationToken ct = default);
    Task<ImportSummaryDto> DownloadCurriculumImportAsync(Guid batchId, CancellationToken ct = default);
    Task<ImportSummaryDto> ExtractCurriculumImportAsync(Guid batchId, CancellationToken ct = default);
    Task<ImportSummaryDto> ValidateCurriculumImportAsync(Guid batchId, CancellationToken ct = default);
    Task<CurriculumImportPreviewDto?> GetCurriculumImportPreviewAsync(Guid batchId, CancellationToken ct = default);
    Task<CurriculumImportPreviewDto> UpdateCurriculumImportPreviewAsync(Guid batchId, CurriculumImportPreviewDto preview, CancellationToken ct = default);
    Task<string> GetCurriculumImportDiffAsync(Guid batchId, CancellationToken ct = default);
    Task<IReadOnlyList<ValidationIssueDto>> GetCurriculumImportIssuesAsync(Guid batchId, CancellationToken ct = default);
    Task ApproveCurriculumImportAsync(Guid batchId, CancellationToken ct = default);
    Task RejectCurriculumImportAsync(Guid batchId, string reason, CancellationToken ct = default);
    Task ImportCurriculumBatchAsync(Guid batchId, CancellationToken ct = default);
    Task PublishCurriculumImportAsync(Guid batchId, CancellationToken ct = default);
    Task<CurriculumVersionDto?> GetCurriculumVersionAsync(CancellationToken ct = default);

    // Curriculum review
    Task<CurriculumReviewSessionDto> StartCurriculumReviewAsync(Guid batchId, CancellationToken ct = default);
    Task<CurriculumReviewPackageDto?> GetCurriculumReviewAsync(Guid batchId, CancellationToken ct = default);
    Task<CurriculumReviewSummaryDto?> GetCurriculumReviewSummaryAsync(Guid batchId, CancellationToken ct = default);
    Task<CurriculumReviewPackageDto> UpdateReviewObjectiveAsync(Guid batchId, string temporaryId, UpdateReviewObjectiveRequest request, CancellationToken ct = default);
    Task<CurriculumReviewPackageDto> UpdateReviewIndicatorAsync(Guid batchId, string temporaryId, UpdateReviewIndicatorRequest request, CancellationToken ct = default);
    Task<CurriculumReviewPackageDto> UpdateReviewUnitAsync(Guid batchId, string temporaryId, UpdateReviewUnitRequest request, CancellationToken ct = default);
    Task<CurriculumReviewPackageDto> AddReviewObjectiveAsync(Guid batchId, AddReviewObjectiveRequest request, CancellationToken ct = default);
    Task<CurriculumReviewPackageDto> AddReviewIndicatorAsync(Guid batchId, string objectiveTemporaryId, AddReviewIndicatorRequest request, CancellationToken ct = default);
    Task<CurriculumValidationResultDto> RevalidateCurriculumReviewAsync(Guid batchId, CancellationToken ct = default);
    Task<RichCurriculumDiffResultDto?> GetCurriculumReviewDiffAsync(Guid batchId, CancellationToken ct = default);
    Task<IReadOnlyList<ReviewChangeDto>> GetCurriculumReviewChangesAsync(Guid batchId, CancellationToken ct = default);
    Task<IReadOnlyList<ReviewCommentDto>> GetCurriculumReviewCommentsAsync(Guid batchId, CancellationToken ct = default);
    Task<ReviewCommentDto> AddCurriculumReviewCommentAsync(Guid batchId, AddReviewCommentRequest request, CancellationToken ct = default);
    Task ResolveCurriculumReviewCommentAsync(Guid batchId, Guid commentId, CancellationToken ct = default);
    Task MarkCurriculumReviewReadyAsync(Guid batchId, CancellationToken ct = default);
    Task<CurriculumReviewPackageDto> BulkDecideCurriculumReviewAsync(Guid batchId, BulkDecisionRequest request, CancellationToken ct = default);

    // Planning calendar / sequence / coverage (Prompt 8)
    Task<PlanningCalendarDto?> GetPlanningCalendarAsync(Guid planningId, CancellationToken ct = default);
    Task<PlanningCalendarDto> ConfigurePlanningScheduleAsync(Guid planningId, ConfigurePlanningScheduleRequest request, CancellationToken ct = default);
    Task<PlanningCalendarDto> GenerateCalendarSessionsAsync(Guid planningId, GenerateCalendarSessionsRequest? request = null, CancellationToken ct = default);
    Task<PlanningCalendarDto> PreviewCalendarAsync(Guid planningId, CancellationToken ct = default);
    Task<PlanningSequenceProposalDto> GenerateSequenceProposalAsync(Guid planningId, GeneratePlanningSequenceRequest request, CancellationToken ct = default);
    Task<PlanningSequenceProposalDto?> GetCurrentSequenceProposalAsync(Guid planningId, CancellationToken ct = default);
    Task ConfirmSequenceProposalAsync(Guid proposalId, CancellationToken ct = default);
    Task<PlanningCoverageDto?> GetPlanningCoverageAsync(Guid planningId, string mode = "Planned", CancellationToken ct = default);
    Task<IReadOnlyList<PlanningAlertDto>> GetPlanningAlertsAsync(Guid planningId, CancellationToken ct = default);
    Task RescheduleSessionAsync(Guid sessionId, RescheduleSessionRequest request, CancellationToken ct = default);
    Task CancelSessionAsync(Guid sessionId, CancelPlanningSessionRequest request, CancellationToken ct = default);
    Task CompleteClassAsync(Guid classId, CompleteClassRequest request, CancellationToken ct = default);

    Task<TeacherDashboardDto> GetTeacherDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StudentDto>> GetStudentsAsync(Guid institutionId, CancellationToken ct = default);
    Task<StudentDto> CreateStudentAsync(Guid institutionId, CreateStudentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<SchoolCourseDto>> GetCoursesAsync(Guid institutionId, CancellationToken ct = default);
    Task<SchoolCourseDto?> GetCourseAsync(Guid courseId, CancellationToken ct = default);
    Task<IReadOnlyList<AcademicPeriodDto>> GetAcademicPeriodsAsync(Guid institutionId, CancellationToken ct = default);
    Task<AcademicPeriodDto> CreateAcademicPeriodAsync(Guid institutionId, CreateAcademicPeriodRequest request, CancellationToken ct = default);
    Task<SchoolCourseDto> CreateCourseAsync(Guid institutionId, CreateSchoolCourseRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<CourseSubjectDto>> GetCourseSubjectsAsync(Guid courseId, CancellationToken ct = default);
    Task<CourseRosterDto?> GetRosterAsync(Guid courseId, CancellationToken ct = default);
    Task<CourseRosterDto?> GetClassRosterAsync(Guid classId, CancellationToken ct = default);
    Task EnrollStudentAsync(Guid courseId, Guid studentId, CancellationToken ct = default);
    Task<SupportPlanDto> AddSupportPlanAsync(Guid studentId, CreateSupportPlanRequest request, CancellationToken ct = default);
    Task SaveAttendanceAsync(Guid classId, SaveAttendanceRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(Guid classId, CancellationToken ct = default);
    Task<IReadOnlyList<ClassDuaStrategyDto>> GetDuaStrategiesAsync(Guid classId, CancellationToken ct = default);
    Task<ClassDuaStrategyDto> AddDuaStrategyAsync(Guid classId, AddClassDuaStrategyRequest request, CancellationToken ct = default);
    Task<LearningAssessmentDto> CreateAssessmentAsync(CreateLearningAssessmentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<LearningAssessmentDto>> GetAssessmentsAsync(Guid? courseId = null, Guid? classId = null, CancellationToken ct = default);
    Task<IReadOnlyList<AssessmentScoreDto>> GetAssessmentScoresAsync(Guid assessmentId, CancellationToken ct = default);
    Task SaveAssessmentScoresAsync(Guid assessmentId, IReadOnlyList<SaveAssessmentScoreRequest> scores, CancellationToken ct = default);
    Task<AssessmentEvidenceSummaryDto?> GetAssessmentEvidenceAsync(Guid assessmentId, CancellationToken ct = default);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<NivelDto>> GetNivelesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<NivelDto>>("api/curriculum/niveles", JsonOptions, ct) ?? [];

    public async Task<IReadOnlyList<AsignaturaDto>> GetAsignaturasAsync(Guid nivelId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<AsignaturaDto>>(
            $"api/curriculum/asignaturas?nivelId={nivelId}", JsonOptions, ct) ?? [];

    public async Task<IReadOnlyList<UnidadDto>> GetUnidadesAsync(Guid asignaturaId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<UnidadDto>>(
            $"api/curriculum/unidades?asignaturaId={asignaturaId}", JsonOptions, ct) ?? [];

    public async Task<IReadOnlyList<ObjetivoAprendizajeDto>> GetObjetivosAsync(Guid unidadId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<ObjetivoAprendizajeDto>>(
            $"api/curriculum/objetivos?unidadId={unidadId}", JsonOptions, ct) ?? [];

    public async Task<ObjetivoAprendizajeDetalleDto?> GetObjetivoDetalleAsync(Guid oaId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/curriculum/objetivos/{oaId}/detalle", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ObjetivoAprendizajeDetalleDto>(JsonOptions, ct);
    }

    public async Task<IReadOnlyList<PlanificacionResumenDto>> GetPlanificacionesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<PlanificacionResumenDto>>("api/planificaciones", JsonOptions, ct) ?? [];

    public async Task<PlanificacionDetalleDto?> GetPlanificacionAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/planificaciones/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<PlanificacionDetalleDto>(JsonOptions, ct);
    }

    public async Task<PlanificacionDetalleDto> CrearPlanificacionAsync(CrearPlanificacionRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync("api/planificaciones", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<PlanificacionDetalleDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al crear planificación.");
    }

    public async Task<(byte[] Bytes, string FileName)> ExportarPlanificacionAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"api/planificaciones/{id}/exportar", null, ct);
        await EnsureSuccess(response, ct);
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        return (bytes, FileNameFrom(response, "planificacion.docx"));
    }

    public async Task<ClaseDetalleDto> AgregarClaseAsync(Guid planificacionId, CrearClaseRequest? request = null, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/planificaciones/{planificacionId}/clases", request ?? new CrearClaseRequest(), JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ClaseDetalleDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al agregar clase.");
    }

    public async Task<ClaseDetalleDto?> GetClaseAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/clases/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<ClaseDetalleDto>(JsonOptions, ct);
    }

    public async Task<ClaseDetalleDto> ActualizarClaseAsync(Guid id, ActualizarClaseRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/clases/{id}", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ClaseDetalleDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al actualizar clase.");
    }

    public async Task EliminarClaseAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync($"api/clases/{id}", ct);
        await EnsureSuccess(response, ct);
    }

    public async Task<ClaseDetalleDto> GenerarEstructuraClaseAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"api/clases/{id}/generar-estructura", null, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ClaseDetalleDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al generar estructura.");
    }

    public async Task<ClassStructureGenerationResultDto> GenerateClassStructureAsync(
        Guid classId, GenerateClassStructureRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/clases/{classId}/generate-structure", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ClassStructureGenerationResultDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al generar estructura avanzada.");
    }

    public async Task<IReadOnlyList<ClassStructureGenerationSummaryDto>> GetStructureGenerationsAsync(
        Guid classId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<ClassStructureGenerationSummaryDto>>(
            $"api/clases/{classId}/structure-generations", JsonOptions, ct) ?? [];

    public async Task<ClassStructureGenerationResultDto?> GetCurrentStructureAsync(
        Guid classId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/clases/{classId}/structure-generations/current", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<ClassStructureGenerationResultDto>(JsonOptions, ct);
    }

    public async Task<ClassStructureGenerationResultDto?> GetStructureGenerationAsync(
        Guid generationId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/structure-generations/{generationId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<ClassStructureGenerationResultDto>(JsonOptions, ct);
    }

    public async Task<ClassGenerationContextDto?> GetGenerationContextAsync(
        Guid classId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/clases/{classId}/generation-context", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<ClassGenerationContextDto>(JsonOptions, ct);
    }

    public async Task<ClassStructureGenerationResultDto> UpdateStructureContentAsync(
        Guid generationId, UpdateClassStructureContentRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync(
            $"api/structure-generations/{generationId}/content", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ClassStructureGenerationResultDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al actualizar estructura.");
    }

    public async Task<ClassStructureGenerationResultDto> SetCurrentStructureAsync(
        Guid generationId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"api/structure-generations/{generationId}/set-current", null, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ClassStructureGenerationResultDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al establecer estructura vigente.");
    }

    public async Task<ClassStructureGenerationResultDto> RetryStructureGenerationAsync(
        Guid generationId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"api/structure-generations/{generationId}/retry", null, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ClassStructureGenerationResultDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al reintentar generación.");
    }

    public async Task<EducationalDocumentGenerationResultDto> GenerateEducationalDocumentAsync(
        Guid classId, GenerateEducationalDocumentRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/clases/{classId}/educational-documents/generate", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<EducationalDocumentGenerationResultDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al generar documento educativo.");
    }

    public async Task<IReadOnlyList<EducationalDocumentSummaryDto>> GetEducationalDocumentsAsync(
        Guid classId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<EducationalDocumentSummaryDto>>(
               $"api/clases/{classId}/educational-documents", JsonOptions, ct) ?? [];

    public async Task<IReadOnlyList<EducationalDocumentSummaryDto>> GetMaterialLibraryAsync(
        Guid? courseId = null, string? type = null, string? search = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (courseId is Guid c) qs.Add($"courseId={c}");
        if (!string.IsNullOrWhiteSpace(type)) qs.Add($"type={Uri.EscapeDataString(type)}");
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"q={Uri.EscapeDataString(search)}");
        var suffix = qs.Count == 0 ? string.Empty : "?" + string.Join("&", qs);
        return await _http.GetFromJsonAsync<List<EducationalDocumentSummaryDto>>(
                   $"api/biblioteca/materiales{suffix}", JsonOptions, ct) ?? [];
    }

    public async Task<EducationalDocumentDetailDto?> GetEducationalDocumentAsync(
        Guid documentId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/educational-documents/{documentId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<EducationalDocumentDetailDto>(JsonOptions, ct);
    }

    public async Task<EducationalDocumentStudentViewDto?> GetEducationalDocumentStudentViewAsync(
        Guid documentId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/educational-documents/{documentId}/student-view", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<EducationalDocumentStudentViewDto>(JsonOptions, ct);
    }

    public async Task<EducationalDocumentDetailDto> UpdateEducationalDocumentAsync(
        Guid documentId, UpdateEducationalDocumentRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync(
            $"api/educational-documents/{documentId}", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<EducationalDocumentDetailDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al actualizar documento.");
    }

    public async Task<EducationalDocumentDetailDto> UpdateEducationalDocumentStatusAsync(
        Guid documentId, UpdateEducationalDocumentStatusRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync(
            $"api/educational-documents/{documentId}/status", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<EducationalDocumentDetailDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al cambiar estado.");
    }

    public async Task<EducationalItemDto> UpdateEducationalItemAsync(
        Guid itemId, UpdateEducationalItemRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync(
            $"api/educational-items/{itemId}", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<EducationalItemDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al actualizar ítem.");
    }

    public async Task<EducationalItemDto> RegenerateEducationalItemAsync(
        Guid itemId, RegenerateEducationalItemRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/educational-items/{itemId}/regenerate", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<EducationalItemDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al regenerar ítem.");
    }

    public async Task<EducationalDocumentDetailDto> ReorderEducationalItemsAsync(
        Guid documentId, ReorderEducationalItemsRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/educational-documents/{documentId}/items/reorder", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<EducationalDocumentDetailDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al reordenar.");
    }

    public async Task<AnswerKeyDto> GetEducationalAnswerKeyAsync(Guid documentId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/educational-documents/{documentId}/answer-key", ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<AnswerKeyDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía de clave.");
    }

    public async Task<EducationalDocumentValidationResultDto> ValidateEducationalDocumentAsync(
        Guid documentId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"api/educational-documents/{documentId}/validate", null, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<EducationalDocumentValidationResultDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía de validación.");
    }

    public async Task<DocumentoDto> GenerarMaterialClaseAsync(Guid id, GenerarMaterialClaseRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/clases/{id}/generar-material", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<DocumentoDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al generar material.");
    }

    public async Task<DocumentoDto?> GetDocumentoAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/documentos/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<DocumentoDto>(JsonOptions, ct);
    }

    public async Task<DocumentoDto> ActualizarDocumentoAsync(Guid id, ActualizarDocumentoRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/documentos/{id}", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<DocumentoDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al actualizar documento.");
    }

    public async Task<(byte[] Bytes, string FileName)> ExportarDocumentoAsync(Guid id, string formato = "docx", CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new { formato }, JsonOptions);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync($"api/documentos/{id}/exportar", content, ct);
        await EnsureSuccess(response, ct);
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fallback = formato.Equals("pdf", StringComparison.OrdinalIgnoreCase) ? "documento.pdf" : "documento.docx";
        return (bytes, FileNameFrom(response, fallback));
    }

    public async Task<ExportResultDto> ExportPlanningAsync(Guid planningId, CreateExportRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/planificaciones/{planningId}/export", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ExportResultDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al exportar planificación.");
    }

    public async Task<ExportResultDto> ExportPlanningPackageAsync(Guid planningId, CreateExportRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/planificaciones/{planningId}/export-package", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ExportResultDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al exportar paquete.");
    }

    public async Task<ExportResultDto> ExportClassAsync(Guid classId, CreateExportRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/clases/{classId}/export", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ExportResultDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al exportar clase.");
    }

    public async Task<ExportResultDto> ExportEducationalDocumentAsync(Guid documentId, CreateExportRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/educational-documents/{documentId}/export", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ExportResultDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al exportar material.");
    }

    public async Task<ExportResultDto> ExportAnswerKeyAsync(Guid documentId, CreateExportRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/educational-documents/{documentId}/export-answer-key", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ExportResultDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al exportar clave.");
    }

    public async Task<ExportResultDto> ExportSpecificationTableAsync(Guid documentId, CreateExportRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/educational-documents/{documentId}/export-specification-table", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ExportResultDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al exportar especificaciones.");
    }

    public async Task<ExportResultDto?> GetExportAsync(Guid exportId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/exports/{exportId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<ExportResultDto>(JsonOptions, ct);
    }

    public async Task<IReadOnlyList<ExportSummaryDto>> GetExportsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<ExportSummaryDto>>("api/exports", JsonOptions, ct) ?? [];

    public async Task<(byte[] Bytes, string FileName)> DownloadExportAsync(Guid exportId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/exports/{exportId}/download", ct);
        await EnsureSuccess(response, ct);
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        return (bytes, FileNameFrom(response, "export.docx"));
    }

    public async Task DeleteExportAsync(Guid exportId, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync($"api/exports/{exportId}", ct);
        await EnsureSuccess(response, ct);
    }

    public async Task<IReadOnlyList<CurriculumAdminSourceDto>> GetCurriculumSourcesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<CurriculumAdminSourceDto>>("api/admin/curriculum/sources", JsonOptions, ct) ?? [];

    public async Task ReloadCurriculumSourcesAsync(CancellationToken ct = default)
    {
        using var response = await _http.PostAsync("api/admin/curriculum/sources/reload", null, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task<IReadOnlyList<CurriculumAdminBatchDto>> GetCurriculumBatchesAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<CurriculumAdminBatchDto>>("api/admin/curriculum/imports", JsonOptions, ct) ?? [];

    public async Task<ImportSummaryDto> CreateCurriculumImportAsync(string sourceIdOrExternalId, CancellationToken ct = default)
    {
        using var content = JsonContent.Create(new { sourceId = sourceIdOrExternalId }, options: JsonOptions);
        using var response = await _http.PostAsync("api/admin/curriculum/imports", content, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ImportSummaryDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al crear lote.");
    }

    public async Task<ImportSummaryDto> ProcessCurriculumImportAsync(Guid batchId, CancellationToken ct = default)
        => await PostImportActionAsync(batchId, "process", ct);

    public async Task<ImportSummaryDto> DownloadCurriculumImportAsync(Guid batchId, CancellationToken ct = default)
        => await PostImportActionAsync(batchId, "download", ct);

    public async Task<ImportSummaryDto> ExtractCurriculumImportAsync(Guid batchId, CancellationToken ct = default)
        => await PostImportActionAsync(batchId, "extract", ct);

    public async Task<ImportSummaryDto> ValidateCurriculumImportAsync(Guid batchId, CancellationToken ct = default)
        => await PostImportActionAsync(batchId, "validate", ct);

    public async Task<CurriculumImportPreviewDto?> GetCurriculumImportPreviewAsync(Guid batchId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<CurriculumImportPreviewDto>($"api/admin/curriculum/imports/{batchId}/preview", JsonOptions, ct);

    public async Task<CurriculumImportPreviewDto> UpdateCurriculumImportPreviewAsync(
        Guid batchId, CurriculumImportPreviewDto preview, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/admin/curriculum/imports/{batchId}/preview", preview, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<CurriculumImportPreviewDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al actualizar vista previa.");
    }

    public async Task<string> GetCurriculumImportDiffAsync(Guid batchId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/admin/curriculum/imports/{batchId}/diff", ct);
        await EnsureSuccess(response, ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<IReadOnlyList<ValidationIssueDto>> GetCurriculumImportIssuesAsync(Guid batchId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<ValidationIssueDto>>($"api/admin/curriculum/imports/{batchId}/issues", JsonOptions, ct) ?? [];

    public async Task ApproveCurriculumImportAsync(Guid batchId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"api/admin/curriculum/imports/{batchId}/approve", null, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task RejectCurriculumImportAsync(Guid batchId, string reason, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/admin/curriculum/imports/{batchId}/reject",
            new RejectReviewRequest { Reason = reason },
            JsonOptions,
            ct);
        await EnsureSuccess(response, ct);
    }

    public async Task ImportCurriculumBatchAsync(Guid batchId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"api/admin/curriculum/imports/{batchId}/import", null, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task PublishCurriculumImportAsync(Guid batchId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"api/admin/curriculum/imports/{batchId}/publish", null, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task<CurriculumVersionDto?> GetCurriculumVersionAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<CurriculumVersionDto>("api/curriculum/version", JsonOptions, ct);

    private string ReviewBase(Guid batchId) => $"api/admin/curriculum/imports/{batchId}/review";

    public async Task<CurriculumReviewSessionDto> StartCurriculumReviewAsync(Guid batchId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"{ReviewBase(batchId)}/start", null, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<CurriculumReviewSessionDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al iniciar revisión.");
    }

    public async Task<CurriculumReviewPackageDto?> GetCurriculumReviewAsync(Guid batchId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(ReviewBase(batchId), ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<CurriculumReviewPackageDto>(JsonOptions, ct);
    }

    public async Task<CurriculumReviewSummaryDto?> GetCurriculumReviewSummaryAsync(Guid batchId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"{ReviewBase(batchId)}/summary", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<CurriculumReviewSummaryDto>(JsonOptions, ct);
    }

    public async Task<CurriculumReviewPackageDto> UpdateReviewObjectiveAsync(
        Guid batchId, string temporaryId, UpdateReviewObjectiveRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync(
            $"{ReviewBase(batchId)}/objectives/{Uri.EscapeDataString(temporaryId)}", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<CurriculumReviewPackageDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al actualizar OA.");
    }

    public async Task<CurriculumReviewPackageDto> UpdateReviewIndicatorAsync(
        Guid batchId, string temporaryId, UpdateReviewIndicatorRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync(
            $"{ReviewBase(batchId)}/indicators/{Uri.EscapeDataString(temporaryId)}", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<CurriculumReviewPackageDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al actualizar indicador.");
    }

    public async Task<CurriculumReviewPackageDto> UpdateReviewUnitAsync(
        Guid batchId, string temporaryId, UpdateReviewUnitRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync(
            $"{ReviewBase(batchId)}/units/{Uri.EscapeDataString(temporaryId)}", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<CurriculumReviewPackageDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al actualizar unidad.");
    }

    public async Task<CurriculumReviewPackageDto> AddReviewObjectiveAsync(
        Guid batchId, AddReviewObjectiveRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"{ReviewBase(batchId)}/objectives", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<CurriculumReviewPackageDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al agregar OA.");
    }

    public async Task<CurriculumReviewPackageDto> AddReviewIndicatorAsync(
        Guid batchId, string objectiveTemporaryId, AddReviewIndicatorRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"{ReviewBase(batchId)}/objectives/{Uri.EscapeDataString(objectiveTemporaryId)}/indicators",
            request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<CurriculumReviewPackageDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al agregar indicador.");
    }

    public async Task<CurriculumValidationResultDto> RevalidateCurriculumReviewAsync(Guid batchId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"{ReviewBase(batchId)}/revalidate", null, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<CurriculumValidationResultDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al revalidar.");
    }

    public async Task<RichCurriculumDiffResultDto?> GetCurriculumReviewDiffAsync(Guid batchId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"{ReviewBase(batchId)}/diff", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<RichCurriculumDiffResultDto>(JsonOptions, ct);
    }

    public async Task<IReadOnlyList<ReviewChangeDto>> GetCurriculumReviewChangesAsync(Guid batchId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<ReviewChangeDto>>($"{ReviewBase(batchId)}/changes", JsonOptions, ct) ?? [];

    public async Task<IReadOnlyList<ReviewCommentDto>> GetCurriculumReviewCommentsAsync(Guid batchId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<ReviewCommentDto>>($"{ReviewBase(batchId)}/comments", JsonOptions, ct) ?? [];

    public async Task<ReviewCommentDto> AddCurriculumReviewCommentAsync(
        Guid batchId, AddReviewCommentRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"{ReviewBase(batchId)}/comments", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ReviewCommentDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al agregar comentario.");
    }

    public async Task ResolveCurriculumReviewCommentAsync(Guid batchId, Guid commentId, CancellationToken ct = default)
    {
        using var response = await _http.PutAsync($"{ReviewBase(batchId)}/comments/{commentId}/resolve", null, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task MarkCurriculumReviewReadyAsync(Guid batchId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"{ReviewBase(batchId)}/ready", null, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task<CurriculumReviewPackageDto> BulkDecideCurriculumReviewAsync(
        Guid batchId, BulkDecisionRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"{ReviewBase(batchId)}/bulk-decide", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<CurriculumReviewPackageDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía en decisión masiva.");
    }

    public async Task<PlanningCalendarDto?> GetPlanningCalendarAsync(Guid planningId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/planificaciones/{planningId}/calendario", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PlanningCalendarDto>(JsonOptions, ct);
    }

    public async Task<PlanningCalendarDto> ConfigurePlanningScheduleAsync(
        Guid planningId, ConfigurePlanningScheduleRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/planificaciones/{planningId}/calendario/configuracion", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<PlanningCalendarDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al configurar horario.");
    }

    public async Task<PlanningCalendarDto> GenerateCalendarSessionsAsync(
        Guid planningId, GenerateCalendarSessionsRequest? request = null, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/planificaciones/{planningId}/calendario/generar",
            request ?? new GenerateCalendarSessionsRequest(), JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<PlanningCalendarDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al generar sesiones.");
    }

    public async Task<PlanningCalendarDto> PreviewCalendarAsync(Guid planningId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"api/planificaciones/{planningId}/calendario/vista-previa", null, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<PlanningCalendarDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía en vista previa.");
    }

    public async Task<PlanningSequenceProposalDto> GenerateSequenceProposalAsync(
        Guid planningId, GeneratePlanningSequenceRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/planificaciones/{planningId}/secuencia/propuestas", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<PlanningSequenceProposalDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException("Respuesta vacía al generar secuencia.");
    }

    public async Task<PlanningSequenceProposalDto?> GetCurrentSequenceProposalAsync(Guid planningId, CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<List<PlanningSequenceProposalDto>>(
            $"api/planificaciones/{planningId}/secuencia/propuestas", JsonOptions, ct) ?? [];
        return list.FirstOrDefault(p => p.IsCurrent) ?? list.FirstOrDefault();
    }

    public async Task ConfirmSequenceProposalAsync(Guid proposalId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"api/secuencia/propuestas/{proposalId}/confirmar", null, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task<PlanningCoverageDto?> GetPlanningCoverageAsync(Guid planningId, string mode = "Planned", CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/planificaciones/{planningId}/cobertura?mode={mode}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PlanningCoverageDto>(JsonOptions, ct);
    }

    public async Task<IReadOnlyList<PlanningAlertDto>> GetPlanningAlertsAsync(Guid planningId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<PlanningAlertDto>>(
            $"api/planificaciones/{planningId}/alertas", JsonOptions, ct) ?? [];

    public async Task RescheduleSessionAsync(Guid sessionId, RescheduleSessionRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/calendario/sesiones/{sessionId}/reprogramar", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task CancelSessionAsync(Guid sessionId, CancelPlanningSessionRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/calendario/sesiones/{sessionId}/cancelar", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task CompleteClassAsync(Guid classId, CompleteClassRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/clases/{classId}/completar", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task<TeacherDashboardDto> GetTeacherDashboardAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<TeacherDashboardDto>("api/teacher/dashboard", JsonOptions, ct)
           ?? new TeacherDashboardDto();

    public async Task<IReadOnlyList<StudentDto>> GetStudentsAsync(Guid institutionId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<StudentDto>>($"api/institutions/{institutionId}/students", JsonOptions, ct) ?? [];

    public async Task<StudentDto> CreateStudentAsync(Guid institutionId, CreateStudentRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/institutions/{institutionId}/students", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<StudentDto>(JsonOptions, ct))!;
    }

    public async Task<IReadOnlyList<SchoolCourseDto>> GetCoursesAsync(Guid institutionId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<SchoolCourseDto>>($"api/institutions/{institutionId}/courses", JsonOptions, ct) ?? [];

    public async Task<SchoolCourseDto?> GetCourseAsync(Guid courseId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<SchoolCourseDto>($"api/courses/{courseId}", JsonOptions, ct);

    public async Task<IReadOnlyList<AcademicPeriodDto>> GetAcademicPeriodsAsync(Guid institutionId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<AcademicPeriodDto>>($"api/institutions/{institutionId}/academic-periods", JsonOptions, ct) ?? [];

    public async Task<AcademicPeriodDto> CreateAcademicPeriodAsync(Guid institutionId, CreateAcademicPeriodRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/institutions/{institutionId}/academic-periods", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<AcademicPeriodDto>(JsonOptions, ct))!;
    }

    public async Task<SchoolCourseDto> CreateCourseAsync(Guid institutionId, CreateSchoolCourseRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/institutions/{institutionId}/courses", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<SchoolCourseDto>(JsonOptions, ct))!;
    }

    public async Task<IReadOnlyList<CourseSubjectDto>> GetCourseSubjectsAsync(Guid courseId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<CourseSubjectDto>>($"api/courses/{courseId}/subjects", JsonOptions, ct) ?? [];

    public async Task<CourseRosterDto?> GetRosterAsync(Guid courseId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/courses/{courseId}/roster", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CourseRosterDto>(JsonOptions, ct);
    }

    public async Task<CourseRosterDto?> GetClassRosterAsync(Guid classId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/clases/{classId}/roster", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CourseRosterDto>(JsonOptions, ct);
    }

    public async Task EnrollStudentAsync(Guid courseId, Guid studentId, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/courses/{courseId}/roster", new EnrollStudentRequest { StudentId = studentId }, JsonOptions, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task<SupportPlanDto> AddSupportPlanAsync(Guid studentId, CreateSupportPlanRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/students/{studentId}/support-plans", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<SupportPlanDto>(JsonOptions, ct))!;
    }

    public async Task SaveAttendanceAsync(Guid classId, SaveAttendanceRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/clases/{classId}/asistencia", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(Guid classId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<AttendanceRecordDto>>($"api/clases/{classId}/asistencia", JsonOptions, ct) ?? [];

    public async Task<IReadOnlyList<ClassDuaStrategyDto>> GetDuaStrategiesAsync(Guid classId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<ClassDuaStrategyDto>>($"api/clases/{classId}/dua", JsonOptions, ct) ?? [];

    public async Task<ClassDuaStrategyDto> AddDuaStrategyAsync(Guid classId, AddClassDuaStrategyRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/clases/{classId}/dua", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ClassDuaStrategyDto>(JsonOptions, ct))!;
    }

    public async Task<LearningAssessmentDto> CreateAssessmentAsync(CreateLearningAssessmentRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync("api/evaluaciones", request, JsonOptions, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<LearningAssessmentDto>(JsonOptions, ct))!;
    }

    public async Task<IReadOnlyList<LearningAssessmentDto>> GetAssessmentsAsync(
        Guid? courseId = null, Guid? classId = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (courseId is Guid c) qs.Add($"courseId={c}");
        if (classId is Guid cl) qs.Add($"classId={cl}");
        var suffix = qs.Count == 0 ? string.Empty : "?" + string.Join("&", qs);
        return await _http.GetFromJsonAsync<List<LearningAssessmentDto>>($"api/evaluaciones{suffix}", JsonOptions, ct) ?? [];
    }

    public async Task<IReadOnlyList<AssessmentScoreDto>> GetAssessmentScoresAsync(Guid assessmentId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<AssessmentScoreDto>>($"api/evaluaciones/{assessmentId}/puntajes", JsonOptions, ct) ?? [];

    public async Task SaveAssessmentScoresAsync(Guid assessmentId, IReadOnlyList<SaveAssessmentScoreRequest> scores, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/evaluaciones/{assessmentId}/puntajes", scores, JsonOptions, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task<AssessmentEvidenceSummaryDto?> GetAssessmentEvidenceAsync(Guid assessmentId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/evaluaciones/{assessmentId}/evidencia", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<AssessmentEvidenceSummaryDto>(JsonOptions, ct);
    }

    private async Task<ImportSummaryDto> PostImportActionAsync(Guid batchId, string action, CancellationToken ct)
    {
        using var response = await _http.PostAsync($"api/admin/curriculum/imports/{batchId}/{action}", null, ct);
        await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<ImportSummaryDto>(JsonOptions, ct))
               ?? throw new InvalidOperationException($"Respuesta vacía en {action}.");
    }

    private static string FileNameFrom(HttpResponseMessage response, string fallback)
    {
        var disposition = response.Content.Headers.ContentDisposition;
        if (!string.IsNullOrWhiteSpace(disposition?.FileNameStar))
            return disposition.FileNameStar.Trim('"');
        if (!string.IsNullOrWhiteSpace(disposition?.FileName))
            return disposition.FileName.Trim('"');
        return fallback;
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"API {(int)response.StatusCode}: {body}");
    }
}
