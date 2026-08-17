namespace AppEducativa.Maui.Views.Planning;

public partial class PlanningSequenceGeneratorPage : ContentPage
{
    public PlanningSequenceGeneratorPage(ViewModels.Planning.PlanningSequenceGeneratorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
