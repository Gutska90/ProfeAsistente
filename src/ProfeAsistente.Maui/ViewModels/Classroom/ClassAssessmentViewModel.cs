using System.Collections.ObjectModel;
using System.Globalization;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Maui.Services.Auth;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Classroom;

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
    public ObservableCollection<AssessmentSpecificationRowDto> SpecRows { get; } = [];
    public ObservableCollection<string> IndicatorLines { get; } = [];
    public ObservableCollection<string> SupportStudents { get; } = [];
    public IReadOnlyList<string> PurposeNames { get; } = ["Diagnóstica", "Formativa", "Sumativa"];
    public IReadOnlyList<string> AchievementLevels { get; } = ["Por lograr", "Medianamente logrado", "Logrado"];

    [ObservableProperty] private string classId = string.Empty;
    [ObservableProperty] private LearningAssessmentDto? selected;
    [ObservableProperty] private AssessmentEvidenceSummaryDto? evidence;
    [ObservableProperty] private string newName = "Ticket de salida";
    [ObservableProperty] private string purposeName = "Formativa";
    [ObservableProperty] private string criteria = "Indicadores de evaluación del OA de la clase.";
    [ObservableProperty] private string oaHeader = string.Empty;
    [ObservableProperty] private string readingSummary = string.Empty;
    [ObservableProperty] private string levelCounts = string.Empty;
    [ObservableProperty] private bool needsReinforcement;
    [ObservableProperty] private bool hasSpecification;
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
        try
        {
            var clase = await _api.GetClaseAsync(id);
            if (clase is not null)
            {
                OaHeader = $"OA {clase.ObjetivoCodigo}: {clase.ObjetivoDescripcion}";
                if (string.IsNullOrWhiteSpace(Criteria) || Criteria.StartsWith("Indicadores", StringComparison.Ordinal))
                    Criteria = clase.Indicadores.Count == 0
                        ? "Indicadores de evaluación del OA de la clase."
                        : string.Join(" · ", clase.Indicadores.Take(3));
            }

            Assessments.Clear();
            foreach (var a in await _sync.GetAssessmentsAsync(id))
                Assessments.Add(a);
            Selected = Assessments.FirstOrDefault();
            MensajeEstado = Assessments.Count == 0
                ? "Cree una evaluación alineada al OA. Luego registre niveles de logro y lea la evidencia."
                : $"{Assessments.Count} evaluación(es) de esta clase.";
            if (Selected is not null)
                await LoadScoresAsync();
            else
            {
                Scores.Clear();
                ClearEvidenceUi();
            }
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
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
        MensajeEstado = $"Creada: {created.Name} ({PurposeLabel(created.Purpose)})"
                        + (string.IsNullOrWhiteSpace(created.ObjectiveCode) ? "" : $" · OA {created.ObjectiveCode}")
                        + (created.EducationalDocumentId is null ? "" : " · con tabla de especificaciones.");
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

        await RefreshEvidenceAsync();
        if (Scores.Count == 0)
            MensajeEstado = "No hay nómina en el curso. Inscriba estudiantes para registrar puntajes.";
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

        await RefreshEvidenceAsync();
        MensajeEstado = _sync.PendingCount > 0
            ? "Puntajes y evidencia en el dispositivo. Se enviarán al reconectar."
            : NeedsReinforcement
                ? "Puntajes guardados. La lectura sugiere crear un refuerzo para este OA."
                : "Puntajes guardados. Evidencia registrada para el OA de la clase.";
    }

    [RelayCommand]
    private async Task VerEspecificacionesAsync()
    {
        if (Selected?.EducationalDocumentId is not Guid docId)
        {
            MensajeEstado = "Esta evaluación no tiene prueba generada con tabla de especificaciones.";
            return;
        }

        await Shell.Current.GoToAsync($"assessmentSpecification?documentId={docId}");
    }

    [RelayCommand]
    private async Task CrearRefuerzoAsync()
    {
        if (!Guid.TryParse(ClassId, out var id)) return;
        await Shell.Current.GoToAsync($"educationalDocumentGeneration?id={id}&type=Exercises&intent=reinforce");
    }

    [RelayCommand]
    private async Task VolverAClaseAsync()
        => await Shell.Current.GoToAsync("..");

    private async Task RefreshEvidenceAsync()
    {
        ClearEvidenceUi();
        if (Selected is null) return;
        try
        {
            Evidence = await _api.GetAssessmentEvidenceAsync(Selected.Id);
            if (Evidence is null) return;

            ReadingSummary = Evidence.ReadingSummary;
            LevelCounts =
                $"Logrado {Evidence.CountLogrado} · Medianamente {Evidence.CountMedianamente} · Por lograr {Evidence.CountPorLograr}";
            NeedsReinforcement = Evidence.NeedsReinforcement;
            if (!string.IsNullOrWhiteSpace(Evidence.ObjectiveCode))
                OaHeader = $"OA {Evidence.ObjectiveCode}: {Evidence.ObjectiveDescription}";

            foreach (var i in Evidence.Indicators)
                IndicatorLines.Add("• " + i);
            foreach (var s in Evidence.SpecificationTable)
                SpecRows.Add(s);
            HasSpecification = SpecRows.Count > 0 || Evidence.EducationalDocumentId is not null;
            foreach (var n in Evidence.StudentsNeedingSupport)
                SupportStudents.Add(n);
        }
        catch
        {
            // Offline / API antigua: la lectura detallada no está disponible.
        }
    }

    private void ClearEvidenceUi()
    {
        Evidence = null;
        ReadingSummary = string.Empty;
        LevelCounts = string.Empty;
        NeedsReinforcement = false;
        HasSpecification = false;
        IndicatorLines.Clear();
        SpecRows.Clear();
        SupportStudents.Clear();
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
