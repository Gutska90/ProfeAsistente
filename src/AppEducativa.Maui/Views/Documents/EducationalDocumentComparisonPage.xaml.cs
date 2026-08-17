using AppEducativa.Maui.ViewModels.Documents;

namespace AppEducativa.Maui.Views.Documents;

public partial class EducationalDocumentComparisonPage : ContentPage
{
    public EducationalDocumentComparisonPage(EducationalDocumentComparisonViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
