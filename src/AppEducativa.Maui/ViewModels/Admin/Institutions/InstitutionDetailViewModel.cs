using CommunityToolkit.Mvvm.ComponentModel;

namespace AppEducativa.Maui.ViewModels.Admin.Institutions;

public partial class InstitutionDetailViewModel : ObservableObject
{
    [ObservableProperty] private string mensaje = "Detalle de establecimiento.";
}
