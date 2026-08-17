using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Admin.Institutions;

public partial class InstitutionListViewModel : ObservableObject
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;

    public InstitutionListViewModel(HttpClient http) => _http = http;

    public ObservableCollection<InstitutionDto> Items { get; } = [];
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string newName = string.Empty;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<InstitutionDto>>("api/institutions", Json) ?? [];
            Items.Clear();
            foreach (var i in list) Items.Add(i);
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName)) return;
        using var response = await _http.PostAsJsonAsync("api/institutions", new CreateInstitutionRequest
        {
            Name = NewName.Trim()
        }, new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } });
        MensajeEstado = response.IsSuccessStatusCode ? "Creado." : await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            NewName = string.Empty;
            await LoadAsync();
        }
    }
}
