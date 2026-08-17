using CommunityToolkit.Mvvm.ComponentModel;

namespace AppEducativa.Maui.ViewModels.Admin.Users;

public partial class UserRolesViewModel : ObservableObject
{
    [ObservableProperty] private string mensaje = "Roles (API: POST /api/admin/users/{id}/roles).";
}
