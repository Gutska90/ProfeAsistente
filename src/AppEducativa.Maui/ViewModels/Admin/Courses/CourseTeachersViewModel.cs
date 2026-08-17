using CommunityToolkit.Mvvm.ComponentModel;

namespace AppEducativa.Maui.ViewModels.Admin.Courses;

public partial class CourseTeachersViewModel : ObservableObject
{
    [ObservableProperty] private string mensaje = "Asignación docente (API: /api/course-subjects/{id}/teachers).";
}