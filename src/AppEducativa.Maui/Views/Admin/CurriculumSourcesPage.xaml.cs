using AppEducativa.Maui.ViewModels.Admin;

namespace AppEducativa.Maui.Views.Admin;

public partial class CurriculumSourcesPage : ContentPage
{
    public CurriculumSourcesPage(CurriculumSourcesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CurriculumSourcesViewModel vm)
            await vm.InicializarCommand.ExecuteAsync(null);
    }
}
