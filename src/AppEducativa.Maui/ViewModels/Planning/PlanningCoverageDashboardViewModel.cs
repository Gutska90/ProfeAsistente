using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Planning;

[QueryProperty(nameof(PlanningId), "planningId")]
public partial class PlanningCoverageDashboardViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public PlanningCoverageDashboardViewModel(IApiClient api) => _api = api;

    public ObservableCollection<ObjectiveCoverageDto> Objectives { get; } = [];
    public ObservableCollection<BloomDistributionDto> Bloom { get; } = [];
    public ObservableCollection<string> MatrixLines { get; } = [];

    [ObservableProperty] private string planningId = string.Empty;
    [ObservableProperty] private string mode = "Planned";
    [ObservableProperty] private PlanningCoverageDto? coverage;
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
            Coverage = await _api.GetPlanningCoverageAsync(id, Mode);
            Objectives.Clear();
            Bloom.Clear();
            MatrixLines.Clear();
            if (Coverage is null)
            {
                MensajeEstado = "Sin datos de cobertura.";
                return;
            }

            foreach (var o in Coverage.Objectives)
                Objectives.Add(o);
            foreach (var b in Coverage.BloomDistribution)
                Bloom.Add(b);

            if (Coverage.Matrix is not null)
            {
                MatrixLines.Add("Clases: " + string.Join(" | ", Coverage.Matrix.ClassLabels));
                foreach (var row in Coverage.Matrix.Rows)
                    MatrixLines.Add($"{row.Label}: {string.Join(" ", row.Cells.Select(c => string.IsNullOrEmpty(c) ? "." : c))}");
            }

            MensajeEstado = Mode == "Executed" ? "Cobertura ejecutada" : "Cobertura planificada";
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

    [RelayCommand]
    private async Task ToggleModeAsync()
    {
        Mode = Mode == "Planned" ? "Executed" : "Planned";
        await CargarAsync();
    }
}
