using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels;

[QueryProperty(nameof(GenerationIdText), "generationId")]
[QueryProperty(nameof(ClaseId), "id")]
public partial class ClassStructureEditorViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public ClassStructureEditorViewModel(IApiClient api) => _api = api;

    public ObservableCollection<string> Advertencias { get; } = [];

    [ObservableProperty] private string generationIdText = string.Empty;
    [ObservableProperty] private string claseId = string.Empty;
    [ObservableProperty] private Guid generationId;

    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string purpose = string.Empty;
    [ObservableProperty] private string status = string.Empty;
    [ObservableProperty] private bool isCurrentVersion;
    [ObservableProperty] private bool requiresReview;
    [ObservableProperty] private bool isOutdated;
    [ObservableProperty] private string curriculumRefs = string.Empty;

    [ObservableProperty] private int startDuration;
    [ObservableProperty] private string startObjective = string.Empty;
    [ObservableProperty] private string startTeacherActions = string.Empty;
    [ObservableProperty] private string startStudentActions = string.Empty;

    [ObservableProperty] private int developmentDuration;
    [ObservableProperty] private string developmentObjective = string.Empty;
    [ObservableProperty] private string developmentTeacherActions = string.Empty;
    [ObservableProperty] private string developmentStudentActions = string.Empty;

    [ObservableProperty] private int closureDuration;
    [ObservableProperty] private string closureObjective = string.Empty;
    [ObservableProperty] private string closureTeacherActions = string.Empty;
    [ObservableProperty] private string closureStudentActions = string.Empty;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string comparacionTexto = string.Empty;
    [ObservableProperty] private bool mostrarComparacion;

    partial void OnGenerationIdTextChanged(string value)
    {
        if (Guid.TryParse(value, out var id))
        {
            GenerationId = id;
            _ = CargarAsync();
        }
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (GenerationId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            MensajeEstado = "Cargando estructura...";
            var result = await _api.GetStructureGenerationAsync(GenerationId);
            if (result is null)
            {
                MensajeEstado = "Generación no encontrada.";
                return;
            }

            ApplyResult(result);
            MensajeEstado = $"Versión {result.GenerationNumber} · {result.Status}";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (GenerationId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            MensajeEstado = "Guardando...";
            var request = BuildUpdateRequest();
            var result = await _api.UpdateStructureContentAsync(GenerationId, request);
            ApplyResult(result);
            MensajeEstado = "Contenido guardado.";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EstablecerVigenteAsync()
    {
        if (GenerationId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            MensajeEstado = "Estableciendo como estructura vigente...";
            var result = await _api.SetCurrentStructureAsync(GenerationId);
            ApplyResult(result);
            MensajeEstado = "Esta versión es ahora la estructura vigente de la clase.";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CompararConAnteriorAsync()
    {
        if (string.IsNullOrWhiteSpace(ClaseId) || !Guid.TryParse(ClaseId, out var claseGuid))
        {
            MensajeEstado = "No hay clase asociada para comparar versiones.";
            return;
        }

        try
        {
            IsBusy = true;
            MensajeEstado = "Comparando con versión anterior...";
            var generations = await _api.GetStructureGenerationsAsync(claseGuid);
            var ordered = generations.OrderByDescending(g => g.GenerationNumber).ToList();
            var currentIdx = ordered.FindIndex(g => g.Id == GenerationId);
            if (currentIdx < 0 || currentIdx + 1 >= ordered.Count)
            {
                ComparacionTexto = "No hay una versión anterior para comparar.";
                MostrarComparacion = true;
                MensajeEstado = ComparacionTexto;
                return;
            }

            var previous = await _api.GetStructureGenerationAsync(ordered[currentIdx + 1].Id);
            var current = await _api.GetStructureGenerationAsync(GenerationId);
            if (previous?.Structure is null || current?.Structure is null)
            {
                ComparacionTexto = "No se pudo cargar el contenido de una de las versiones.";
                MostrarComparacion = true;
                return;
            }

            ComparacionTexto = BuildTextComparison(
                ordered[currentIdx + 1].GenerationNumber, previous.Structure,
                ordered[currentIdx].GenerationNumber, current.Structure);
            MostrarComparacion = true;
            MensajeEstado = "Comparación lista (versión anterior vs actual).";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GenerarOtraVersionAsync()
    {
        if (!string.IsNullOrWhiteSpace(ClaseId))
        {
            await Shell.Current.GoToAsync($"classStructureGeneration?id={ClaseId}");
            return;
        }

        if (GenerationId == Guid.Empty) return;

        try
        {
            IsBusy = true;
            MensajeEstado = "Generando otra versión...";
            var result = await _api.RetryStructureGenerationAsync(GenerationId);
            await Shell.Current.GoToAsync(
                $"classStructureEditor?generationId={result.GenerationId}&id={result.ClassId}");
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyResult(ClassStructureGenerationResultDto result)
    {
        GenerationId = result.GenerationId;
        GenerationIdText = result.GenerationId.ToString();
        if (result.ClassId != Guid.Empty)
            ClaseId = result.ClassId.ToString();

        Status = result.Status;
        IsCurrentVersion = result.IsCurrentVersion;
        RequiresReview = result.RequiresReview;
        IsOutdated = result.IsOutdated;

        Advertencias.Clear();
        foreach (var w in result.Warnings)
            Advertencias.Add(w);

        if (result.Curriculum is not null)
        {
            CurriculumRefs =
                $"OA {result.Curriculum.ObjectiveCode} · release {result.Curriculum.CurriculumRelease}";
        }

        var s = result.Structure;
        if (s is null) return;

        Title = s.Title;
        Purpose = s.Purpose;
        BindPhase(s.Start, d => StartDuration = d, o => StartObjective = o,
            t => StartTeacherActions = t, st => StartStudentActions = st);
        BindPhase(s.Development, d => DevelopmentDuration = d, o => DevelopmentObjective = o,
            t => DevelopmentTeacherActions = t, st => DevelopmentStudentActions = st);
        BindPhase(s.Closure, d => ClosureDuration = d, o => ClosureObjective = o,
            t => ClosureTeacherActions = t, st => ClosureStudentActions = st);
    }

    private UpdateClassStructureContentRequest BuildUpdateRequest() => new()
    {
        Title = Title,
        Purpose = Purpose,
        Start = BuildPhase(StartDuration, StartObjective, StartTeacherActions, StartStudentActions),
        Development = BuildPhase(DevelopmentDuration, DevelopmentObjective, DevelopmentTeacherActions, DevelopmentStudentActions),
        Closure = BuildPhase(ClosureDuration, ClosureObjective, ClosureTeacherActions, ClosureStudentActions),
        ChangeSummary = "Edición manual desde MAUI"
    };

    private static void BindPhase(
        ClassPhaseDto phase,
        Action<int> setDuration,
        Action<string> setObjective,
        Action<string> setTeacher,
        Action<string> setStudent)
    {
        setDuration(phase.DurationMinutes);
        setObjective(phase.Objective ?? string.Empty);
        setTeacher(JoinLines(phase.TeacherActions));
        setStudent(JoinLines(phase.StudentActions));
    }

    private static ClassPhaseDto BuildPhase(int duration, string objective, string teacher, string student) => new()
    {
        DurationMinutes = duration,
        Objective = objective ?? string.Empty,
        TeacherActions = SplitLines(teacher),
        StudentActions = SplitLines(student)
    };

    private static string JoinLines(IEnumerable<string>? items) =>
        items is null ? string.Empty : string.Join("\n", items.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static List<string> SplitLines(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

    private static string BuildTextComparison(
        int prevNumber, ClassStructureContentDto prev,
        int currNumber, ClassStructureContentDto curr)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Comparación v{prevNumber} → v{currNumber}");
        AppendDiff(sb, "Título", prev.Title, curr.Title);
        AppendDiff(sb, "Propósito", prev.Purpose, curr.Purpose);
        AppendDiff(sb, "Inicio", SummarizePhase(prev.Start), SummarizePhase(curr.Start));
        AppendDiff(sb, "Desarrollo", SummarizePhase(prev.Development), SummarizePhase(curr.Development));
        AppendDiff(sb, "Cierre", SummarizePhase(prev.Closure), SummarizePhase(curr.Closure));
        AppendDiff(sb, "Duración total",
            prev.TotalDurationMinutes.ToString(),
            curr.TotalDurationMinutes.ToString());
        AppendDiff(sb, "Evaluación formativa",
            prev.FormativeAssessment?.Strategy ?? "",
            curr.FormativeAssessment?.Strategy ?? "");
        AppendDiff(sb, "Diferenciación",
            string.Join("; ", prev.Differentiation?.SupportActions ?? []),
            string.Join("; ", curr.Differentiation?.SupportActions ?? []));
        return sb.ToString().Trim();
    }

    private static void AppendDiff(System.Text.StringBuilder sb, string label, string before, string after)
    {
        if (string.Equals(before?.Trim(), after?.Trim(), StringComparison.Ordinal))
        {
            sb.AppendLine($"• {label}: sin cambios");
            return;
        }

        sb.AppendLine($"• {label}:");
        sb.AppendLine($"    − {Truncate(before)}");
        sb.AppendLine($"    + {Truncate(after)}");
    }

    private static string SummarizePhase(ClassPhaseDto phase) =>
        $"{phase.DurationMinutes} min · {phase.Objective}";

    private static string Truncate(string? text, int max = 180)
    {
        text ??= string.Empty;
        text = text.Replace('\n', ' ').Trim();
        return text.Length <= max ? text : text[..max] + "…";
    }
}
