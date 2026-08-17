using AppEducativa.Maui.ViewModels;

namespace AppEducativa.Maui.Views;

public partial class ClaseDetallePage : ContentPage
{
    public ClaseDetallePage(ClaseDetalleViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
