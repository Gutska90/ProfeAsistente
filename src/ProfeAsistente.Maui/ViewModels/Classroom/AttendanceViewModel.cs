using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Classroom;

[QueryProperty(nameof(ClassId), "classId")]
public partial class AttendanceViewModel : ObservableObject
{
    private readonly IOfflineSyncService _sync;

    public AttendanceViewModel(IOfflineSyncService sync) => _sync = sync;

    public ObservableCollection<AttendanceRow> Rows { get; } = [];
    [ObservableProperty] private string classId = string.Empty;
    [ObservableProperty] private string? mensajeEstado;

    partial void OnClassIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (!Guid.TryParse(ClassId, out var id)) return;
        var roster = await _sync.GetClassRosterAsync(id);
        var existing = await _sync.GetAttendanceAsync(id);
        var present = existing.ToDictionary(a => a.StudentId, a => a.Status == AttendanceStatus.Present);

        Rows.Clear();
        if (roster is not null && roster.Students.Count > 0)
        {
            foreach (var s in roster.Students)
            {
                var isPresent = present.TryGetValue(s.StudentId, out var p) ? p : true;
                Rows.Add(new AttendanceRow { StudentId = s.StudentId, Name = s.DisplayName, Present = isPresent });
            }

            MensajeEstado = roster.CourseId == Guid.Empty
                ? roster.CourseName
                : $"{Rows.Count} estudiante(s) de {roster.CourseName}.";
            return;
        }

        foreach (var a in existing)
            Rows.Add(new AttendanceRow { StudentId = a.StudentId, Name = a.StudentName, Present = a.Status == AttendanceStatus.Present });

        MensajeEstado = Rows.Count == 0
            ? "No hay nómina en el curso de esta planificación. Inscríbalos en Nómina y PIE."
            : $"{Rows.Count} registro(s).";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Guid.TryParse(ClassId, out var id)) return;
        if (Rows.Count == 0)
        {
            MensajeEstado = "No hay estudiantes para guardar.";
            return;
        }

        await _sync.SaveAttendanceAsync(id, new SaveAttendanceRequest
        {
            Entries = Rows.Select(r => new AttendanceEntryRequest
            {
                StudentId = r.StudentId,
                Status = r.Present ? AttendanceStatus.Present : AttendanceStatus.Absent
            }).ToList()
        });
        MensajeEstado = _sync.PendingCount > 0
            ? "Asistencia en el dispositivo. Se enviará al reconectar (no es SIGE)."
            : "Asistencia guardada (registro local de apoyo, no SIGE).";
    }
}

public partial class AttendanceRow : ObservableObject
{
    [ObservableProperty] private Guid studentId;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private bool present = true;
}
