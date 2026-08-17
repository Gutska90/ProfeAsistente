using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Documents;

[QueryProperty(nameof(ItemIdText), "itemId")]
public partial class EducationalItemEditorViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public EducationalItemEditorViewModel(IApiClient api) => _api = api;

    [ObservableProperty] private string itemIdText = string.Empty;
    [ObservableProperty] private string statement = string.Empty;
    [ObservableProperty] private string? expectedAnswer;
    [ObservableProperty] private string? explanation;
    [ObservableProperty] private decimal points = 1;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    partial void OnItemIdTextChanged(string value) => _ = CargarAsync();

    [RelayCommand]
    public Task CargarAsync()
    {
        MensajeEstado = "Edita el ítem y guarda.";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (!Guid.TryParse(ItemIdText, out var itemId)) return;
        try
        {
            IsBusy = true;
            await _api.UpdateEducationalItemAsync(itemId, new UpdateEducationalItemRequest
            {
                ItemType = EducationalItemType.ShortAnswer,
                Statement = Statement,
                ExpectedAnswer = ExpectedAnswer,
                Explanation = Explanation,
                Points = Points,
                Difficulty = ItemDifficulty.Intermediate,
                BloomLevel = "Comprender"
            });
            MensajeEstado = "Ítem guardado.";
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
