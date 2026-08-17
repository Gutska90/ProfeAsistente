using AppEducativa.Maui.ViewModels;

namespace AppEducativa.Maui.Views;

public partial class AdministrarCurriculumPage : ContentPage
{
    public AdministrarCurriculumPage(AdministrarCurriculumViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AdministrarCurriculumViewModel vm)
            await vm.InicializarCommand.ExecuteAsync(null);
    }
}
