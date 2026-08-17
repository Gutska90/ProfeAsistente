using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Admin;

public partial class CurriculumSourcesViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly LocalApiLauncher _launcher;

    public CurriculumSourcesViewModel(IApiClient api, LocalApiLauncher launcher)
    {
        _api = api;
        _launcher = launcher;
    }

    public ObservableCollection<CurriculumAdminSourceDto> Fuentes { get; } = [];

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private CurriculumAdminSourceDto? fuenteSeleccionada;

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
            Fuentes.Clear();
            foreach (var s in await _api.GetCurriculumSourcesAsync())
                Fuentes.Add(s);
            MensajeEstado = $"{Fuentes.Count} fuente(s). Oficial vs demo se distingue por TipoFuente / EsContenidoOficial en el planificador.";
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
    private async Task RecargarFuentesAsync()
    {
        try
        {
            IsBusy = true;
            await _api.ReloadCurriculumSourcesAsync();
            await CargarAsync();
            MensajeEstado = "Fuentes recargadas desde curriculum-sources.json.";
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
    private async Task CrearLoteAsync(CurriculumAdminSourceDto? fuente)
    {
        fuente ??= FuenteSeleccionada;
        var key = fuente?.ExternalId ?? fuente?.Id.ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            MensajeEstado = "Seleccione una fuente con ExternalId (ej. matematica-4-basico-programa).";
            return;
        }

        try
        {
            IsBusy = true;
            var summary = await _api.CreateCurriculumImportAsync(key);
            MensajeEstado = $"Lote creado: {summary.BatchId:N} · estado {summary.Status}";
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
