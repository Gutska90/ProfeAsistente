using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Admin.CurriculumReview;

[QueryProperty(nameof(BatchIdText), "id")]
public partial class CurriculumReviewUnitsViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public CurriculumReviewUnitsViewModel(IApiClient api) => _api = api;

    public ObservableCollection<ReviewUnitDto> Unidades { get; } = [];

    [ObservableProperty] private string batchIdText = "";
    [ObservableProperty] private Guid batchId;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    partial void OnBatchIdTextChanged(string value)
    {
        if (Guid.TryParse(value, out var id))
            BatchId = id;
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (BatchId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            var package = await _api.GetCurriculumReviewAsync(BatchId);
            Unidades.Clear();
            if (package is null)
            {
                MensajeEstado = "Sin paquete de revisión.";
                return;
            }

            foreach (var u in package.Units.Where(x => !x.IsDeleted).OrderBy(x => x.Number))
                Unidades.Add(u);
            MensajeEstado = $"{Unidades.Count} unidades";
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task IrObjetivosAsync()
    {
        if (BatchId == Guid.Empty) return;
        await Shell.Current.GoToAsync($"adminReviewObjectives?id={BatchId}");
    }
}
