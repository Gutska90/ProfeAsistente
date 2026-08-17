using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Maui.Services.Auth;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Classroom;

public partial class CourseRosterViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly IAuthenticationService _auth;

    public CourseRosterViewModel(IApiClient api, IAuthenticationService auth)
    {
        _api = api;
        _auth = auth;
    }

    public ObservableCollection<SchoolCourseDto> Courses { get; } = [];
    public ObservableCollection<RosterStudentDto> Roster { get; } = [];
    public ObservableCollection<StudentDto> InstitutionStudents { get; } = [];

    [ObservableProperty] private SchoolCourseDto? selectedCourse;
    [ObservableProperty] private StudentDto? selectedInstitutionStudent;
    [ObservableProperty] private RosterStudentDto? selectedRosterStudent;
    [ObservableProperty] private string firstName = string.Empty;
    [ObservableProperty] private string lastName = string.Empty;
    [ObservableProperty] private string pieStrategies = "Tiempo adicional, instrucción multimodal y evidencia alternativa (DUA).";
    [ObservableProperty] private string? mensajeEstado;

    partial void OnSelectedCourseChanged(SchoolCourseDto? value)
    {
        if (value is not null)
            _ = LoadRosterAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var inst = _auth.ActiveInstitutionId;
        if (inst is null)
        {
            MensajeEstado = "Seleccione un establecimiento.";
            return;
        }

        var previous = SelectedCourse?.Id;
        Courses.Clear();
        foreach (var c in await _api.GetCoursesAsync(inst.Value))
            Courses.Add(c);

        SelectedCourse = Courses.FirstOrDefault(c => c.Id == previous) ?? Courses.FirstOrDefault();

        InstitutionStudents.Clear();
        foreach (var s in await _api.GetStudentsAsync(inst.Value))
            InstitutionStudents.Add(s);

        if (SelectedCourse is null)
            MensajeEstado = "No hay cursos. Créelos en Cursos o pida a un administrador.";
        else
            await LoadRosterAsync();
    }

    [RelayCommand]
    private async Task LoadRosterAsync()
    {
        if (SelectedCourse is null) return;
        var roster = await _api.GetRosterAsync(SelectedCourse.Id);
        Roster.Clear();
        if (roster is not null)
        {
            foreach (var s in roster.Students)
                Roster.Add(s);
        }

        MensajeEstado = $"{Roster.Count} inscrito(s) en {SelectedCourse.DisplayName}. Agregue o inscriba estudiantes al curso.";
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var inst = _auth.ActiveInstitutionId;
        if (inst is null || string.IsNullOrWhiteSpace(FirstName)) return;
        var created = await _api.CreateStudentAsync(inst.Value, new CreateStudentRequest
        {
            InstitutionId = inst.Value,
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim()
        });
        if (SelectedCourse is not null)
            await _api.EnrollStudentAsync(SelectedCourse.Id, created.Id);
        FirstName = string.Empty;
        LastName = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task EnrollSelectedAsync()
    {
        if (SelectedCourse is null)
        {
            MensajeEstado = "Seleccione un curso.";
            return;
        }

        if (SelectedInstitutionStudent is null)
        {
            MensajeEstado = "Seleccione un estudiante del establecimiento.";
            return;
        }

        await _api.EnrollStudentAsync(SelectedCourse.Id, SelectedInstitutionStudent.Id);
        await LoadRosterAsync();
        MensajeEstado = $"{SelectedInstitutionStudent.DisplayName} inscrito en {SelectedCourse.DisplayName}.";
    }

    [RelayCommand]
    private async Task AddPieAsync()
    {
        var studentId = SelectedRosterStudent?.StudentId ?? SelectedInstitutionStudent?.Id;
        if (studentId is null)
        {
            MensajeEstado = "Seleccione un estudiante.";
            return;
        }

        await _api.AddSupportPlanAsync(studentId.Value, new CreateSupportPlanRequest
        {
            PlanType = SupportPlanType.Pie,
            NeedType = SpecialEducationalNeedType.Transitory,
            Title = "Plan de apoyo PIE / Decreto 83",
            Strategies = PieStrategies,
            AccessAdjustments = "Acceso: representación múltiple y participación.",
            StartDate = DateOnly.FromDateTime(DateTime.Today)
        });
        MensajeEstado = "Plan PIE/DUA registrado. Aparecerá en el dashboard y en la clase.";
        await LoadAsync();
    }
}
