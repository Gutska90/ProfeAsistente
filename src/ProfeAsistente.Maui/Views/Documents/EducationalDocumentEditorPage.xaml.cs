using ProfeAsistente.Maui.ViewModels.Documents;

namespace ProfeAsistente.Maui.Views.Documents;

public partial class EducationalDocumentEditorPage : ContentPage
{
    public EducationalDocumentEditorPage(EducationalDocumentEditorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
