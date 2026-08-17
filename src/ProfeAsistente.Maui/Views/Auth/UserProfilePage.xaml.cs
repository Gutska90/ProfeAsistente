using ProfeAsistente.Maui.Services;
using ProfeAsistente.Maui.Services.Auth;

namespace ProfeAsistente.Maui.Views.Auth;

public partial class UserProfilePage : ContentPage
{
    private readonly IAuthenticationService _auth;
    private readonly IOfflineSyncService _sync;

    public UserProfilePage(IAuthenticationService auth, IOfflineSyncService sync)
    {
        InitializeComponent();
        _auth = auth;
        _sync = sync;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var me = await _auth.GetMeAsync() ?? _auth.CurrentUser;
        DisplayName.Text = me?.DisplayName ?? "—";
        Email.Text = me?.Email ?? "—";
        Institution.Text = me?.ActiveInstitutionName ?? "Sin establecimiento";
        SyncStatus.Text = _sync.StatusText;
    }

    private async void OnSyncNow(object? sender, EventArgs e)
    {
        var result = await _sync.FlushAsync();
        SyncStatus.Text = result.StoppedOnError is null
            ? _sync.StatusText
            : $"{_sync.StatusText}\n{result.StoppedOnError}";
    }

    private async void OnChangePassword(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("changePassword");

    private async void OnLogout(object? sender, EventArgs e)
    {
        await _auth.LogoutAsync();
        await Shell.Current.GoToAsync("//login");
    }
}
