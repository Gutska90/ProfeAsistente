using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProfeAsistente.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Admin.Users;

public partial class UserListViewModel : ObservableObject
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;

    public UserListViewModel(HttpClient http) => _http = http;

    public ObservableCollection<UserSummaryDto> Users { get; } = [];
    [ObservableProperty] private string? mensajeEstado;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<UserSummaryDto>>("api/admin/users", Json) ?? [];
            Users.Clear();
            foreach (var u in list) Users.Add(u);
            MensajeEstado = $"{Users.Count} usuarios";
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CreateAsync() => await Shell.Current.GoToAsync("createUser");
}
