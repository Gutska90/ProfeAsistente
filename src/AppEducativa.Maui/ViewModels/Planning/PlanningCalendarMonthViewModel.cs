namespace AppEducativa.Maui.ViewModels.Planning;

public class PlanningCalendarMonthViewModel : PlanningCalendarViewModel
{
    public PlanningCalendarMonthViewModel(Services.IApiClient api) : base(api)
    {
        ViewMode = "Mes";
    }
}
