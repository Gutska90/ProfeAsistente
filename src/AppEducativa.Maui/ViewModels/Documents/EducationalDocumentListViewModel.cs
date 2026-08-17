using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Documents;

[QueryProperty(nameof(ClaseId), "id")]
public partial class EducationalDocumentListViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public EducationalDocumentListViewModel(IApiClient api) => _api = api;

    public ObservableCollection<EducationalDocumentSummaryDto> Documentos { get; } = [];

    [ObservableProperty] private string claseId = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    partial void OnClaseIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = CargarAsync();
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        try
        {
            IsBusy = true;
            Documentos.Clear();
            foreach (var d in await _api.GetEducationalDocumentsAsync(id))
                Documentos.Add(d);
            MensajeEstado = Documentos.Count == 0
                ? "Sin materiales aún. Use Crear guía / actividad / prueba."
                : $"{Documentos.Count} material(es) en esta clase.";
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
    private async Task GenerarAsync(string? tipo)
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        await Shell.Current.GoToAsync($"educationalDocumentGeneration?id={id}&type={tipo ?? "Assessment"}");
    }

    [RelayCommand]
    private async Task AbrirAsync(EducationalDocumentSummaryDto? doc)
    {
        if (doc is null) return;
        await Shell.Current.GoToAsync($"educationalDocumentEditor?documentId={doc.Id}&id={ClaseId}");
    }
}
