namespace ProfeAsistente.Maui.Views.Planning;

public partial class PlanningAlertsPage : ContentPage
{
    public PlanningAlertsPage(ViewModels.Planning.PlanningAlertsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
