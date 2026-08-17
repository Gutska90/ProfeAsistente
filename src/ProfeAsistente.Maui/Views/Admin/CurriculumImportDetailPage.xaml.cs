using ProfeAsistente.Maui.ViewModels.Admin;

namespace ProfeAsistente.Maui.Views.Admin;

public partial class CurriculumImportDetailPage : ContentPage
{
    public CurriculumImportDetailPage(CurriculumImportDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CurriculumImportDetailViewModel vm)
            await vm.CargarCommand.ExecuteAsync(null);
    }
}
