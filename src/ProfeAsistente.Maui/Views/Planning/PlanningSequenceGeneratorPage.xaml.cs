namespace ProfeAsistente.Maui.Views.Planning;

public partial class PlanningSequenceGeneratorPage : ContentPage
{
    public PlanningSequenceGeneratorPage(ViewModels.Planning.PlanningSequenceGeneratorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
