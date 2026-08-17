namespace ProfeAsistente.Maui.Views.Planning;

public partial class PlanningCalendarMonthPage : ContentPage
{
    public PlanningCalendarMonthPage(ViewModels.Planning.PlanningCalendarMonthViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
