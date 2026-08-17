using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Admin.Courses;

[QueryProperty(nameof(CourseId), "id")]
public partial class CourseDetailViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly IOfflineSyncService _sync;

    public CourseDetailViewModel(IApiClient api, IOfflineSyncService sync)
    {
        _api = api;
        _sync = sync;
    }

    public ObservableCollection<CourseSubjectDto> Subjects { get; } = [];
    public ObservableCollection<PlanificacionResumenDto> Plannings { get; } = [];

    [ObservableProperty] private string courseId = string.Empty;
    [ObservableProperty] private SchoolCourseDto? course;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string subjectsLine = string.Empty;

    partial void OnCourseIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (!Guid.TryParse(CourseId, out var id)) return;
        try
        {
            Course = await _api.GetCourseAsync(id);
            Subjects.Clear();
            foreach (var s in await _api.GetCourseSubjectsAsync(id))
                Subjects.Add(s);
            SubjectsLine = Subjects.Count == 0
                ? (Course?.Subtitle ?? "Sin asignatura asignada aún")
                : string.Join(" · ", Subjects.Select(s => s.SubjectName).Where(n => !string.IsNullOrWhiteSpace(n)));

            Plannings.Clear();
            foreach (var p in (await _sync.GetPlanificacionesAsync()).Where(p => p.SchoolCourseId == id))
                Plannings.Add(p);

            MensajeEstado = Course is null
                ? "Curso no encontrado."
                : Plannings.Count == 0
                    ? "Curso listo. Cree una planificación para este curso."
                    : $"{Plannings.Count} planificación(es) en este curso.";
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
    }

    [RelayCommand]
    private async Task OpenPlanningAsync(PlanificacionResumenDto? plan)
    {
        if (plan is null) return;
        await Shell.Current.GoToAsync($"planificacionDetalle?id={plan.Id}");
    }

    [RelayCommand]
    private async Task NewPlanningAsync()
        => await Shell.Current.GoToAsync($"nuevaPlanificacion?courseId={CourseId}");

    [RelayCommand]
    private async Task OpenAllPlanningsAsync()
        => await Shell.Current.GoToAsync($"//planificaciones?courseId={CourseId}");

    [RelayCommand]
    private async Task OpenMaterialsAsync()
        => await Shell.Current.GoToAsync($"//biblioteca?courseId={CourseId}");

    [RelayCommand]
    private async Task OpenRosterAsync()
        => await Shell.Current.GoToAsync($"//nomina");

    [RelayCommand]
    private async Task OpenTodayAsync()
        => await Shell.Current.GoToAsync("//inicio");
}
