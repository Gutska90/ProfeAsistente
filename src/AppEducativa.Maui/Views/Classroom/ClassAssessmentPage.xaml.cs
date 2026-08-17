namespace AppEducativa.Maui.Views.Classroom;

public partial class ClassAssessmentPage : ContentPage
{
    public ClassAssessmentPage(ViewModels.Classroom.ClassAssessmentViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
