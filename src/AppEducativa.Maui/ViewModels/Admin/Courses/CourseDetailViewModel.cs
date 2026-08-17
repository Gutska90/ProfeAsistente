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

    public CourseDetailViewModel(IApiClient api) => _api = api;

    public ObservableCollection<CourseSubjectDto> Subjects { get; } = [];

    [ObservableProperty] private string courseId = string.Empty;
    [ObservableProperty] private SchoolCourseDto? course;
    [ObservableProperty] private string? mensajeEstado;

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
            MensajeEstado = Course is null
                ? "Curso no encontrado."
                : Subjects.Count == 0
                    ? "Curso listo. Inscriba estudiantes en Nómina y PIE."
                    : $"{Subjects.Count} asignatura(s) asignada(s).";
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
    }

    [RelayCommand]
    private async Task OpenRosterAsync() => await Shell.Current.GoToAsync("//nomina");
}
