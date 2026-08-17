using System.Collections.ObjectModel;
using System.Globalization;
using AppEducativa.Maui.Services;
using AppEducativa.Maui.Services.Auth;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Classroom;

[QueryProperty(nameof(ClassId), "classId")]
public partial class ClassAssessmentViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly IAuthenticationService _auth;
    private readonly IOfflineSyncService _sync;

    public ClassAssessmentViewModel(IApiClient api, IAuthenticationService auth, IOfflineSyncService sync)
    {
        _api = api;
        _auth = auth;
        _sync = sync;
    }

    public ObservableCollection<LearningAssessmentDto> Assessments { get; } = [];
    public ObservableCollection<ScoreRow> Scores { get; } = [];
    public IReadOnlyList<string> PurposeNames { get; } = ["Diagnóstica", "Formativa", "Sumativa"];
    public IReadOnlyList<string> AchievementLevels { get; } = ["Por lograr", "Medianamente logrado", "Logrado"];

    [ObservableProperty] private string classId = string.Empty;
    [ObservableProperty] private LearningAssessmentDto? selected;
    [ObservableProperty] private string newName = "Ticket de salida";
    [ObservableProperty] private string purposeName = "Formativa";
    [ObservableProperty] private string criteria = "Indicadores de evaluación del OA de la clase.";
    [ObservableProperty] private string? mensajeEstado;

    partial void OnClassIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = LoadAsync();
    }

    partial void OnSelectedChanged(LearningAssessmentDto? value)
    {
        if (value is not null)
            _ = LoadScoresAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (!Guid.TryParse(ClassId, out var id)) return;
        Assessments.Clear();
        foreach (var a in await _sync.GetAssessmentsAsync(id))
            Assessments.Add(a);
        Selected = Assessments.FirstOrDefault();
        MensajeEstado = Assessments.Count == 0
            ? "Cree una evaluación formativa o sumativa alineada al OA de esta clase. No reemplaza el libro oficial."
            : $"{Assessments.Count} evaluación(es) de esta clase.";
        if (Selected is not null)
            await LoadScoresAsync();
        else
            Scores.Clear();
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (!Guid.TryParse(ClassId, out var id)) return;
        var created = await _api.CreateAssessmentAsync(new CreateLearningAssessmentRequest
        {
            InstitutionId = _auth.ActiveInstitutionId ?? Guid.Empty,
            ClassId = id,
            Purpose = PurposeName switch
            {
                "Diagnóstica" => EvaluationPurpose.Diagnostic,
                "Sumativa" => EvaluationPurpose.Summative,
                _ => EvaluationPurpose.Formative
            },
            Name = string.IsNullOrWhiteSpace(NewName) ? "Evaluación de la clase" : NewName.Trim(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Criteria = Criteria
        });
        MensajeEstado = $"Creada: {created.Name} ({PurposeLabel(created.Purpose)}).";
        await LoadAsync();
        Selected = Assessments.FirstOrDefault(a => a.Id == created.Id);
    }

    [RelayCommand]
    private async Task LoadScoresAsync()
    {
        if (Selected is null) return;
        Scores.Clear();
        foreach (var s in await _sync.GetScoresAsync(Selected.Id))
        {
            Scores.Add(new ScoreRow
            {
                StudentId = s.StudentId,
                Name = s.StudentName,
                ScoreText = s.Score?.ToString(CultureInfo.CurrentCulture) ?? string.Empty,
                AchievementLevel = string.IsNullOrWhiteSpace(s.AchievementLevel) ? "Medianamente logrado" : s.AchievementLevel,
                Feedback = s.Feedback ?? string.Empty
            });
        }

        if (Scores.Count == 0)
            MensajeEstado = "No hay nómina en el curso. Inscriba estudiantes para registrar puntajes.";
        else
            MensajeEstado = $"{Scores.Count} estudiante(s). Niveles: por lograr / medianamente logrado / logrado.";
    }

    [RelayCommand]
    private async Task SaveScoresAsync()
    {
        if (Selected is null)
        {
            MensajeEstado = "Seleccione o cree una evaluación.";
            return;
        }

        if (Scores.Count == 0)
        {
            MensajeEstado = "No hay estudiantes para puntuar.";
            return;
        }

        await _sync.SaveScoresAsync(Selected.Id, Scores.Select(r => new SaveAssessmentScoreRequest
        {
            StudentId = r.StudentId,
            Score = decimal.TryParse(r.ScoreText, NumberStyles.Number, CultureInfo.CurrentCulture, out var n) ? n : null,
            AchievementLevel = r.AchievementLevel,
            Feedback = string.IsNullOrWhiteSpace(r.Feedback) ? null : r.Feedback.Trim()
        }).ToList());
        MensajeEstado = _sync.PendingCount > 0
            ? "Puntajes en el dispositivo. Se enviarán al reconectar (no es SIGE)."
            : "Puntajes guardados (registro local de apoyo, no SIGE).";
    }

    public static string PurposeLabel(EvaluationPurpose purpose) => purpose switch
    {
        EvaluationPurpose.Diagnostic => "Diagnóstica",
        EvaluationPurpose.Summative => "Sumativa",
        _ => "Formativa"
    };
}

public partial class ScoreRow : ObservableObject
{
    [ObservableProperty] private Guid studentId;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string scoreText = string.Empty;
    [ObservableProperty] private string achievementLevel = "Medianamente logrado";
    [ObservableProperty] private string feedback = string.Empty;
}
