using AppEducativa.Maui.ViewModels;

namespace AppEducativa.Maui.Views;

public partial class PlanificacionDetallePage : ContentPage
{
    public PlanificacionDetallePage(PlanificacionDetalleViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PlanificacionDetalleViewModel vm && !string.IsNullOrEmpty(vm.PlanificacionId))
            _ = vm.CargarCommand.ExecuteAsync(null);
    }
}
