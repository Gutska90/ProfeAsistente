using AppEducativa.Maui.ViewModels.Documents;

namespace AppEducativa.Maui.Views.Documents;

public partial class EducationalItemEditorPage : ContentPage
{
    public EducationalItemEditorPage(EducationalItemEditorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
