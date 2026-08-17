using AppEducativa.Maui.ViewModels.Documents;

namespace AppEducativa.Maui.Views.Documents;

public partial class EducationalDocumentGenerationPage : ContentPage
{
    public EducationalDocumentGenerationPage(EducationalDocumentGenerationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
