namespace ProfeAsistente.Maui.Views.Planning;

public partial class PlanningCalendarPage : ContentPage
{
    public PlanningCalendarPage(ViewModels.Planning.PlanningCalendarViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
