using ProfeAsistente.Maui.ViewModels.Admin;

namespace ProfeAsistente.Maui.Views.Admin;

public partial class CurriculumImportPreviewPage : ContentPage
{
    public CurriculumImportPreviewPage(CurriculumImportPreviewViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CurriculumImportPreviewViewModel vm)
            await vm.CargarCommand.ExecuteAsync(null);
    }
}
