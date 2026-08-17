using AppEducativa.Maui.ViewModels;

namespace AppEducativa.Maui.Views;

public partial class PlanificacionListPage : ContentPage
{
    public PlanificacionListPage(PlanificacionListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PlanificacionListViewModel vm)
            _ = vm.InicializarCommand.ExecuteAsync(null);
    }
}
