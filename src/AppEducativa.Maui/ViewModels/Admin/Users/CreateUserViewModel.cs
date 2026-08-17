using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Admin.Users;

public partial class CreateUserViewModel : ObservableObject
{
    private readonly HttpClient _http;

    public CreateUserViewModel(HttpClient http) => _http = http;

    [ObservableProperty] private string userName = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string firstName = string.Empty;
    [ObservableProperty] private string lastName = string.Empty;
    [ObservableProperty] private string? mensajeEstado;

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            using var response = await _http.PostAsJsonAsync("api/admin/users", new CreateUserRequest
            {
                UserName = UserName.Trim(),
                Email = Email.Trim(),
                Password = Password,
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                Roles = ["Teacher"]
            }, new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } });
            MensajeEstado = response.IsSuccessStatusCode
                ? "Usuario creado."
                : await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
    }
}
