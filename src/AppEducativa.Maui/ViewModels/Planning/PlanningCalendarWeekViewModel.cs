namespace AppEducativa.Maui.ViewModels.Planning;

public class PlanningCalendarWeekViewModel : PlanningCalendarViewModel
{
    public PlanningCalendarWeekViewModel(Services.IApiClient api) : base(api)
    {
        ViewMode = "Semana";
    }
}
