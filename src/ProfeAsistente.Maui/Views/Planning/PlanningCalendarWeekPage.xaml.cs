namespace ProfeAsistente.Maui.Views.Planning;

public partial class PlanningCalendarWeekPage : ContentPage
{
    public PlanningCalendarWeekPage(ViewModels.Planning.PlanningCalendarWeekViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
