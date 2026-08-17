using ProfeAsistente.Maui.Services.Auth;
using ProfeAsistente.Shared.Dtos;

namespace ProfeAsistente.Maui.Views.Institutions;

public partial class InstitutionSelectorPage : ContentPage
{
    private readonly IAuthenticationService _auth;

    public InstitutionSelectorPage(IAuthenticationService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        List.ItemsSource = _auth.CurrentUser?.Memberships.Where(m => m.IsActive).ToList() ?? [];
        List.ItemTemplate = new DataTemplate(() =>
        {
            var label = new Label { Padding = 8 };
            label.SetBinding(Label.TextProperty, nameof(InstitutionMembershipDto.InstitutionName));
            return label;
        });
    }

    private async void OnContinue(object? sender, EventArgs e)
    {
        if (List.SelectedItem is InstitutionMembershipDto m)
            await _auth.SetActiveInstitutionAsync(m.InstitutionId);
        AppShell.ApplyMenuForCurrentUser(_auth);
        await Shell.Current.GoToAsync("//inicio");
    }
}
