namespace AppEducativa.Maui.Views.Auth;

public partial class ChangePasswordPage : ContentPage
{
    public ChangePasswordPage(ViewModels.Auth.ChangePasswordViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
