using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using ProfeAsistente.Shared.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Documents;

[QueryProperty(nameof(ClaseId), "id")]
[QueryProperty(nameof(TypeName), "type")]
[QueryProperty(nameof(Intent), "intent")]
public partial class EducationalDocumentGenerationViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private CancellationTokenSource? _cts;
    private bool _suspendTypeSync;

    public EducationalDocumentGenerationViewModel(IApiClient api) => _api = api;

    public ObservableCollection<string> Dificultades { get; } = new(["Básica", "Intermedia", "Avanzada"]);
    public ObservableCollection<string> TiposDocumento { get; } = new(["Guía", "Actividad", "Prueba"]);

    [ObservableProperty] private string claseId = string.Empty;
    [ObservableProperty] private string typeName = "Assessment";
    [ObservableProperty] private string intent = string.Empty;
    [ObservableProperty] private string documentType = "Assessment";
    [ObservableProperty] private string tipoSeleccionado = "Prueba";
    [ObservableProperty] private string difficulty = "Intermedia";
    [ObservableProperty] private int itemCount = 10;
    [ObservableProperty] private int estimatedDurationMinutes = 60;
    [ObservableProperty] private bool includeAnswerKey = true;
    [ObservableProperty] private bool includeFeedback = true;
    [ObservableProperty] private bool includeScoring = true;
    [ObservableProperty] private string? teacherInstructions;
    [ObservableProperty] private string contextoCurricular = string.Empty;
    [ObservableProperty] private bool canGenerate = true;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    partial void OnClaseIdChanged(string value) => _ = CargarContextoAsync();

    partial void OnTypeNameChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        DocumentType = value;
        SyncTipoDesdeDocumentType();
    }

    partial void OnIntentChanged(string value) => ApplyIntentPresets(value);

    partial void OnDocumentTypeChanged(string value) => SyncTipoDesdeDocumentType();

    partial void OnTipoSeleccionadoChanged(string value)
    {
        if (_suspendTypeSync) return;
        var parsed = MaterialUiLabels.ParseTypeLabel(value);
        if (parsed is EducationalDocumentType t)
            DocumentType = t.ToString();
    }

    private void SyncTipoDesdeDocumentType()
    {
        _suspendTypeSync = true;
        TipoSeleccionado = MaterialUiLabels.Type(DocumentType);
        _suspendTypeSync = false;
    }

    private void ApplyIntentPresets(string? intent)
    {
        switch (intent?.Trim().ToLowerInvariant())
        {
            case "exitticket":
                DocumentType = "Assessment";
                ItemCount = 3;
                EstimatedDurationMinutes = 10;
                IncludeAnswerKey = true;
                IncludeFeedback = true;
                IncludeScoring = false;
                Difficulty = "Básica";
                TeacherInstructions =
                    "Ticket de salida breve (cierre de clase). 2–3 ítems cortos alineados al OA e indicadores. " +
                    "Formativo, sin ponderación. Lenguaje claro para los últimos minutos de la sesión.";
                break;
            case "simplify":
                DocumentType = "LearningGuide";
                ItemCount = 5;
                EstimatedDurationMinutes = 30;
                IncludeAnswerKey = true;
                IncludeFeedback = true;
                IncludeScoring = false;
                Difficulty = "Básica";
                TeacherInstructions =
                    "Versión simplificada (DUA / Decreto 83): instrucciones cortas, un paso a la vez, " +
                    "vocabulario accesible y ejemplos concretos. Mantener alineación estricta al OA.";
                break;
            case "scaffold":
                DocumentType = "Exercises";
                ItemCount = 6;
                EstimatedDurationMinutes = 40;
                IncludeAnswerKey = true;
                IncludeFeedback = true;
                IncludeScoring = false;
                Difficulty = "Básica";
                TeacherInstructions =
                    "Versión con mayor andamiaje: modelado inicial, pistas graduadas, banco de palabras o " +
                    "ejemplos resueltos parciales. Aumentar apoyo sin cambiar el OA ni inventar indicadores.";
                break;
            case "reinforce":
                DocumentType = "Exercises";
                ItemCount = 6;
                EstimatedDurationMinutes = 35;
                IncludeAnswerKey = true;
                IncludeFeedback = true;
                IncludeScoring = false;
                Difficulty = "Básica";
                TeacherInstructions =
                    "Refuerzo post-evaluación alineado estrictamente al OA e indicadores de la clase. " +
                    "Priorizar ítems formativos cortos para estudiantes en nivel «por lograr» o «medianamente logrado». " +
                    "Incluir modelado, un ejemplo resuelto y práctica graduada. No inventar OA ni indicadores nuevos.";
                break;
        }

        SyncTipoDesdeDocumentType();
    }

    private static ItemDifficulty ParseDifficulty(string? label) => label?.Trim().ToLowerInvariant() switch
    {
        "básica" or "basica" or "basic" => ItemDifficulty.Basic,
        "avanzada" or "advanced" => ItemDifficulty.Advanced,
        _ => ItemDifficulty.Intermediate
    };

    [RelayCommand]
    public async Task CargarContextoAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        try
        {
            SyncTipoDesdeDocumentType();
            var clase = await _api.GetClaseAsync(id);
            var structure = await _api.GetCurrentStructureAsync(id);
            if (clase is null)
            {
                CanGenerate = false;
                MensajeEstado = "Clase no encontrada.";
                return;
            }

            ContextoCurricular =
                $"{clase.Nivel} · {clase.Asignatura}\n" +
                $"Unidad: {clase.Unidad}\n" +
                $"OA {clase.ObjetivoCodigo}: {clase.ObjetivoDescripcion}\n" +
                $"Bloom: {clase.NivelBloom}\n" +
                $"Indicadores: {clase.Indicadores.Count}\n" +
                $"Estructura vigente: {(structure is null ? "no" : $"v{structure.GenerationNumber}")}";
            CanGenerate = clase.IndicadorEvaluacionIds.Count > 0 || clase.Indicadores.Count > 0;
            MensajeEstado = CanGenerate
                ? "Listo para generar."
                : "Seleccione indicadores en la clase antes de generar.";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error: {ex.Message}";
            CanGenerate = false;
        }
    }

    [RelayCommand]
    private async Task GenerarAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id) || !CanGenerate) return;
        if (!Enum.TryParse<EducationalDocumentType>(DocumentType, true, out var type))
        {
            MensajeEstado = "Tipo de material inválido.";
            return;
        }

        var difficulty = ParseDifficulty(Difficulty);

        try
        {
            IsBusy = true;
            _cts = new CancellationTokenSource();
            MensajeEstado = "Generando material…";
            var clase = await _api.GetClaseAsync(id);
            var result = await _api.GenerateEducationalDocumentAsync(id, new GenerateEducationalDocumentRequest
            {
                DocumentType = type,
                ItemCount = Math.Clamp(ItemCount, 1, 50),
                EvaluationIndicatorIds = clase?.IndicadorEvaluacionIds ?? [],
                Difficulty = difficulty,
                EstimatedDurationMinutes = EstimatedDurationMinutes,
                IncludeAnswerKey = IncludeAnswerKey,
                IncludeFeedback = IncludeFeedback,
                IncludeScoring = IncludeScoring,
                TeacherInstructions = TeacherInstructions
            }, _cts.Token);

            MensajeEstado = "Material generado. Abriendo editor…";
            await Shell.Current.GoToAsync(
                $"educationalDocumentEditor?documentId={result.DocumentId}&id={id}");
        }
        catch (OperationCanceledException)
        {
            MensajeEstado = "Generación cancelada.";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void Cancelar()
    {
        _cts?.Cancel();
        MensajeEstado = "Cancelando…";
    }
}
