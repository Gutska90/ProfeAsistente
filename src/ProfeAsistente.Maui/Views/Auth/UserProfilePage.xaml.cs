using ProfeAsistente.Maui.Services;
using ProfeAsistente.Maui.Services.Auth;
using ProfeAsistente.Shared.Dtos;

namespace ProfeAsistente.Maui.Views.Auth;

public partial class UserProfilePage : ContentPage
{
    private readonly IAuthenticationService _auth;
    private readonly IOfflineSyncService _sync;
    private readonly IApiClient _api;
    private bool? _usedInClass = true;

    public UserProfilePage(IAuthenticationService auth, IOfflineSyncService sync, IApiClient api)
    {
        InitializeComponent();
        _auth = auth;
        _sync = sync;
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var me = await _auth.GetMeAsync() ?? _auth.CurrentUser;
        DisplayName.Text = me?.DisplayName ?? "—";
        Email.Text = me?.Email ?? "—";
        Institution.Text = me?.ActiveInstitutionName ?? "Sin establecimiento";
        SyncStatus.Text = _sync.StatusText;
        await RefreshPilotAsync();
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

    private async void OnRefreshPilot(object? sender, EventArgs e) => await RefreshPilotAsync();

    private void OnUsedInClassYes(object? sender, EventArgs e)
    {
        _usedInClass = true;
        PilotMessage.Text = "Marcó: sí usó el material en clase.";
    }

    private void OnUsedInClassNo(object? sender, EventArgs e)
    {
        _usedInClass = false;
        PilotMessage.Text = "Marcó: aún no usó el material en clase.";
    }

    private async void OnSubmitPilotReport(object? sender, EventArgs e)
    {
        if (!int.TryParse(MinutesSavedEntry.Text?.Trim(), out var minutes) || minutes is < 0 or > 480)
        {
            PilotMessage.Text = "Indique minutos entre 0 y 480.";
            return;
        }

        try
        {
            var bucket = WithoutAppPicker.SelectedIndex switch
            {
                0 => WithoutAppDurationBuckets.Under15,
                1 => WithoutAppDurationBuckets.From15To30,
                2 => WithoutAppDurationBuckets.From30To60,
                3 => WithoutAppDurationBuckets.From1To2Hours,
                4 => WithoutAppDurationBuckets.Over2Hours,
                _ => null
            };
            await _api.SubmitPilotSessionReportAsync(new SubmitPilotSessionReportRequest
            {
                MinutesSavedEstimate = minutes,
                MaterialsUsedInClass = _usedInClass,
                WouldUseAgain = true,
                WithoutAppDurationBucket = bucket
            });
            PilotMessage.Text = "Gracias: registramos su ahorro de tiempo.";
            await RefreshPilotAsync();
        }
        catch (Exception ex)
        {
            PilotMessage.Text = $"Error: {ex.Message}";
        }
    }

    private async Task RefreshPilotAsync()
    {
        try
        {
            var m = await _api.GetPilotMetricsAsync();
            PilotSummary.Text = string.IsNullOrWhiteSpace(m.SummaryLine)
                ? "Sin actividad aún en el periodo."
                : m.SummaryLine;
        }
        catch (Exception ex)
        {
            PilotSummary.Text = $"No se pudieron cargar métricas: {ex.Message}";
        }
    }
}
