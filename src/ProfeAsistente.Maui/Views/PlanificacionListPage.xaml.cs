using ProfeAsistente.Maui.ViewModels;

namespace ProfeAsistente.Maui.Views;

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
