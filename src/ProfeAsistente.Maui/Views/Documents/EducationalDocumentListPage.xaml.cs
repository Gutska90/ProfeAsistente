using ProfeAsistente.Maui.ViewModels.Documents;

namespace ProfeAsistente.Maui.Views.Documents;

public partial class EducationalDocumentListPage : ContentPage
{
    public EducationalDocumentListPage(EducationalDocumentListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
