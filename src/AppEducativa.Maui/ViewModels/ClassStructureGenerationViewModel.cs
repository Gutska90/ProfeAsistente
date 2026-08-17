using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels;

[QueryProperty(nameof(ClaseId), "id")]
public partial class ClassStructureGenerationViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private CancellationTokenSource? _generateCts;

    public ClassStructureGenerationViewModel(IApiClient api) => _api = api;

    public ObservableCollection<string> Indicadores { get; } = [];
    public ObservableCollection<string> Habilidades { get; } = [];
    public ObservableCollection<string> Actitudes { get; } = [];
    public ObservableCollection<string> ObjetivosTransversales { get; } = [];
    public ObservableCollection<string> Advertencias { get; } = [];

    [ObservableProperty] private string claseId = string.Empty;
    [ObservableProperty] private ClassGenerationContextDto? contexto;
    [ObservableProperty] private string contextoResumen = string.Empty;
    [ObservableProperty] private string oaTexto = string.Empty;
    [ObservableProperty] private string bloomTexto = string.Empty;
    [ObservableProperty] private string versionCurricular = string.Empty;

    [ObservableProperty] private int durationMinutes = 90;
    [ObservableProperty] private string? previousKnowledge;
    [ObservableProperty] private string? availableResources;
    [ObservableProperty] private string? studentContext;
    [ObservableProperty] private string? teacherInstructions;
    [ObservableProperty] private bool includeFormativeAssessment = true;
    [ObservableProperty] private bool includeDifferentiation = true;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isGenerating;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string? errorMensaje;

    partial void OnClaseIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = CargarAsync();
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        try
        {
            IsBusy = true;
            ErrorMensaje = null;
            MensajeEstado = "Cargando contexto curricular...";

            Contexto = await _api.GetGenerationContextAsync(id);
            if (Contexto is null)
            {
                // Fallback: armar contexto básico desde la clase
                var clase = await _api.GetClaseAsync(id);
                if (clase is null)
                {
                    ErrorMensaje = "No se pudo cargar la clase.";
                    MensajeEstado = null;
                    return;
                }

                Contexto = new ClassGenerationContextDto
                {
                    Level = clase.Nivel,
                    Subject = clase.Asignatura,
                    Unit = clase.Unidad,
                    ObjectiveCode = clase.ObjetivoCodigo,
                    ObjectiveDescription = clase.ObjetivoDescripcion,
                    Indicators = clase.Indicadores,
                    BloomLevel = clase.NivelBloom,
                    DurationMinutes = 90
                };
            }

            DurationMinutes = Contexto.DurationMinutes > 0 ? Contexto.DurationMinutes : 90;
            ContextoResumen = $"{Contexto.Level} · {Contexto.Subject} · {Contexto.Unit}";
            OaTexto = string.IsNullOrWhiteSpace(Contexto.ObjectiveCode)
                ? Contexto.ObjectiveDescription
                : $"{Contexto.ObjectiveCode}: {Contexto.ObjectiveDescription}";
            BloomTexto = Contexto.BloomLevel;
            VersionCurricular = Contexto.CurriculumRelease;

            Fill(Indicadores, Contexto.Indicators);
            Fill(Habilidades, Contexto.Skills);
            Fill(Actitudes, Contexto.Attitudes);
            Fill(ObjetivosTransversales, Contexto.TransversalObjectives);

            MensajeEstado = "Configura la generación y pulsa Generar estructura.";
        }
        catch (Exception ex)
        {
            ErrorMensaje = ex.Message;
            MensajeEstado = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GenerarAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        if (IsGenerating) return;

        _generateCts?.Cancel();
        _generateCts = new CancellationTokenSource();
        var ct = _generateCts.Token;

        try
        {
            IsGenerating = true;
            IsBusy = true;
            ErrorMensaje = null;
            Advertencias.Clear();
            MensajeEstado = "Generando estructura con IA...";

            var request = new GenerateClassStructureRequest
            {
                DurationMinutes = DurationMinutes > 0 ? DurationMinutes : 90,
                PreviousKnowledge = PreviousKnowledge,
                AvailableResources = AvailableResources,
                StudentContext = StudentContext,
                TeacherInstructions = TeacherInstructions,
                IncludeFormativeAssessment = IncludeFormativeAssessment,
                IncludeDifferentiation = IncludeDifferentiation
            };

            var result = await _api.GenerateClassStructureAsync(id, request, ct);

            foreach (var w in result.Warnings)
                Advertencias.Add(w);

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                ErrorMensaje = $"{result.ErrorCode}: {result.ErrorMessage}";
                MensajeEstado = "La generación falló.";
                return;
            }

            if (result.Structure is null)
            {
                ErrorMensaje = "La API no devolvió una estructura.";
                MensajeEstado = "Revisa el error e intenta de nuevo.";
                return;
            }

            MensajeEstado = "Estructura generada. Abriendo editor...";
            await Shell.Current.GoToAsync(
                $"classStructureEditor?generationId={result.GenerationId}&id={id}");
        }
        catch (OperationCanceledException)
        {
            MensajeEstado = "Generación cancelada.";
        }
        catch (Exception ex)
        {
            ErrorMensaje = ex.Message;
            MensajeEstado = "Error al generar.";
        }
        finally
        {
            IsGenerating = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancelar()
    {
        _generateCts?.Cancel();
        MensajeEstado = "Cancelando...";
    }

    private static void Fill(ObservableCollection<string> target, IEnumerable<string>? items)
    {
        target.Clear();
        if (items is null) return;
        foreach (var item in items.Where(s => !string.IsNullOrWhiteSpace(s)))
            target.Add(item);
    }
}
