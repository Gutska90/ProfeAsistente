using System.Collections.ObjectModel;
using System.Text;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using AppEducativa.Shared.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Documents;

[QueryProperty(nameof(DocumentIdText), "documentId")]
[QueryProperty(nameof(ClaseId), "id")]
public partial class EducationalDocumentEditorViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public EducationalDocumentEditorViewModel(IApiClient api) => _api = api;

    public ObservableCollection<EducationalItemDto> Items { get; } = [];
    public ObservableCollection<AssessmentSpecificationRowDto> Specs { get; } = [];

    [ObservableProperty] private string documentIdText = string.Empty;
    [ObservableProperty] private string claseId = string.Empty;
    [ObservableProperty] private Guid documentId;
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string instructions = string.Empty;
    [ObservableProperty] private string status = string.Empty;
    [ObservableProperty] private string statusLabel = string.Empty;
    [ObservableProperty] private string documentType = string.Empty;
    [ObservableProperty] private string typeLabel = string.Empty;
    [ObservableProperty] private string curriculumRefs = string.Empty;
    [ObservableProperty] private decimal? totalPoints;
    [ObservableProperty] private int? duration;
    [ObservableProperty] private bool isOutdated;
    [ObservableProperty] private bool isTeacherView = true;
    [ObservableProperty] private string studentPreview = string.Empty;
    [ObservableProperty] private string comparisonText = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string? rowVersion;

    partial void OnDocumentIdTextChanged(string value)
    {
        if (Guid.TryParse(value, out var id))
        {
            DocumentId = id;
            _ = CargarAsync();
        }
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (DocumentId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            var doc = await _api.GetEducationalDocumentAsync(DocumentId);
            if (doc is null)
            {
                MensajeEstado = "Documento no encontrado.";
                return;
            }

            Apply(doc);
            MensajeEstado = $"{TypeLabel} · {StatusLabel}";
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
    private async Task GuardarAsync()
    {
        try
        {
            IsBusy = true;
            var doc = await _api.UpdateEducationalDocumentAsync(DocumentId, new UpdateEducationalDocumentRequest
            {
                RowVersion = RowVersion,
                Title = Title,
                Instructions = Instructions,
                EstimatedDurationMinutes = Duration,
                TotalPoints = TotalPoints,
                ChangeSummary = "Edición desde MAUI"
            });
            Apply(doc);
            MensajeEstado = "Material guardado.";
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
    private async Task ValidarAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _api.ValidateEducationalDocumentAsync(DocumentId);
            MensajeEstado = result.IsValid
                ? "Validación OK."
                : "Errores: " + string.Join(" ", result.Errors);
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
    private async Task CambiarEstadoAsync(string? statusName)
    {
        if (!Enum.TryParse<EducationalDocumentStatus>(statusName, true, out var status)) return;
        try
        {
            IsBusy = true;
            var doc = await _api.UpdateEducationalDocumentStatusAsync(DocumentId,
                new UpdateEducationalDocumentStatusRequest { Status = status });
            Apply(doc);
            MensajeEstado = $"Estado: {doc.Status}";
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
    private async Task VistaEstudianteAsync()
    {
        try
        {
            IsBusy = true;
            IsTeacherView = false;
            var view = await _api.GetEducationalDocumentStudentViewAsync(DocumentId);
            if (view is null)
            {
                StudentPreview = "Sin vista estudiante.";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine(view.Title);
            sb.AppendLine(view.Instructions);
            sb.AppendLine();
            foreach (var item in view.Items)
            {
                sb.AppendLine($"{item.Order}. {item.Statement} ({item.Points} pts)");
                foreach (var opt in item.Options)
                    sb.AppendLine($"   {opt.Order}) {opt.Text}");
                sb.AppendLine();
            }

            StudentPreview = sb.ToString();
            // Confirm no answer leakage in rendered text
            if (StudentPreview.Contains("IsCorrect", StringComparison.OrdinalIgnoreCase)
                || StudentPreview.Contains("ExpectedAnswer", StringComparison.OrdinalIgnoreCase))
                MensajeEstado = "Advertencia: posible fuga de clave.";
            else
                MensajeEstado = "Vista estudiante (sin respuestas).";
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
    private async Task VistaDocenteAsync()
    {
        IsTeacherView = true;
        await CargarAsync();
    }

    [RelayCommand]
    private async Task RegenerarItemAsync(EducationalItemDto? item)
    {
        if (item is null) return;
        try
        {
            IsBusy = true;
            MensajeEstado = $"Regenerando ítem {item.Order}…";
            await _api.RegenerateEducationalItemAsync(item.Id, new RegenerateEducationalItemRequest
            {
                Reason = "Regeneración solicitada desde el editor",
                KeepItemType = true,
                KeepIndicator = true
            });
            await CargarAsync();
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
    private async Task ExportarAsync(string? audience)
    {
        if (DocumentId == Guid.Empty) return;
        var aud = string.IsNullOrWhiteSpace(audience) ? "Student" : audience;
        await Shell.Current.GoToAsync(
            $"exportOptions?documentId={DocumentId}&context=document&planningId=&classId=");
        // Audience is set on the options page; pre-select via query is not wired — user chooses Student/Teacher.
        _ = aud;
    }

    [RelayCommand]
    private async Task CompararAsync()
    {
        if (!Guid.TryParse(ClaseId, out var claseGuid)) return;
        try
        {
            IsBusy = true;
            var list = await _api.GetEducationalDocumentsAsync(claseGuid);
            var sameType = list.Where(d => d.DocumentType == DocumentType)
                .OrderByDescending(d => d.UpdatedAt).ToList();
            if (sameType.Count < 2)
            {
                ComparisonText = "No hay otra versión del mismo tipo para comparar.";
                return;
            }

            var current = await _api.GetEducationalDocumentAsync(sameType[0].Id);
            var previous = await _api.GetEducationalDocumentAsync(sameType[1].Id);
            ComparisonText =
                $"v anterior: {previous?.Title} ({previous?.Items.Count} ítems, {previous?.TotalPoints} pts)\n" +
                $"v actual: {current?.Title} ({current?.Items.Count} ítems, {current?.TotalPoints} pts)\n" +
                $"Estados: {previous?.Status} → {current?.Status}";
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

    private void Apply(EducationalDocumentDetailDto doc)
    {
        DocumentId = doc.Id;
        DocumentIdText = doc.Id.ToString();
        Title = doc.Title;
        Instructions = doc.Instructions;
        Status = doc.Status;
        StatusLabel = doc.IsOutdated
            ? MaterialUiLabels.Status(EducationalDocumentStatus.Outdated)
            : MaterialUiLabels.Status(doc.Status);
        DocumentType = doc.DocumentType;
        TypeLabel = MaterialUiLabels.Type(doc.DocumentType);
        TotalPoints = doc.TotalPoints;
        Duration = doc.EstimatedDurationMinutes;
        IsOutdated = doc.IsOutdated;
        RowVersion = doc.RowVersion;
        CurriculumRefs = $"OA {doc.ObjectiveCode} · {doc.CurriculumRelease} · Bloom {doc.BloomLevel}";
        Items.Clear();
        foreach (var i in doc.Items) Items.Add(i);
        Specs.Clear();
        foreach (var s in doc.SpecificationTable) Specs.Add(s);
    }
}
