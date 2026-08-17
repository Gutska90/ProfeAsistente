using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Documents;

[QueryProperty(nameof(CourseId), "courseId")]
public partial class MaterialLibraryViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public MaterialLibraryViewModel(IApiClient api) => _api = api;

    public ObservableCollection<EducationalDocumentSummaryDto> Materiales { get; } = [];
    public ObservableCollection<string> FiltrosTipo { get; } = new(["Todos", "Guía", "Actividad", "Prueba", "Plantillas"]);

    [ObservableProperty] private string courseId = string.Empty;
    [ObservableProperty] private string filtroTipo = "Todos";
    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private string tituloPagina = "Biblioteca";
    [ObservableProperty] private bool filtroCursoActivo;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    partial void OnCourseIdChanged(string value)
    {
        FiltroCursoActivo = Guid.TryParse(value, out _);
        _ = CargarAsync();
    }

    partial void OnFiltroTipoChanged(string value) => _ = CargarAsync();

    [RelayCommand]
    public async Task CargarAsync()
    {
        try
        {
            IsBusy = true;
            Guid? course = Guid.TryParse(CourseId, out var cid) ? cid : null;
            FiltroCursoActivo = course is not null;
            var templatesOnly = string.Equals(FiltroTipo, "Plantillas", StringComparison.OrdinalIgnoreCase);
            TituloPagina = templatesOnly
                ? "Plantillas"
                : course is null ? "Biblioteca" : "Materiales del curso";
            var type = templatesOnly ? null : MaterialUiLabels.ParseTypeLabel(FiltroTipo);
            var typeParam = type?.ToString();
            Materiales.Clear();
            foreach (var m in await _api.GetMaterialLibraryAsync(course, typeParam, Busqueda, templatesOnly))
                Materiales.Add(m);
            MensajeEstado = Materiales.Count == 0
                ? templatesOnly
                    ? "Sin plantillas. Guarde una desde el editor (Guardar plantilla)."
                    : "Sin materiales aún. Prepárelos desde una clase (Asistente)."
                : $"{Materiales.Count} material(es).";
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
    private async Task BuscarAsync() => await CargarAsync();

    [RelayCommand]
    private async Task LimpiarFiltroCursoAsync()
    {
        CourseId = string.Empty;
        await CargarAsync();
    }

    [RelayCommand]
    private async Task UsarEnOtraClaseAsync(EducationalDocumentSummaryDto? doc)
    {
        if (doc is null) return;
        try
        {
            IsBusy = true;
            var targets = await _api.GetReuseTargetsAsync(doc.Id);
            if (targets.Count == 0)
            {
                MensajeEstado = "No hay otras clases disponibles para reutilizar este material.";
                return;
            }

            var labels = targets.Select(t => t.Label).ToArray();
            var choice = await Shell.Current.DisplayActionSheet(
                "Usar en otra clase", "Cancelar", null, labels);
            if (string.IsNullOrWhiteSpace(choice) || choice == "Cancelar") return;
            var target = targets.FirstOrDefault(t => t.Label == choice);
            if (target is null) return;

            var result = await _api.ReuseEducationalDocumentAsync(doc.Id, new ReuseEducationalDocumentRequest
            {
                TargetClassId = target.ClassId,
                SetAsCurrent = true
            });
            MensajeEstado = result.ObjectiveChanged
                ? $"Copiado. Revise OA ({result.SourceObjectiveCode} → {result.TargetObjectiveCode})."
                : "Material copiado a la clase destino.";
            await Shell.Current.GoToAsync(
                $"educationalDocumentEditor?documentId={result.DocumentId}&id={result.ClassId}");
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
    private async Task AbrirAsync(EducationalDocumentSummaryDto? doc)
    {
        if (doc is null) return;
        await Shell.Current.GoToAsync($"educationalDocumentEditor?documentId={doc.Id}&id={doc.ClassId}");
    }

    [RelayCommand]
    private async Task AbrirClaseAsync(EducationalDocumentSummaryDto? doc)
    {
        if (doc is null) return;
        await Shell.Current.GoToAsync($"claseDetalle?id={doc.ClassId}");
    }
}
