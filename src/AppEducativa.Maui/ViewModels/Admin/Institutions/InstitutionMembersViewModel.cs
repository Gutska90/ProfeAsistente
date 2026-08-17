using CommunityToolkit.Mvvm.ComponentModel;

namespace AppEducativa.Maui.ViewModels.Admin.Institutions;

public partial class InstitutionMembersViewModel : ObservableObject
{
    [ObservableProperty] private string mensaje = "Miembros (API: /api/institutions/{id}/members).";
}
