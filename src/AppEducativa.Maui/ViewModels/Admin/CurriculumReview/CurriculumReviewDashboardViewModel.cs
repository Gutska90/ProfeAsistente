using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Admin.CurriculumReview;

[QueryProperty(nameof(BatchIdText), "id")]
public partial class CurriculumReviewDashboardViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public CurriculumReviewDashboardViewModel(IApiClient api) => _api = api;

    [ObservableProperty] private string batchIdText = "";
    [ObservableProperty] private Guid batchId;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    [ObservableProperty] private string documentTitle = "";
    [ObservableProperty] private string levelName = "";
    [ObservableProperty] private string subjectName = "";
    [ObservableProperty] private string importStatus = "";
    [ObservableProperty] private string reviewStatus = "";
    [ObservableProperty] private string confidenceText = "";
    [ObservableProperty] private string countsText = "";
    [ObservableProperty] private string issuesText = "";
    [ObservableProperty] private string metaText = "";

    [ObservableProperty] private bool canRevalidate = true;
    [ObservableProperty] private bool canMarkReady;
    [ObservableProperty] private bool canApprove;
    [ObservableProperty] private bool canReject = true;
    [ObservableProperty] private bool canImport;
    [ObservableProperty] private bool canPublish;
    [ObservableProperty] private bool canNavigateReview = true;

    private CurriculumReviewSummaryDto? _summary;

    partial void OnBatchIdTextChanged(string value)
    {
        if (Guid.TryParse(value, out var id))
            BatchId = id;
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (BatchId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            _summary = await _api.GetCurriculumReviewSummaryAsync(BatchId);
            if (_summary is null)
            {
                MensajeEstado = "No hay sesión de revisión. Inicie la revisión desde el detalle del lote.";
                CanNavigateReview = false;
                CanMarkReady = CanApprove = CanImport = CanPublish = false;
                return;
            }

            ApplySummary(_summary);
            MensajeEstado = $"Revisión: {ReviewStatus} · Lote: {ImportStatus}";
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

    private void ApplySummary(CurriculumReviewSummaryDto s)
    {
        DocumentTitle = s.DocumentTitle ?? "(sin título)";
        LevelName = s.LevelName ?? "—";
        SubjectName = s.SubjectName ?? "—";
        ImportStatus = s.ImportStatus;
        ReviewStatus = s.Status;
        ConfidenceText = $"Confianza extracción: {s.ExtractionConfidence:0.00}";
        CountsText =
            $"Unidades {s.Units.Total} (pend {s.Units.Pending}) · OA {s.Objectives.Total} (pend {s.Objectives.Pending}, corr {s.Objectives.Corrected}) · " +
            $"Indicadores {s.Indicators.Total} · Hab {s.Skills} · Act {s.Attitudes}";
        IssuesText =
            $"Bloqueantes {s.Issues.Blocking} · Errores {s.Issues.Errors} · Advertencias {s.Issues.Warnings} · Info {s.Issues.Info} · " +
            $"Cambios {s.Changes} · Comentarios abiertos {s.UnresolvedComments}";
        MetaText =
            $"Última validación: {Fmt(s.LastValidationAt)} · Último diff: {Fmt(s.LastDiffAt)}";

        var import = s.ImportStatus;
        var review = s.Status;
        CanNavigateReview = true;
        CanRevalidate = IsOneOf(import, "PendingReview", "Validated", "ReadyForApproval", "Failed")
                        || IsOneOf(review, "InProgress", "CorrectionsRequired", "ReadyForApproval");
        CanMarkReady = s.CanMarkReady;
        CanApprove = IsOneOf(import, "ReadyForApproval") || IsOneOf(review, "ReadyForApproval");
        CanReject = !IsOneOf(import, "Approved", "Rejected", "Imported");
        CanImport = IsOneOf(import, "Approved");
        CanPublish = IsOneOf(import, "Imported");
    }

    private static string Fmt(DateTime? dt) => dt?.ToLocalTime().ToString("g") ?? "—";

    private static bool IsOneOf(string? value, params string[] options) =>
        options.Any(o => string.Equals(value, o, StringComparison.OrdinalIgnoreCase));

    [RelayCommand]
    private Task IrUnidadesAsync() => GoAsync("adminReviewUnits");

    [RelayCommand]
    private Task IrObjetivosAsync() => GoAsync("adminReviewObjectives");

    [RelayCommand]
    private Task IrProblemasAsync() => GoAsync("adminReviewIssues");

    [RelayCommand]
    private Task IrDiffAsync() => GoAsync("adminReviewDiff");

    [RelayCommand]
    private Task IrCambiosAsync() => GoAsync("adminReviewChanges");

    [RelayCommand]
    private Task IrComentariosAsync() => GoAsync("adminReviewComments");

    private async Task GoAsync(string route)
    {
        if (BatchId == Guid.Empty) return;
        await Shell.Current.GoToAsync($"{route}?id={BatchId}");
    }

    [RelayCommand]
    private async Task RevalidarAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _api.RevalidateCurriculumReviewAsync(BatchId);
            MensajeEstado = result.IsValid
                ? $"Validación OK · Listo para aprobar: {(result.CanMarkReady ? "sí" : "no")}"
                : $"Validación con problemas ({result.Issues.Count}).";
            await CargarAsync();
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task MarcarListoAsync()
    {
        try
        {
            IsBusy = true;
            await _api.MarkCurriculumReviewReadyAsync(BatchId);
            MensajeEstado = "Marcado listo para aprobar.";
            await CargarAsync();
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AprobarAsync()
    {
        try
        {
            IsBusy = true;
            await _api.ApproveCurriculumImportAsync(BatchId);
            MensajeEstado = "Lote aprobado.";
            await CargarAsync();
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RechazarAsync()
    {
        var reason = await Shell.Current.DisplayPromptAsync(
            "Rechazar lote", "Indique el motivo del rechazo:", "Rechazar", "Cancelar", "Motivo");
        if (string.IsNullOrWhiteSpace(reason)) return;
        try
        {
            IsBusy = true;
            await _api.RejectCurriculumImportAsync(BatchId, reason.Trim());
            MensajeEstado = "Lote rechazado.";
            await CargarAsync();
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ImportarAsync()
    {
        try
        {
            IsBusy = true;
            await _api.ImportCurriculumBatchAsync(BatchId);
            MensajeEstado = "Importación completada.";
            await CargarAsync();
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task PublicarAsync()
    {
        try
        {
            IsBusy = true;
            await _api.PublishCurriculumImportAsync(BatchId);
            MensajeEstado = "Currículum publicado.";
            await CargarAsync();
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}
