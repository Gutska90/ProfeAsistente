using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Documents;

[QueryProperty(nameof(DocumentIdText), "documentId")]
public partial class AssessmentSpecificationViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public AssessmentSpecificationViewModel(IApiClient api) => _api = api;

    public ObservableCollection<AssessmentSpecificationRowDto> Rows { get; } = [];

    [ObservableProperty] private string documentIdText = string.Empty;
    [ObservableProperty] private string? mensajeEstado;

    partial void OnDocumentIdTextChanged(string value) => _ = CargarAsync();

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (!Guid.TryParse(DocumentIdText, out var id)) return;
        try
        {
            Rows.Clear();
            var doc = await _api.GetEducationalDocumentAsync(id);
            if (doc is null)
            {
                MensajeEstado = "Documento no encontrado.";
                return;
            }

            foreach (var row in doc.SpecificationTable)
                Rows.Add(row);
            MensajeEstado = $"{Rows.Count} fila(s) de especificación.";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error: {ex.Message}";
        }
    }
}
