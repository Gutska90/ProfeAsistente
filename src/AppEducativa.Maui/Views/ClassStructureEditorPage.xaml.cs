using AppEducativa.Maui.ViewModels;

namespace AppEducativa.Maui.Views;

public partial class ClassStructureEditorPage : ContentPage
{
    public ClassStructureEditorPage(ClassStructureEditorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ClassStructureEditorViewModel vm
            && vm.GenerationId != Guid.Empty
            && string.IsNullOrWhiteSpace(vm.Title))
        {
            await vm.CargarCommand.ExecuteAsync(null);
        }
    }
}
