using AppEducativa.Maui.ViewModels.Exports;

namespace AppEducativa.Maui.Views.Exports;

public partial class ExportHistoryPage : ContentPage
{
    public ExportHistoryPage(ExportHistoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ExportHistoryViewModel vm)
            _ = vm.CargarAsync();
    }
}
