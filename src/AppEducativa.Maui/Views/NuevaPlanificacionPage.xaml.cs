using AppEducativa.Maui.ViewModels;

namespace AppEducativa.Maui.Views;

public partial class NuevaPlanificacionPage : ContentPage
{
    public NuevaPlanificacionPage(NuevaPlanificacionViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is NuevaPlanificacionViewModel vm)
            _ = vm.InicializarCommand.ExecuteAsync(null);
    }
}
