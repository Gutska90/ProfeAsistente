using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Exports;

public partial class ExportHistoryViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly IFileSaveService _files;

    public ExportHistoryViewModel(IApiClient api, IFileSaveService files)
    {
        _api = api;
        _files = files;
    }

    public ObservableCollection<ExportSummaryDto> Items { get; } = [];

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    [RelayCommand]
    public async Task CargarAsync()
    {
        try
        {
            IsBusy = true;
            Items.Clear();
            foreach (var item in await _api.GetExportsAsync())
                Items.Add(item);
            MensajeEstado = $"{Items.Count} exportación(es).";
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
    private async Task DescargarAsync(ExportSummaryDto? item)
    {
        if (item is null) return;
        try
        {
            IsBusy = true;
            var (bytes, fileName) = await _api.DownloadExportAsync(item.Id);
            var path = await _files.SaveAsync(bytes, fileName, "application/octet-stream");
            MensajeEstado = path is null ? "No se pudo guardar." : $"Guardado: {path}";
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message.Contains("expir", StringComparison.OrdinalIgnoreCase)
                ? "La exportación expiró y debe generarse nuevamente."
                : $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EliminarAsync(ExportSummaryDto? item)
    {
        if (item is null) return;
        try
        {
            await _api.DeleteExportAsync(item.Id);
            Items.Remove(item);
            MensajeEstado = "Exportación eliminada.";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error: {ex.Message}";
        }
    }
}
