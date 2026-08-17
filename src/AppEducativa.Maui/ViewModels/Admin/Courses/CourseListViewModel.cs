using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Maui.Services.Auth;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Admin.Courses;

public partial class CourseListViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly IAuthenticationService _auth;

    public CourseListViewModel(IApiClient api, IAuthenticationService auth)
    {
        _api = api;
        _auth = auth;
    }

    public ObservableCollection<SchoolCourseDto> Courses { get; } = [];
    public ObservableCollection<NivelDto> Niveles { get; } = [];

    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string newName = string.Empty;
    [ObservableProperty] private string newSection = "A";
    [ObservableProperty] private NivelDto? selectedLevel;
    [ObservableProperty] private bool isBusy;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var inst = _auth.ActiveInstitutionId;
        if (inst is null)
        {
            MensajeEstado = "Seleccione un establecimiento (Perfil o al iniciar sesión).";
            return;
        }

        try
        {
            IsBusy = true;
            if (Niveles.Count == 0)
            {
                foreach (var n in await _api.GetNivelesAsync())
                    Niveles.Add(n);
                SelectedLevel ??= Niveles.FirstOrDefault(n => n.Codigo == "4B") ?? Niveles.FirstOrDefault();
            }

            Courses.Clear();
            foreach (var c in await _api.GetCoursesAsync(inst.Value))
                Courses.Add(c);
            MensajeEstado = Courses.Count == 0
                ? "No hay cursos. Cree uno abajo (nivel + letra)."
                : $"{Courses.Count} curso(s). Toque uno para entrar.";
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
    private async Task CreateAsync()
    {
        var inst = _auth.ActiveInstitutionId;
        if (inst is null)
        {
            MensajeEstado = "Seleccione un establecimiento.";
            return;
        }

        if (SelectedLevel is null)
        {
            MensajeEstado = "Elija el nivel del curso.";
            return;
        }

        var name = string.IsNullOrWhiteSpace(NewName) ? SelectedLevel.Nombre : NewName.Trim();
        try
        {
            IsBusy = true;
            var period = await EnsurePeriodAsync(inst.Value);
            await _api.CreateCourseAsync(inst.Value, new CreateSchoolCourseRequest
            {
                AcademicPeriodId = period.Id,
                LevelId = SelectedLevel.Id,
                Name = name,
                Section = string.IsNullOrWhiteSpace(NewSection) ? "A" : NewSection.Trim()
            });
            NewName = string.Empty;
            MensajeEstado = "Curso creado.";
            await LoadAsync();
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
    private async Task OpenAsync(SchoolCourseDto? course)
    {
        if (course is null) return;
        await Shell.Current.GoToAsync($"courseDetail?id={course.Id}");
    }

    private async Task<AcademicPeriodDto> EnsurePeriodAsync(Guid institutionId)
    {
        var periods = await _api.GetAcademicPeriodsAsync(institutionId);
        var current = periods.FirstOrDefault(p => p.IsCurrent) ?? periods.FirstOrDefault();
        if (current is not null) return current;
        var year = DateTime.Today.Year;
        return await _api.CreateAcademicPeriodAsync(institutionId, new CreateAcademicPeriodRequest
        {
            Name = $"Año escolar {year}",
            Year = year,
            StartDate = new DateOnly(year, 3, 1),
            EndDate = new DateOnly(year, 12, 20),
            IsCurrent = true
        });
    }
}
