using ProfeAsistente.Maui.ViewModels.Documents;

namespace ProfeAsistente.Maui.Views.Documents;

public partial class AssessmentSpecificationPage : ContentPage
{
    public AssessmentSpecificationPage(AssessmentSpecificationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
