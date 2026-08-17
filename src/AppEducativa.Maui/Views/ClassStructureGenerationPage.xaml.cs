using AppEducativa.Maui.ViewModels;

namespace AppEducativa.Maui.Views;

public partial class ClassStructureGenerationPage : ContentPage
{
    public ClassStructureGenerationPage(ClassStructureGenerationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ClassStructureGenerationViewModel vm
            && Guid.TryParse(vm.ClaseId, out _)
            && vm.Contexto is null)
        {
            await vm.CargarCommand.ExecuteAsync(null);
        }
    }
}
