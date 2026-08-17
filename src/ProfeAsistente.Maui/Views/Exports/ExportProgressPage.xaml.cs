using ProfeAsistente.Maui.ViewModels.Exports;

namespace ProfeAsistente.Maui.Views.Exports;

public partial class ExportProgressPage : ContentPage
{
    public ExportProgressPage(ExportProgressViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
