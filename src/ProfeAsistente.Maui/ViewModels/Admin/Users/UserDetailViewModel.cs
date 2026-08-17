using CommunityToolkit.Mvvm.ComponentModel;

namespace ProfeAsistente.Maui.ViewModels.Admin.Users;

public partial class UserDetailViewModel : ObservableObject
{
    [ObservableProperty] private string mensaje = "Detalle de usuario (API: GET /api/admin/users/{id}).";
}
