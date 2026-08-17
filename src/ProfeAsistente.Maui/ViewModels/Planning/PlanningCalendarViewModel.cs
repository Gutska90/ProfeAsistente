using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Planning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Planning;

[QueryProperty(nameof(PlanningId), "planningId")]
public partial class PlanningCalendarViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public PlanningCalendarViewModel(IApiClient api) => _api = api;

    public ObservableCollection<PlanningCalendarSessionDto> Sessions { get; } = [];
    public ObservableCollection<PlanningCalendarSessionDto> DaySessions { get; } = [];
    public ObservableCollection<CalendarDayCell> MonthDays { get; } = [];
    public ObservableCollection<CalendarDayCell> WeekDays { get; } = [];
    public IReadOnlyList<string> WeekdayHeaders { get; } = PlanningCalendarLayout.WeekdayHeaders;

    [ObservableProperty] private string planningId = string.Empty;
    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    [ObservableProperty] private string viewMode = "Mes";
    [ObservableProperty] private string periodTitle = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private bool hasCalendar;
    [ObservableProperty] private PlanningCalendarSessionDto? selectedSession;

    public bool ShowMonth => string.Equals(ViewMode, "Mes", StringComparison.Ordinal);
    public bool ShowWeek => string.Equals(ViewMode, "Semana", StringComparison.Ordinal);
    public bool ShowList => string.Equals(ViewMode, "Lista", StringComparison.Ordinal);

    partial void OnPlanningIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = CargarAsync();
    }

    partial void OnSelectedDateChanged(DateTime value) => RebuildLayout();

    partial void OnViewModeChanged(string value)
    {
        OnPropertyChanged(nameof(ShowMonth));
        OnPropertyChanged(nameof(ShowWeek));
        OnPropertyChanged(nameof(ShowList));
        RebuildLayout();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (!Guid.TryParse(PlanningId, out var id)) return;
        try
        {
            IsBusy = true;
            var cal = await _api.GetPlanningCalendarAsync(id);
            Sessions.Clear();
            HasCalendar = cal is not null && cal.Sessions.Count > 0;
            if (cal is null || cal.Sessions.Count == 0)
            {
                MensajeEstado = "Sin sesiones. Configure el horario (lunes/miércoles/viernes) y genere el calendario. No hay feriados automáticos.";
                RebuildLayout();
                return;
            }

            foreach (var s in cal.Sessions.OrderBy(x => x.ScheduledDate).ThenBy(x => x.SessionNumber))
                Sessions.Add(s);

            var first = Sessions.Min(s => s.ScheduledDate);
            var last = Sessions.Max(s => s.ScheduledDate);
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (SelectedDate.Date == DateTime.Today && (today < first || today > last))
                SelectedDate = first.ToDateTime(TimeOnly.MinValue);

            RebuildLayout();
            MensajeEstado = $"{cal.AvailableSessionCount} activas · {cal.CancelledSessionCount} canceladas. Toque un día para ver la clase.";
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectDay(CalendarDayCell? cell)
    {
        if (cell is null) return;
        SelectedDate = cell.Date.ToDateTime(TimeOnly.MinValue);
    }

    [RelayCommand]
    private void PreviousPeriod()
    {
        SelectedDate = ShowWeek ? SelectedDate.AddDays(-7) : SelectedDate.AddMonths(-1);
    }

    [RelayCommand]
    private void NextPeriod()
    {
        SelectedDate = ShowWeek ? SelectedDate.AddDays(7) : SelectedDate.AddMonths(1);
    }

    [RelayCommand]
    private void ShowMonthView() => ViewMode = "Mes";

    [RelayCommand]
    private void ShowWeekView() => ViewMode = "Semana";

    [RelayCommand]
    private void ShowListView() => ViewMode = "Lista";

    [RelayCommand]
    private async Task CancelarAsync(PlanningCalendarSessionDto? session)
    {
        if (session is null) return;
        if (!await Application.Current!.MainPage!.DisplayAlert("Cancelar sesión", $"¿Cancelar sesión {session.SessionNumber}?", "Sí", "No"))
            return;
        await _api.CancelSessionAsync(session.Id, new CancelPlanningSessionRequest { Reason = "Cancelada desde calendario" });
        await CargarAsync();
    }

    [RelayCommand]
    private async Task ReprogramarAsync()
    {
        if (SelectedSession is null)
        {
            MensajeEstado = "Elija una sesión de la lista del día.";
            return;
        }

        var target = DateOnly.FromDateTime(SelectedDate);
        await _api.RescheduleSessionAsync(SelectedSession.Id, new RescheduleSessionRequest
        {
            NewDate = target,
            Reason = "Reprogramada desde el calendario"
        });
        MensajeEstado = $"Sesión {SelectedSession.SessionNumber} movida al {target:dd/MM}.";
        await CargarAsync();
    }

    [RelayCommand]
    private async Task AbrirClaseAsync(PlanningCalendarSessionDto? session)
    {
        session ??= SelectedSession;
        if (session?.ClassId is Guid classId)
        {
            await Shell.Current.GoToAsync($"claseDetalle?id={classId}");
            return;
        }

        MensajeEstado = "Esta sesión aún no tiene clase. Genere o confirme la secuencia pedagógica.";
    }

    [RelayCommand]
    private async Task OpenWeekPageAsync()
        => await Shell.Current.GoToAsync($"planningCalendarWeek?planningId={PlanningId}");

    [RelayCommand]
    private async Task OpenMonthPageAsync()
        => await Shell.Current.GoToAsync($"planningCalendarMonth?planningId={PlanningId}");

    [RelayCommand]
    private async Task ConfigurarAsync()
        => await Shell.Current.GoToAsync($"planningSchedule?planningId={PlanningId}");

    [RelayCommand]
    private async Task CoberturaAsync()
        => await Shell.Current.GoToAsync($"planningCoverage?planningId={PlanningId}");

    [RelayCommand]
    private async Task AlertasAsync()
        => await Shell.Current.GoToAsync($"planningAlerts?planningId={PlanningId}");

    private void RebuildLayout()
    {
        var selected = DateOnly.FromDateTime(SelectedDate);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var list = Sessions.ToList();

        MonthDays.Clear();
        foreach (var c in PlanningCalendarLayout.BuildMonth(selected, list, selected, today))
            MonthDays.Add(c);

        WeekDays.Clear();
        foreach (var c in PlanningCalendarLayout.BuildWeek(selected, list, selected, today))
            WeekDays.Add(c);

        PeriodTitle = ShowWeek
            ? PlanningCalendarLayout.WeekTitle(selected)
            : PlanningCalendarLayout.MonthTitle(selected);

        DaySessions.Clear();
        foreach (var s in Sessions.Where(x => x.ScheduledDate == selected).OrderBy(x => x.StartTime).ThenBy(x => x.SessionNumber))
            DaySessions.Add(s);

        if (SelectedSession is not null && DaySessions.All(s => s.Id != SelectedSession.Id))
            SelectedSession = DaySessions.FirstOrDefault();
        else if (SelectedSession is null)
            SelectedSession = DaySessions.FirstOrDefault();
    }
}
