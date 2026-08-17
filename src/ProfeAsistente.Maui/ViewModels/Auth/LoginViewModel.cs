using ProfeAsistente.Maui.Services;
using ProfeAsistente.Maui.Services.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Auth;

public partial class LoginViewModel : ObservableObject
{
    public const string DemoUserName = "admin";
    public const string DemoPassword = "Admin!Pass123";

    private readonly IAuthenticationService _auth;
    private readonly LocalApiLauncher _launcher;

    public LoginViewModel(IAuthenticationService auth, LocalApiLauncher launcher)
    {
        _auth = auth;
        _launcher = launcher;
    }

    [ObservableProperty] private string userName = DemoUserName;
    [ObservableProperty] private string password = DemoPassword;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado = "Usuario de prueba: admin / Admin!Pass123";

    [RelayCommand]
    private Task LoginAsync() => SignInAsync(UserName.Trim(), Password);

    [RelayCommand]
    private Task DemoLoginAsync()
    {
        UserName = DemoUserName;
        Password = DemoPassword;
        return SignInAsync(DemoUserName, DemoPassword);
    }

    private async Task SignInAsync(string user, string password)
    {
        try
        {
            IsBusy = true;
            MensajeEstado = "Comprobando API local…";
            try
            {
                await _launcher.EnsureRunningAsync();
            }
            catch (Exception ex)
            {
                MensajeEstado = $"No se pudo iniciar la API: {ex.Message}";
                return;
            }

            MensajeEstado = "Iniciando sesión…";
            var result = await _auth.LoginAsync(user, password);
            if (result is null)
            {
                MensajeEstado = "No se pudo iniciar sesión. ¿Está la API en http://127.0.0.1:5180?";
                return;
            }

            // En MVP local no forzar pantalla de cambio de contraseña.
            AppShell.ApplyMenuForCurrentUser(_auth);
            if (result.User.Memberships.Count > 1)
            {
                await Shell.Current.GoToAsync("institutionSelector");
                return;
            }

            await Shell.Current.GoToAsync("//inicio");
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
