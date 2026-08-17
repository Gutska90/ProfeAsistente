using AppEducativa.Maui.ViewModels.Admin;

namespace AppEducativa.Maui.Views.Admin;

public partial class CurriculumImportsPage : ContentPage
{
    public CurriculumImportsPage(CurriculumImportsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CurriculumImportsViewModel vm)
            await vm.InicializarCommand.ExecuteAsync(null);
    }
}
