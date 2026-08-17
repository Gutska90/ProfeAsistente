using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Admin.CurriculumReview;

[QueryProperty(nameof(BatchIdText), "id")]
[QueryProperty(nameof(ObjectiveTemporaryId), "objectiveTemporaryId")]
public partial class CurriculumReviewObjectiveDetailViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private CurriculumReviewPackageDto? _package;
    private string? _rowVersion;
    private string _savedCode = "";
    private string _savedDescription = "";
    private string _savedDecision = "";

    public CurriculumReviewObjectiveDetailViewModel(IApiClient api) => _api = api;

    public ObservableCollection<ReviewIndicatorDto> Indicadores { get; } = [];
    public ObservableCollection<ReviewFieldIssueDto> Issues { get; } = [];

    [ObservableProperty] private string batchIdText = "";
    [ObservableProperty] private Guid batchId;
    [ObservableProperty] private string objectiveTemporaryId = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private bool isDirty;

    [ObservableProperty] private string extractedCode = "";
    [ObservableProperty] private string extractedDescription = "";
    [ObservableProperty] private string sourceFragment = "";
    [ObservableProperty] private string pageText = "";
    [ObservableProperty] private string confidenceText = "";

    [ObservableProperty] private string editCode = "";
    [ObservableProperty] private string editDescription = "";
    [ObservableProperty] private string decision = "";
    [ObservableProperty] private bool wasManuallyModified;

    partial void OnBatchIdTextChanged(string value)
    {
        if (Guid.TryParse(value, out var id))
            BatchId = id;
    }

    partial void OnObjectiveTemporaryIdChanged(string value)
    {
        if (BatchId != Guid.Empty && !string.IsNullOrWhiteSpace(value))
            _ = CargarAsync();
    }

    partial void OnEditCodeChanged(string value) => RefreshDirty();
    partial void OnEditDescriptionChanged(string value) => RefreshDirty();

    private void RefreshDirty() =>
        IsDirty = EditCode != _savedCode || EditDescription != _savedDescription || Decision != _savedDecision;

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (BatchId == Guid.Empty || string.IsNullOrWhiteSpace(ObjectiveTemporaryId)) return;
        try
        {
            IsBusy = true;
            _package = await _api.GetCurriculumReviewAsync(BatchId);
            if (_package is null)
            {
                MensajeEstado = "Sin paquete de revisión.";
                return;
            }

            _rowVersion = _package.RowVersion;
            BindCurrent();
            MensajeEstado = "OA cargado.";
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    private void BindCurrent()
    {
        var o = CurrentObjective();
        if (o is null)
        {
            MensajeEstado = "OA no encontrado.";
            return;
        }

        ExtractedCode = o.ExtractedCode;
        ExtractedDescription = o.ExtractedDescription;
        SourceFragment = o.SourceFragment ?? "";
        PageText = o.PageStart is null ? "—" : $"p. {o.PageStart}" + (o.PageEnd is null || o.PageEnd == o.PageStart ? "" : $"-{o.PageEnd}");
        ConfidenceText = o.ExtractionConfidence.ToString("0.00");
        EditCode = o.Code ?? o.ExtractedCode;
        EditDescription = o.Description;
        Decision = o.Decision;
        WasManuallyModified = o.WasManuallyModified;
        _savedCode = EditCode;
        _savedDescription = EditDescription;
        _savedDecision = Decision;
        IsDirty = false;

        Indicadores.Clear();
        foreach (var i in _package!.Indicators
                     .Where(x => x.ObjectiveTemporaryId == o.TemporaryId && !x.IsDeleted)
                     .OrderBy(x => x.Order))
            Indicadores.Add(i);

        Issues.Clear();
        foreach (var issue in o.Issues)
            Issues.Add(issue);
    }

    private ReviewObjectiveDto? CurrentObjective() =>
        _package?.Objectives.FirstOrDefault(o => o.TemporaryId == ObjectiveTemporaryId);

    [RelayCommand]
    private Task AceptarAsync() => SetDecisionAndSaveAsync(CurriculumRecordDecision.Accepted);

    [RelayCommand]
    private Task CorregirAsync() => SetDecisionAndSaveAsync(CurriculumRecordDecision.Corrected);

    [RelayCommand]
    private Task RechazarAsync() => SetDecisionAndSaveAsync(CurriculumRecordDecision.Rejected);

    private async Task SetDecisionAndSaveAsync(CurriculumRecordDecision decision)
    {
        Decision = decision.ToString();
        RefreshDirty();
        await GuardarAsync();
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (BatchId == Guid.Empty || string.IsNullOrWhiteSpace(ObjectiveTemporaryId)) return;
        try
        {
            IsBusy = true;
            Enum.TryParse<CurriculumRecordDecision>(Decision, true, out var decision);
            _package = await _api.UpdateReviewObjectiveAsync(BatchId, ObjectiveTemporaryId, new UpdateReviewObjectiveRequest
            {
                Code = EditCode,
                Description = EditDescription,
                Decision = decision,
                RowVersion = _rowVersion
            });
            _rowVersion = _package.RowVersion;
            BindCurrent();
            MensajeEstado = "Guardado.";
        }
        catch (Exception ex) { MensajeEstado = $"Error al guardar: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AgregarIndicadorAsync()
    {
        var text = await Shell.Current.DisplayPromptAsync(
            "Nuevo indicador", "Descripción del indicador:", "Agregar", "Cancelar");
        if (string.IsNullOrWhiteSpace(text)) return;
        try
        {
            IsBusy = true;
            _package = await _api.AddReviewIndicatorAsync(BatchId, ObjectiveTemporaryId, new AddReviewIndicatorRequest
            {
                Description = text.Trim(),
                RowVersion = _rowVersion
            });
            _rowVersion = _package.RowVersion;
            BindCurrent();
            MensajeEstado = "Indicador agregado.";
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SiguientePendienteAsync()
    {
        if (!await ConfirmDiscardIfDirtyAsync()) return;
        if (_package is null) await CargarAsync();
        if (_package is null) return;

        var pending = _package.Objectives
            .Where(o => !o.IsDeleted && string.Equals(o.Decision, "Pending", StringComparison.OrdinalIgnoreCase))
            .OrderBy(o => o.Code ?? o.ExtractedCode)
            .ToList();
        if (pending.Count == 0)
        {
            MensajeEstado = "No hay OA pendientes.";
            return;
        }

        var idx = pending.FindIndex(o => o.TemporaryId == ObjectiveTemporaryId);
        var next = pending[(idx + 1) % pending.Count];
        if (next.TemporaryId == ObjectiveTemporaryId && pending.Count == 1)
        {
            MensajeEstado = "Este es el único OA pendiente.";
            return;
        }

        ObjectiveTemporaryId = next.TemporaryId;
        BindCurrent();
        MensajeEstado = $"Siguiente pendiente: {next.Code ?? next.ExtractedCode}";
    }

    [RelayCommand]
    private async Task AnteriorPendienteAsync()
    {
        if (!await ConfirmDiscardIfDirtyAsync()) return;
        if (_package is null) await CargarAsync();
        if (_package is null) return;

        var pending = _package.Objectives
            .Where(o => !o.IsDeleted && string.Equals(o.Decision, "Pending", StringComparison.OrdinalIgnoreCase))
            .OrderBy(o => o.Code ?? o.ExtractedCode)
            .ToList();
        if (pending.Count == 0)
        {
            MensajeEstado = "No hay OA pendientes.";
            return;
        }

        var idx = pending.FindIndex(o => o.TemporaryId == ObjectiveTemporaryId);
        if (idx < 0) idx = 0;
        var prev = pending[(idx - 1 + pending.Count) % pending.Count];
        ObjectiveTemporaryId = prev.TemporaryId;
        BindCurrent();
        MensajeEstado = $"Anterior pendiente: {prev.Code ?? prev.ExtractedCode}";
    }

    public async Task<bool> ConfirmDiscardIfDirtyAsync()
    {
        if (!IsDirty) return true;
        var choice = await Shell.Current.DisplayAlert(
            "Cambios sin guardar",
            "Hay cambios sin guardar. ¿Desea descartarlos?",
            "Descartar",
            "Cancelar");
        return choice;
    }
}
