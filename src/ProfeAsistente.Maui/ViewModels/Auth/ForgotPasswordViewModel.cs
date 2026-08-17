using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProfeAsistente.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Auth;

public partial class ForgotPasswordViewModel : ObservableObject
{
    private readonly HttpClient _http;

    public ForgotPasswordViewModel(HttpClient http) => _http = http;

    [ObservableProperty] private string userNameOrEmail = string.Empty;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string? developmentToken;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        using var response = await _http.PostAsJsonAsync("api/auth/forgot-password",
            new ForgotPasswordRequest { UserNameOrEmail = UserNameOrEmail },
            new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } });
        var body = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        MensajeEstado = body?.Message ?? "Si la cuenta existe, se generó una solicitud de recuperación.";
        DevelopmentToken = body?.DevelopmentResetToken;
    }
}
