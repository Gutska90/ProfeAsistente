using AppEducativa.Maui.ViewModels.Documents;

namespace AppEducativa.Maui.Views.Documents;

public partial class EducationalDocumentEditorPage : ContentPage
{
    public EducationalDocumentEditorPage(EducationalDocumentEditorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
