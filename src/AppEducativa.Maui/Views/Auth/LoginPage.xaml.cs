namespace AppEducativa.Maui.Views.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage(ViewModels.Auth.LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnForgotClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("forgotPassword");
}
