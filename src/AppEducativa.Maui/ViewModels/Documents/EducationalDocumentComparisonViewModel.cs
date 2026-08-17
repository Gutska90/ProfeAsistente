using AppEducativa.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Documents;

[QueryProperty(nameof(ClaseId), "id")]
[QueryProperty(nameof(DocumentType), "type")]
public partial class EducationalDocumentComparisonViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public EducationalDocumentComparisonViewModel(IApiClient api) => _api = api;

    [ObservableProperty] private string claseId = string.Empty;
    [ObservableProperty] private string documentType = "Assessment";
    [ObservableProperty] private string comparisonText = string.Empty;
    [ObservableProperty] private string? mensajeEstado;

    partial void OnClaseIdChanged(string value) => _ = CargarAsync();

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        try
        {
            var list = (await _api.GetEducationalDocumentsAsync(id))
                .Where(d => string.Equals(d.DocumentType, DocumentType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(d => d.UpdatedAt)
                .Take(2)
                .ToList();
            if (list.Count < 2)
            {
                ComparisonText = "Se necesitan al menos dos versiones del mismo tipo.";
                return;
            }

            var a = await _api.GetEducationalDocumentAsync(list[0].Id);
            var b = await _api.GetEducationalDocumentAsync(list[1].Id);
            ComparisonText =
                $"Actual: {a?.Title} · {a?.Items.Count} ítems · {a?.TotalPoints} pts · {a?.Status}\n" +
                $"Anterior: {b?.Title} · {b?.Items.Count} ítems · {b?.TotalPoints} pts · {b?.Status}";
            MensajeEstado = "Comparación lista.";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error: {ex.Message}";
        }
    }
}
