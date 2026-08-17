using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels;

[QueryProperty(nameof(CourseIdFilter), "courseId")]
public partial class PlanificacionListViewModel : ObservableObject
{
    private readonly LocalApiLauncher _launcher;
    private readonly IOfflineSyncService _sync;
    private Guid? _courseFilter;
    private List<PlanificacionResumenDto> _all = [];

    public PlanificacionListViewModel(LocalApiLauncher launcher, IOfflineSyncService sync)
    {
        _launcher = launcher;
        _sync = sync;
    }

    public string CourseIdFilter
    {
        get => _courseFilter?.ToString() ?? string.Empty;
        set
        {
            _courseFilter = Guid.TryParse(value, out var id) ? id : null;
            ApplyFilter();
        }
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
            _all = (await _sync.GetPlanificacionesAsync()).ToList();
            ApplyFilter();
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

    private void ApplyFilter()
    {
        Planificaciones.Clear();
        var list = _courseFilter is Guid cid
            ? _all.Where(p => p.SchoolCourseId == cid)
            : _all;
        foreach (var p in list) Planificaciones.Add(p);
        var pending = _sync.PendingCount > 0 ? $" · {_sync.PendingCount} pendiente(s)" : string.Empty;
        var scope = _courseFilter is null ? string.Empty : " de este curso";
        MensajeEstado = Planificaciones.Count == 0
            ? $"Aún no hay planificaciones{scope}. Crea una nueva."
            : $"{Planificaciones.Count} planificación(es){scope}{pending}.";
    }

    [RelayCommand]
    private async Task NuevaAsync()
    {
        var q = _courseFilter is Guid cid ? $"?courseId={cid}" : string.Empty;
        await Shell.Current.GoToAsync($"nuevaPlanificacion{q}");
    }

    [RelayCommand]
    private async Task AbrirAsync(PlanificacionResumenDto? plan)
    {
        if (plan is null) return;
        await Shell.Current.GoToAsync($"planificacionDetalle?id={plan.Id}");
    }
}
