using System.Collections.ObjectModel;
using System.Text;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using ProfeAsistente.Shared.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Documents;

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
    [ObservableProperty] private string? qualitySummary;
    [ObservableProperty] private bool showFeedbackReasons;
    [ObservableProperty] private string? feedbackReason;
    [ObservableProperty] private bool isTemplate;
    [ObservableProperty] private bool hasLineage;
    [ObservableProperty] private string? lineageLabel;
    [ObservableProperty] private bool hasMyFeedback;
    [ObservableProperty] private string? myFeedbackLabel;

    public IReadOnlyList<string> FeedbackReasonLabels { get; } =
    [
        MaterialFeedbackReasons.Label(MaterialFeedbackReasons.NotAligned),
        MaterialFeedbackReasons.Label(MaterialFeedbackReasons.TooHard),
        MaterialFeedbackReasons.Label(MaterialFeedbackReasons.TooEasy),
        MaterialFeedbackReasons.Label(MaterialFeedbackReasons.Duplicated),
        MaterialFeedbackReasons.Label(MaterialFeedbackReasons.Unclear),
        MaterialFeedbackReasons.Label(MaterialFeedbackReasons.ContentError),
        MaterialFeedbackReasons.Label(MaterialFeedbackReasons.Other)
    ];

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

    [RelayCommand]
    private async Task FeedbackUtilAsync()
    {
        if (DocumentId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            ShowFeedbackReasons = false;
            await _api.SubmitMaterialFeedbackAsync(DocumentId, new SubmitMaterialFeedbackRequest { Useful = true });
            HasMyFeedback = true;
            MyFeedbackLabel = "Gracias: marcó que sí le sirvió.";
            MensajeEstado = MyFeedbackLabel;
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
    private void FeedbackNoUtil()
    {
        ShowFeedbackReasons = true;
        MensajeEstado = "¿Qué falló? Elija un motivo.";
    }

    [RelayCommand]
    private async Task EnviarFeedbackNegativoAsync()
    {
        if (DocumentId == Guid.Empty) return;
        var reason = FeedbackReason switch
        {
            var l when l == MaterialFeedbackReasons.Label(MaterialFeedbackReasons.NotAligned) => MaterialFeedbackReasons.NotAligned,
            var l when l == MaterialFeedbackReasons.Label(MaterialFeedbackReasons.TooHard) => MaterialFeedbackReasons.TooHard,
            var l when l == MaterialFeedbackReasons.Label(MaterialFeedbackReasons.TooEasy) => MaterialFeedbackReasons.TooEasy,
            var l when l == MaterialFeedbackReasons.Label(MaterialFeedbackReasons.Duplicated) => MaterialFeedbackReasons.Duplicated,
            var l when l == MaterialFeedbackReasons.Label(MaterialFeedbackReasons.Unclear) => MaterialFeedbackReasons.Unclear,
            var l when l == MaterialFeedbackReasons.Label(MaterialFeedbackReasons.ContentError) => MaterialFeedbackReasons.ContentError,
            _ => MaterialFeedbackReasons.Other
        };
        try
        {
            IsBusy = true;
            await _api.SubmitMaterialFeedbackAsync(DocumentId, new SubmitMaterialFeedbackRequest
            {
                Useful = false,
                Reason = reason
            });
            ShowFeedbackReasons = false;
            HasMyFeedback = true;
            MyFeedbackLabel = $"Gracias: registramos «{MaterialFeedbackReasons.Label(reason)}».";
            MensajeEstado = MyFeedbackLabel;
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
    private async Task UsarEnOtraClaseAsync()
    {
        if (DocumentId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            var targets = await _api.GetReuseTargetsAsync(DocumentId);
            if (targets.Count == 0)
            {
                MensajeEstado = "No hay otras clases del mismo curso/unidad para reutilizar.";
                return;
            }

            var labels = targets.Select(t => t.Label).ToArray();
            var choice = await Shell.Current.DisplayActionSheet(
                "Usar en otra clase", "Cancelar", null, labels);
            if (string.IsNullOrWhiteSpace(choice) || choice == "Cancelar") return;

            var target = targets.FirstOrDefault(t => t.Label == choice);
            if (target is null) return;

            var result = await _api.ReuseEducationalDocumentAsync(DocumentId, new ReuseEducationalDocumentRequest
            {
                TargetClassId = target.ClassId,
                SetAsCurrent = true
            });

            var warn = result.ObjectiveChanged
                ? $" OA cambió ({result.SourceObjectiveCode} → {result.TargetObjectiveCode}). Revise el material."
                : string.Empty;
            MensajeEstado = $"Material copiado a clase {target.ClassNumber}.{warn}";

            var go = await Shell.Current.DisplayAlert(
                "Material reutilizado",
                "¿Abrir la copia en la clase destino?",
                "Abrir", "Quedarme");
            if (go)
            {
                DocumentId = result.DocumentId;
                DocumentIdText = result.DocumentId.ToString();
                ClaseId = result.ClassId.ToString();
                await CargarAsync();
            }
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
    private async Task GuardarComoPlantillaAsync()
    {
        if (DocumentId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            var doc = await _api.SaveEducationalDocumentAsTemplateAsync(DocumentId);
            Apply(doc);
            MensajeEstado = "Plantilla guardada. Aparece en Biblioteca → Plantillas.";
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
    private async Task DuplicarAsync()
    {
        if (DocumentId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            var doc = await _api.DuplicateEducationalDocumentAsync(DocumentId, new DuplicateEducationalDocumentRequest());
            DocumentId = doc.Id;
            DocumentIdText = doc.Id.ToString();
            Apply(doc);
            MensajeEstado = "Copia creada en esta clase (borrador).";
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
    private async Task MarcarVersionActualAsync()
    {
        if (DocumentId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            var doc = await _api.SetCurrentEducationalDocumentAsync(DocumentId);
            Apply(doc);
            MensajeEstado = "Esta versión quedó como actual para la clase.";
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
        IsTemplate = doc.IsTemplate;
        HasLineage = doc.SourceDocumentId is not null;
        LineageLabel = HasLineage
            ? "Copia / reutilizado desde otro material"
            : null;
        RowVersion = doc.RowVersion;
        CurriculumRefs = $"OA {doc.ObjectiveCode} · {doc.CurriculumRelease} · Bloom {doc.BloomLevel}";
        QualitySummary = doc.QualityReport?.SummaryLine;
        HasMyFeedback = doc.MyFeedback is not null;
        MyFeedbackLabel = doc.MyFeedback is null
            ? null
            : doc.MyFeedback.Useful
                ? "Ya indicó que le sirvió."
                : $"Ya indicó: {MaterialFeedbackReasons.Label(doc.MyFeedback.Reason)}";
        Items.Clear();
        foreach (var i in doc.Items) Items.Add(i);
        Specs.Clear();
        foreach (var s in doc.SpecificationTable) Specs.Add(s);
    }
}
