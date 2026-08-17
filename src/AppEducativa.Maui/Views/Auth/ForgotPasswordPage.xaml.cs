namespace AppEducativa.Maui.Views.Auth;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage(ViewModels.Auth.ForgotPasswordViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
