using AppEducativa.Maui.ViewModels.Documents;

namespace AppEducativa.Maui.Views.Documents;

public partial class AssessmentSpecificationPage : ContentPage
{
    public AssessmentSpecificationPage(AssessmentSpecificationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
