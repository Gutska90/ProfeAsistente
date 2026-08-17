using ProfeAsistente.Maui.ViewModels.Exports;

namespace ProfeAsistente.Maui.Views.Exports;

public partial class ExportOptionsPage : ContentPage
{
    public ExportOptionsPage(ExportOptionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
