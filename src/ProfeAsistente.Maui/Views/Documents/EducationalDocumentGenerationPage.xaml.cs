using ProfeAsistente.Maui.ViewModels.Documents;

namespace ProfeAsistente.Maui.Views.Documents;

public partial class EducationalDocumentGenerationPage : ContentPage
{
    public EducationalDocumentGenerationPage(EducationalDocumentGenerationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
