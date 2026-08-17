using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Admin.CurriculumReview;

[QueryProperty(nameof(BatchIdText), "id")]
public partial class CurriculumReviewChangesViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public CurriculumReviewChangesViewModel(IApiClient api) => _api = api;

    public ObservableCollection<ReviewChangeDto> Changes { get; } = [];

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
            Changes.Clear();
            foreach (var c in await _api.GetCurriculumReviewChangesAsync(BatchId))
                Changes.Add(c);
            MensajeEstado = $"{Changes.Count} cambios";
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}
