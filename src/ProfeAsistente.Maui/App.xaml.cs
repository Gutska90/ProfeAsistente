using ProfeAsistente.Maui.Services;
using ProfeAsistente.Maui.Services.Auth;

namespace ProfeAsistente.Maui;

public partial class App : Application
{
    private readonly IAuthenticationService _auth;
    private readonly LocalApiLauncher _launcher;

    public App(IAuthenticationService auth, LocalApiLauncher launcher)
    {
        InitializeComponent();
        _auth = auth;
        _launcher = launcher;
        var shell = new AppShell();
        shell.WireSessionExpired(auth);
        shell.ApplyMenu(auth);
        MainPage = shell;
        _ = NavigateStartupAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Title = "ProfeAsistente";
        window.MinimumWidth = 960;
        window.MinimumHeight = 640;
        window.Width = 1120;
        window.Height = 780;
        return window;
    }

    private async Task NavigateStartupAsync()
    {
        try
        {
            try { await _launcher.EnsureRunningAsync(); }
            catch { /* El login mostrará el error si la API no arranca. */ }

            if (!await _auth.EnsureAuthenticatedAsync())
            {
                AppShell.ApplyMenuForCurrentUser(_auth);
                await Shell.Current.GoToAsync("//login");
                return;
            }

            var me = await _auth.GetMeAsync();
            if (me is null)
            {
                AppShell.ApplyMenuForCurrentUser(_auth);
                await Shell.Current.GoToAsync("//login");
                return;
            }

            AppShell.ApplyMenuForCurrentUser(_auth);
            await Shell.Current.GoToAsync("//inicio");
        }
        catch
        {
            await Shell.Current.GoToAsync("//login");
        }
    }
}
