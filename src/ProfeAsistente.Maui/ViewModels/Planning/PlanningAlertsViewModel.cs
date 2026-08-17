using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Planning;

[QueryProperty(nameof(PlanningId), "planningId")]
public partial class PlanningAlertsViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public PlanningAlertsViewModel(IApiClient api) => _api = api;

    public ObservableCollection<PlanningAlertDto> Alerts { get; } = [];

    [ObservableProperty] private string planningId = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    partial void OnPlanningIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = CargarAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (!Guid.TryParse(PlanningId, out var id)) return;
        try
        {
            IsBusy = true;
            Alerts.Clear();
            foreach (var a in await _api.GetPlanningAlertsAsync(id))
                Alerts.Add(a);
            MensajeEstado = Alerts.Count == 0 ? "Sin alertas activas." : $"{Alerts.Count} alerta(s).";
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
