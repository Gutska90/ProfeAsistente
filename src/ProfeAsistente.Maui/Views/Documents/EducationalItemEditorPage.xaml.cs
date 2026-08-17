using ProfeAsistente.Maui.ViewModels.Documents;

namespace ProfeAsistente.Maui.Views.Documents;

public partial class EducationalItemEditorPage : ContentPage
{
    public EducationalItemEditorPage(EducationalItemEditorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
