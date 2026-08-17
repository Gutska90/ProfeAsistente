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
    public ObservableCollection<string> FiltrosTipo { get; } = new(["Todos", "Guía", "Actividad", "Prueba"]);

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
            TituloPagina = course is null ? "Biblioteca" : "Materiales del curso";
            var type = MaterialUiLabels.ParseTypeLabel(FiltroTipo);
            var typeParam = type?.ToString();
            Materiales.Clear();
            foreach (var m in await _api.GetMaterialLibraryAsync(course, typeParam, Busqueda))
                Materiales.Add(m);
            MensajeEstado = Materiales.Count == 0
                ? "Sin materiales aún. Prepárelos desde una clase (Asistente)."
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
