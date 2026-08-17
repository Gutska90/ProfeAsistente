using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Admin;

[QueryProperty(nameof(BatchIdText), "id")]
public partial class CurriculumImportDetailViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public CurriculumImportDetailViewModel(IApiClient api) => _api = api;

    public ObservableCollection<ValidationIssueDto> Issues { get; } = [];

    [ObservableProperty] private string batchIdText = "";
    [ObservableProperty] private Guid batchId;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string estado = "";
    [ObservableProperty] private string resumen = "";
    [ObservableProperty] private string diffJson = "";

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
            var batches = await _api.GetCurriculumBatchesAsync();
            var batch = batches.FirstOrDefault(b => b.Id == BatchId);
            Estado = batch?.Estado ?? "?";
            Resumen = batch?.Mensaje ?? $"{batch?.CantidadUnidades} u · {batch?.CantidadOA} OA";
            Issues.Clear();
            foreach (var issue in await _api.GetCurriculumImportIssuesAsync(BatchId))
                Issues.Add(issue);
            try { DiffJson = await _api.GetCurriculumImportDiffAsync(BatchId); }
            catch { DiffJson = "(sin diff aún)"; }
            MensajeEstado = $"Estado: {Estado}";
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
    private Task DescargarAsync() => RunStepAsync(() => _api.DownloadCurriculumImportAsync(BatchId));

    [RelayCommand]
    private Task ExtraerAsync() => RunStepAsync(() => _api.ExtractCurriculumImportAsync(BatchId));

    [RelayCommand]
    private Task ValidarAsync() => RunStepAsync(() => _api.ValidateCurriculumImportAsync(BatchId));

    [RelayCommand]
    private Task ProcesarAsync() => RunStepAsync(() => _api.ProcessCurriculumImportAsync(BatchId));

    [RelayCommand]
    private async Task VerPreviewAsync()
    {
        if (BatchId == Guid.Empty) return;
        await Shell.Current.GoToAsync($"adminImportPreview?id={BatchId}");
    }

    [RelayCommand]
    private async Task IniciarRevisionAsync()
    {
        if (BatchId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            await _api.StartCurriculumReviewAsync(BatchId);
            MensajeEstado = "Revisión iniciada.";
            await Shell.Current.GoToAsync($"adminReviewDashboard?id={BatchId}");
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
            MensajeEstado = "Importación completada. Los OA oficiales aparecen en el planificador.";
            await CargarAsync();
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    private async Task RunStepAsync(Func<Task<ImportSummaryDto>> action)
    {
        try
        {
            IsBusy = true;
            var summary = await action();
            Estado = summary.Status;
            Resumen = $"{summary.Units} u · {summary.Objectives} OA · {summary.Warnings} adv · {summary.Errors} err";
            MensajeEstado = $"OK → {summary.Status}";
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
}
