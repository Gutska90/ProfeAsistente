using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Planning;

[QueryProperty(nameof(PlanningId), "planningId")]
public partial class PlanningScheduleConfigurationViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public PlanningScheduleConfigurationViewModel(IApiClient api) => _api = api;

    public ObservableCollection<string> ExcludedLines { get; } = [];

    [ObservableProperty] private string planningId = string.Empty;
    [ObservableProperty] private DateTime startDate = DateTime.Today;
    [ObservableProperty] private DateTime endDate = DateTime.Today.AddDays(56);
    [ObservableProperty] private bool monday = true;
    [ObservableProperty] private bool wednesday = true;
    [ObservableProperty] private bool friday;
    [ObservableProperty] private TimeSpan startTime = new(8, 0, 0);
    [ObservableProperty] private int durationMinutes = 90;
    [ObservableProperty] private string excludedDateText = string.Empty;
    [ObservableProperty] private string excludedReason = "Feriado";
    [ObservableProperty] private int sessionCount;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    private readonly List<AddExcludedDateRequest> _excluded = [];

    partial void OnPlanningIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = CargarAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (!Guid.TryParse(PlanningId, out var id)) return;
        try
        {
            IsBusy = true;
            var cal = await _api.GetPlanningCalendarAsync(id);
            if (cal?.Configuration is null)
            {
                MensajeEstado = "Sin configuración. Defina horarios y genere sesiones.";
                return;
            }

            StartDate = cal.Configuration.StartDate.ToDateTime(TimeOnly.MinValue);
            EndDate = cal.Configuration.EndDate.ToDateTime(TimeOnly.MinValue);
            DurationMinutes = cal.Configuration.DefaultClassDurationMinutes;
            SessionCount = cal.AvailableSessionCount;
            MensajeEstado = $"{SessionCount} sesiones disponibles.";
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
    private void AgregarExclusion()
    {
        if (!DateOnly.TryParse(ExcludedDateText, out var date))
        {
            MensajeEstado = "Fecha excluida inválida (yyyy-MM-dd).";
            return;
        }

        _excluded.Add(new AddExcludedDateRequest
        {
            Date = date,
            Reason = ExcludedReason,
            ExclusionType = PlanningExclusionType.Holiday
        });
        ExcludedLines.Add($"{date:yyyy-MM-dd} — {ExcludedReason}");
        ExcludedDateText = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarYGenerarAsync()
    {
        if (!Guid.TryParse(PlanningId, out var id)) return;
        try
        {
            IsBusy = true;
            MensajeEstado = "Configurando horario…";
            var weekly = new List<WeeklyScheduleRequest>();
            void AddDay(bool on, DayOfWeek dow)
            {
                if (!on) return;
                weekly.Add(new WeeklyScheduleRequest
                {
                    DayOfWeek = dow,
                    StartTime = TimeOnly.FromTimeSpan(StartTime),
                    DurationMinutes = DurationMinutes,
                    SessionsPerDay = 1
                });
            }

            AddDay(Monday, DayOfWeek.Monday);
            AddDay(Wednesday, DayOfWeek.Wednesday);
            AddDay(Friday, DayOfWeek.Friday);

            await _api.ConfigurePlanningScheduleAsync(id, new ConfigurePlanningScheduleRequest
            {
                StartDate = DateOnly.FromDateTime(StartDate),
                EndDate = DateOnly.FromDateTime(EndDate),
                DefaultClassDurationMinutes = DurationMinutes,
                WeeklySchedule = weekly,
                ExcludedDates = _excluded
            });

            MensajeEstado = "Generando sesiones…";
            var cal = await _api.GenerateCalendarSessionsAsync(id);
            SessionCount = cal.AvailableSessionCount;
            MensajeEstado = $"Listo: {SessionCount} sesiones generadas.";
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
    private async Task IrCalendarioAsync()
        => await Shell.Current.GoToAsync($"planningCalendar?planningId={PlanningId}");

    [RelayCommand]
    private async Task IrSecuenciaAsync()
    {
        await Shell.Current.GoToAsync($"planningSequence?planningId={PlanningId}");
    }
}
