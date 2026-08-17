namespace AppEducativa.Maui.Views.Admin.Users;

public partial class CreateUserPage : ContentPage
{
    public CreateUserPage(ViewModels.Admin.Users.CreateUserViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
