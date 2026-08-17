using AppEducativa.Maui.ViewModels.Exports;

namespace AppEducativa.Maui.Views.Exports;

public partial class ExportOptionsPage : ContentPage
{
    public ExportOptionsPage(ExportOptionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
