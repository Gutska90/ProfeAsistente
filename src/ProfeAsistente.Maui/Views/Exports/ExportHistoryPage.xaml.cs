using ProfeAsistente.Maui.ViewModels.Exports;

namespace ProfeAsistente.Maui.Views.Exports;

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
