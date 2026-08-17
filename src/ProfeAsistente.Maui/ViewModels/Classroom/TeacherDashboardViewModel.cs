using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Classroom;

public partial class TeacherDashboardViewModel : ObservableObject
{
    private readonly IOfflineSyncService _sync;

    public TeacherDashboardViewModel(IOfflineSyncService sync) => _sync = sync;

    [ObservableProperty] private TeacherDashboardDto? dashboard;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string? syncStatus;
    [ObservableProperty] private bool hasTodayClasses;
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
            HasTodayClasses = Dashboard.TodayClasses.Count > 0;
            MensajeEstado = Dashboard.InstitutionName;
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
        var result = await _sync.FlushAsync();
        SyncStatus = _sync.StatusText;
        if (result.StoppedOnError is not null)
            MensajeEstado = $"Sync parcial: {result.Sent} enviado(s). {result.StoppedOnError}";
        else if (result.Sent > 0)
            MensajeEstado = $"{result.Sent} cambio(s) sincronizado(s).";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task OpenClassAsync(UpcomingClassDto? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync($"claseDetalle?id={item.ClassId}");
    }

    [RelayCommand]
    private async Task PrepareClassAsync(UpcomingClassDto? item) => await OpenClassAsync(item);

    [RelayCommand]
    private async Task OpenCoursesAsync() => await Shell.Current.GoToAsync("//cursos");

    [RelayCommand]
    private async Task OpenPlanningsAsync() => await Shell.Current.GoToAsync("//planificaciones");
}
