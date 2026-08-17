using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Documents;

[QueryProperty(nameof(ClaseId), "id")]
[QueryProperty(nameof(TypeName), "type")]
public partial class EducationalDocumentGenerationViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private CancellationTokenSource? _cts;

    public EducationalDocumentGenerationViewModel(IApiClient api) => _api = api;

    public ObservableCollection<string> Dificultades { get; } = new(["Basic", "Intermediate", "Advanced"]);
    public ObservableCollection<string> TiposDocumento { get; } =
        new(["LearningGuide", "Exercises", "Assessment"]);

    [ObservableProperty] private string claseId = string.Empty;
    [ObservableProperty] private string typeName = "Assessment";
    [ObservableProperty] private string documentType = "Assessment";
    [ObservableProperty] private string difficulty = "Intermediate";
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
        if (!string.IsNullOrWhiteSpace(value))
            DocumentType = value;
    }

    [RelayCommand]
    public async Task CargarContextoAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        try
        {
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
                : "Selecciona indicadores en la clase antes de generar.";
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
            MensajeEstado = "Tipo de documento inválido.";
            return;
        }

        if (!Enum.TryParse<ItemDifficulty>(Difficulty, true, out var difficulty))
            difficulty = ItemDifficulty.Intermediate;

        try
        {
            IsBusy = true;
            _cts = new CancellationTokenSource();
            MensajeEstado = "Generando material con IA…";
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
            MensajeEstado = "Generación cancelada en el cliente.";
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
