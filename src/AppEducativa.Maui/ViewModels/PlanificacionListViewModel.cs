using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels;

public partial class PlanificacionListViewModel : ObservableObject
{
    private readonly LocalApiLauncher _launcher;
    private readonly IOfflineSyncService _sync;

    public PlanificacionListViewModel(LocalApiLauncher launcher, IOfflineSyncService sync)
    {
        _launcher = launcher;
        _sync = sync;
    }

    public ObservableCollection<PlanificacionResumenDto> Planificaciones { get; } = [];

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    [RelayCommand]
    public async Task InicializarAsync()
    {
        try
        {
            IsBusy = true;
            try
            {
                await _launcher.EnsureRunningAsync();
            }
            catch
            {
                /* Sin API: se usa la copia local. */
            }
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
            var list = await _sync.GetPlanificacionesAsync();
            Planificaciones.Clear();
            foreach (var p in list) Planificaciones.Add(p);
            var pending = _sync.PendingCount > 0 ? $" · {_sync.PendingCount} pendiente(s)" : string.Empty;
            MensajeEstado = Planificaciones.Count == 0
                ? "Aún no hay planificaciones. Crea una nueva."
                : $"{Planificaciones.Count} planificación(es){pending}.";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al cargar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NuevaAsync() => await Shell.Current.GoToAsync("nuevaPlanificacion");

    [RelayCommand]
    private async Task AbrirAsync(PlanificacionResumenDto? plan)
    {
        if (plan is null) return;
        await Shell.Current.GoToAsync($"planificacionDetalle?id={plan.Id}");
    }
}
