using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels;

/// <summary>Hub legado: redirige al flujo oficial de importación PDF.</summary>
public partial class AdministrarCurriculumViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly LocalApiLauncher _launcher;

    public AdministrarCurriculumViewModel(IApiClient api, LocalApiLauncher launcher)
    {
        _api = api;
        _launcher = launcher;
    }

    public ObservableCollection<CurriculumAdminSourceDto> Fuentes { get; } = [];
    public ObservableCollection<CurriculumAdminBatchDto> Lotes { get; } = [];

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string versionInfo = "";

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
            Lotes.Clear();
            foreach (var s in await _api.GetCurriculumSourcesAsync())
                Fuentes.Add(s);
            foreach (var b in await _api.GetCurriculumBatchesAsync())
                Lotes.Add(b);
            var ver = await _api.GetCurriculumVersionAsync();
            VersionInfo = ver is null
                ? "Sin versión"
                : $"Vigente: {ver.Version} · {ver.ObjetivosVigentes} OA aprobados";
            MensajeEstado = $"{Fuentes.Count} fuentes · {Lotes.Count} lotes. Use Fuentes / Lotes en el menú para el flujo oficial.";
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
    private async Task IrAFuentesAsync() => await Shell.Current.GoToAsync("//adminSources");

    [RelayCommand]
    private async Task IrALotesAsync() => await Shell.Current.GoToAsync("//adminImports");

    [RelayCommand]
    private async Task CrearLoteMat4Async()
    {
        try
        {
            IsBusy = true;
            await _api.ReloadCurriculumSourcesAsync();
            var summary = await _api.CreateCurriculumImportAsync("matematica-4-basico-programa");
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
