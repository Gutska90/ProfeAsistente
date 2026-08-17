using System.Collections.ObjectModel;
using System.Text.Json;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Admin;

[QueryProperty(nameof(BatchIdText), "id")]
public partial class CurriculumImportPreviewViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public CurriculumImportPreviewViewModel(IApiClient api) => _api = api;

    public ObservableCollection<CurriculumUnitPreviewDto> Unidades { get; } = [];
    public ObservableCollection<CurriculumObjectivePreviewDto> Objetivos { get; } = [];
    public ObservableCollection<CurriculumIndicatorPreviewDto> Indicadores { get; } = [];

    [ObservableProperty] private string batchIdText = "";
    [ObservableProperty] private Guid batchId;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string estado = "";
    [ObservableProperty] private string habilidades = "";
    [ObservableProperty] private string actitudes = "";
    [ObservableProperty] private string previewJson = "";

    private CurriculumImportPreviewDto? _preview;

    partial void OnBatchIdTextChanged(string value)
    {
        if (Guid.TryParse(value, out var id))
            BatchId = id;
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (BatchId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            _preview = await _api.GetCurriculumImportPreviewAsync(BatchId);
            if (_preview is null)
            {
                MensajeEstado = "Sin vista previa. Ejecute extract/validate primero.";
                return;
            }

            Estado = _preview.Status;
            Unidades.Clear();
            foreach (var u in _preview.Units) Unidades.Add(u);
            Objetivos.Clear();
            foreach (var o in _preview.Objectives) Objetivos.Add(o);
            Indicadores.Clear();
            foreach (var i in _preview.Indicators) Indicadores.Add(i);
            Habilidades = string.Join("\n", _preview.Skills);
            Actitudes = string.Join("\n", _preview.Attitudes);
            PreviewJson = JsonSerializer.Serialize(_preview, JsonOptions);
            MensajeEstado = $"{Objetivos.Count} OA · {Indicadores.Count} indicadores · confianza {_preview.ConfianzaPromedio:0.00}";
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
    private async Task GuardarCorreccionesAsync()
    {
        if (_preview is null) return;
        try
        {
            IsBusy = true;
            _preview.Skills = Habilidades.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            _preview.Attitudes = Actitudes.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            _preview.Units = Unidades.ToList();
            _preview.Objectives = Objetivos.ToList();
            _preview.Indicators = Indicadores.ToList();
            _preview = await _api.UpdateCurriculumImportPreviewAsync(BatchId, _preview);
            PreviewJson = JsonSerializer.Serialize(_preview, JsonOptions);
            MensajeEstado = "Correcciones guardadas (auditoría en CurriculumReviewChanges).";
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
}
