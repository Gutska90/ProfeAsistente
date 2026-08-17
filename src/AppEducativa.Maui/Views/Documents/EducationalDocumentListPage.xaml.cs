using AppEducativa.Maui.ViewModels.Documents;

namespace AppEducativa.Maui.Views.Documents;

public partial class EducationalDocumentListPage : ContentPage
{
    public EducationalDocumentListPage(EducationalDocumentListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
