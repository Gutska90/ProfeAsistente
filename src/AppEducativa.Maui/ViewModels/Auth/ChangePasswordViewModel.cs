using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppEducativa.Maui.Services.Auth;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Auth;

public partial class ChangePasswordViewModel : ObservableObject
{
    private readonly HttpClient _http;
    private readonly IAuthenticationService _auth;

    public ChangePasswordViewModel(HttpClient http, IAuthenticationService auth)
    {
        _http = http;
        _auth = auth;
    }

    [ObservableProperty] private string currentPassword = string.Empty;
    [ObservableProperty] private string newPassword = string.Empty;
    [ObservableProperty] private string? mensajeEstado;

    [RelayCommand]
    private async Task ChangeAsync()
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/change-password");
            req.Content = JsonContent.Create(new ChangePasswordRequest
            {
                CurrentPassword = CurrentPassword,
                NewPassword = NewPassword
            }, options: new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } });
            if (!string.IsNullOrWhiteSpace(_auth.AccessToken))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
            using var response = await _http.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                MensajeEstado = await response.Content.ReadAsStringAsync();
                return;
            }

            MensajeEstado = "Contraseña actualizada.";
            await Shell.Current.GoToAsync("//inicio");
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
    }
}
