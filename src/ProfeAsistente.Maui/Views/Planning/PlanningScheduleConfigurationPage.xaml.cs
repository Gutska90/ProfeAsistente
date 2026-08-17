namespace ProfeAsistente.Maui.Views.Planning;

public partial class PlanningScheduleConfigurationPage : ContentPage
{
    public PlanningScheduleConfigurationPage(ViewModels.Planning.PlanningScheduleConfigurationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
