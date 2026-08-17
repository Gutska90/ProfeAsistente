using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Admin;

public partial class CurriculumImportsViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly LocalApiLauncher _launcher;

    public CurriculumImportsViewModel(IApiClient api, LocalApiLauncher launcher)
    {
        _api = api;
        _launcher = launcher;
    }

    public ObservableCollection<CurriculumAdminBatchDto> Lotes { get; } = [];

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    [RelayCommand]
    public async Task InicializarAsync()
    {
        try
        {
            IsBusy = true;
            await _launcher.EnsureRunningAsync();
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
    private async Task CargarAsync()
    {
        try
        {
            IsBusy = true;
            Lotes.Clear();
            foreach (var b in await _api.GetCurriculumBatchesAsync())
                Lotes.Add(b);
            MensajeEstado = $"{Lotes.Count} lote(s). Colores/etiquetas: Created/PendingReview = pendiente · Approved = aprobado · Imported = importado · Failed = error.";
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
    private async Task AbrirDetalleAsync(CurriculumAdminBatchDto? batch)
    {
        if (batch is null) return;
        await Shell.Current.GoToAsync($"adminImportDetail?id={batch.Id}");
    }

    [RelayCommand]
    private async Task CrearLoteMat4Async()
    {
        try
        {
            IsBusy = true;
            await _api.ReloadCurriculumSourcesAsync();
            var summary = await _api.CreateCurriculumImportAsync("matematica-4-basico-programa");
            MensajeEstado = $"Lote {summary.BatchId:N} creado.";
            await Shell.Current.GoToAsync($"adminImportDetail?id={summary.BatchId}");
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
