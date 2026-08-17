using AppEducativa.Maui.ViewModels.Documents;

namespace AppEducativa.Maui.Views.Documents;

public partial class MaterialLibraryPage : ContentPage
{
    public MaterialLibraryPage(MaterialLibraryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Appearing += async (_, _) => await vm.CargarCommand.ExecuteAsync(null);
    }
}
