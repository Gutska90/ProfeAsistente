namespace AppEducativa.Maui.Views.Admin.Users;

public partial class UserDetailPage : ContentPage
{
    public UserDetailPage(ViewModels.Admin.Users.UserDetailViewModel vm)
    {
        InitializeComponent();
        Msg.Text = vm.Mensaje;
    }
}
