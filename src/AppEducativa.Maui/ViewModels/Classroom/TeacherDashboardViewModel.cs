using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Classroom;

public partial class TeacherDashboardViewModel : ObservableObject
{
    private readonly IOfflineSyncService _sync;

    public TeacherDashboardViewModel(IOfflineSyncService sync) => _sync = sync;

    [ObservableProperty] private TeacherDashboardDto? dashboard;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string? syncStatus;
    [ObservableProperty] private bool isBusy;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            await _sync.FlushAsync();
            Dashboard = await _sync.GetDashboardAsync();
            SyncStatus = _sync.StatusText;
            MensajeEstado = Dashboard?.Reminders.FirstOrDefault();
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
            SyncStatus = _sync.StatusText;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        await _sync.FlushAsync();
        SyncStatus = _sync.StatusText;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task OpenClassAsync(UpcomingClassDto? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync($"claseDetalle?id={item.ClassId}");
    }

    [RelayCommand]
    private async Task OpenPlanningsAsync() => await Shell.Current.GoToAsync("//planificaciones");

    [RelayCommand]
    private async Task OpenRosterAsync() => await Shell.Current.GoToAsync("//nomina");
}
