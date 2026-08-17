namespace ProfeAsistente.Maui.Views.Admin.Users;

public partial class UserListPage : ContentPage
{
    public UserListPage(ViewModels.Admin.Users.UserListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
