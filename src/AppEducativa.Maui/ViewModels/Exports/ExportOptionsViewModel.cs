using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Exports;

[QueryProperty(nameof(PlanningId), "planningId")]
[QueryProperty(nameof(ClassId), "classId")]
[QueryProperty(nameof(DocumentId), "documentId")]
[QueryProperty(nameof(Context), "context")]
public partial class ExportOptionsViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public ExportOptionsViewModel(IApiClient api) => _api = api;

    [ObservableProperty] private string planningId = string.Empty;
    [ObservableProperty] private string classId = string.Empty;
    [ObservableProperty] private string documentId = string.Empty;
    [ObservableProperty] private string context = "planning";
    [ObservableProperty] private string audience = "Teacher";
    [ObservableProperty] private bool includeCurriculum = true;
    [ObservableProperty] private bool includeAnswerKey;
    [ObservableProperty] private bool includeSpecTable;
    [ObservableProperty] private bool confirmOutdated;
    [ObservableProperty] private string? schoolName;
    [ObservableProperty] private string? teacherName;
    [ObservableProperty] private string? courseName;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    [RelayCommand]
    private async Task ExportarAsync(string? mode)
    {
        try
        {
            IsBusy = true;
            MensajeEstado = "Preparando datos…";
            if (!Enum.TryParse<ExportAudience>(Audience, true, out var audience))
                audience = ExportAudience.Teacher;

            var request = new CreateExportRequest
            {
                Audience = audience,
                IncludeCurriculumReferences = IncludeCurriculum,
                IncludeAnswerKey = IncludeAnswerKey && audience != ExportAudience.Student,
                IncludeSpecificationTable = IncludeSpecTable && audience != ExportAudience.Student,
                IncludeTeacherNotes = audience != ExportAudience.Student,
                ConfirmOutdatedExport = ConfirmOutdated,
                SchoolName = SchoolName,
                TeacherName = TeacherName,
                CourseName = CourseName
            };

            ExportResultDto result;
            MensajeEstado = "Generando documento…";
            switch ((mode ?? Context).ToLowerInvariant())
            {
                case "package":
                    if (!Guid.TryParse(PlanningId, out var pkgId)) throw new InvalidOperationException("Planificación requerida.");
                    result = await _api.ExportPlanningPackageAsync(pkgId, request);
                    break;
                case "class":
                    if (!Guid.TryParse(ClassId, out var cId)) throw new InvalidOperationException("Clase requerida.");
                    result = await _api.ExportClassAsync(cId, request);
                    break;
                case "document":
                case "guide":
                case "exercises":
                case "assessment":
                    if (!Guid.TryParse(DocumentId, out var dId)) throw new InvalidOperationException("Documento requerido.");
                    request.DocumentType = mode?.ToLowerInvariant() switch
                    {
                        "guide" => ExportDocumentType.LearningGuide,
                        "exercises" => ExportDocumentType.Exercises,
                        _ => ExportDocumentType.Assessment
                    };
                    result = await _api.ExportEducationalDocumentAsync(dId, request);
                    break;
                case "answerkey":
                    if (!Guid.TryParse(DocumentId, out var akId)) throw new InvalidOperationException("Documento requerido.");
                    result = await _api.ExportAnswerKeyAsync(akId, request);
                    break;
                case "spec":
                    if (!Guid.TryParse(DocumentId, out var spId)) throw new InvalidOperationException("Documento requerido.");
                    result = await _api.ExportSpecificationTableAsync(spId, request);
                    break;
                default:
                    if (!Guid.TryParse(PlanningId, out var pId)) throw new InvalidOperationException("Planificación requerida.");
                    result = await _api.ExportPlanningAsync(pId, request);
                    break;
            }

            await Shell.Current.GoToAsync($"exportProgress?exportId={result.ExportId}");
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task IrHistorialAsync() => await Shell.Current.GoToAsync("exportHistory");
}
