using ProfeAsistente.Maui.ViewModels.Documents;

namespace ProfeAsistente.Maui.Views.Documents;

public partial class EducationalDocumentComparisonPage : ContentPage
{
    public EducationalDocumentComparisonPage(EducationalDocumentComparisonViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
