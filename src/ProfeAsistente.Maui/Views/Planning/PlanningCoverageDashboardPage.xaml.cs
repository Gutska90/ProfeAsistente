namespace ProfeAsistente.Maui.Views.Planning;

public partial class PlanningCoverageDashboardPage : ContentPage
{
    public PlanningCoverageDashboardPage(ViewModels.Planning.PlanningCoverageDashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
