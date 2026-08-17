using ProfeAsistente.Maui.ViewModels;

namespace ProfeAsistente.Maui.Views;

public partial class ClaseDetallePage : ContentPage
{
    private readonly ClaseDetalleViewModel _vm;

    public ClaseDetallePage(ClaseDetalleViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _vm.RefreshOnAppearingAsync();
    }
}
