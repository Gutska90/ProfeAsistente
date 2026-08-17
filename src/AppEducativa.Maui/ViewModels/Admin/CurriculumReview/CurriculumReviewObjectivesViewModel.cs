using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Admin.CurriculumReview;

[QueryProperty(nameof(BatchIdText), "id")]
public partial class CurriculumReviewObjectivesViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private List<ReviewObjectiveDto> _all = [];

    public CurriculumReviewObjectivesViewModel(IApiClient api) => _api = api;

    public ObservableCollection<ObjectiveListItem> Objetivos { get; } = [];
    public IReadOnlyList<string> Filtros { get; } =
        ["Todos", "Pendientes", "Corregidos", "Con errores", "Modificados"];

    [ObservableProperty] private string batchIdText = "";
    [ObservableProperty] private Guid batchId;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string filtroSeleccionado = "Todos";
    [ObservableProperty] private string busqueda = "";

    partial void OnBatchIdTextChanged(string value)
    {
        if (Guid.TryParse(value, out var id))
            BatchId = id;
    }

    partial void OnFiltroSeleccionadoChanged(string value) => ApplyFilter();
    partial void OnBusquedaChanged(string value) => ApplyFilter();

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (BatchId == Guid.Empty) return;
        try
        {
            IsBusy = true;
            var package = await _api.GetCurriculumReviewAsync(BatchId);
            _all = package?.Objectives.Where(o => !o.IsDeleted).ToList() ?? [];
            ApplyFilter();
            MensajeEstado = $"{_all.Count} OA en el lote";
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    private void ApplyFilter()
    {
        IEnumerable<ReviewObjectiveDto> q = _all;
        q = FiltroSeleccionado switch
        {
            "Pendientes" => q.Where(o => string.Equals(o.Decision, "Pending", StringComparison.OrdinalIgnoreCase)),
            "Corregidos" => q.Where(o => string.Equals(o.Decision, "Corrected", StringComparison.OrdinalIgnoreCase)),
            "Con errores" => q.Where(o => o.IssueCount > 0 || o.Issues.Count > 0),
            "Modificados" => q.Where(o => o.WasManuallyModified),
            _ => q
        };

        if (!string.IsNullOrWhiteSpace(Busqueda))
        {
            var term = Busqueda.Trim();
            q = q.Where(o =>
                (o.Code?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || o.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
                || o.ExtractedCode.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        Objetivos.Clear();
        foreach (var o in q.OrderBy(x => x.Code ?? x.ExtractedCode))
            Objetivos.Add(ObjectiveListItem.From(o));
    }

    [RelayCommand]
    private async Task AbrirDetalleAsync(ObjectiveListItem? item)
    {
        if (item is null || BatchId == Guid.Empty) return;
        await Shell.Current.GoToAsync(
            $"adminReviewObjectiveDetail?id={BatchId}&objectiveTemporaryId={Uri.EscapeDataString(item.TemporaryId)}");
    }
}

public sealed class ObjectiveListItem
{
    public string TemporaryId { get; init; } = "";
    public string DecisionText { get; init; } = "";
    public string ModifiedBadge { get; init; } = "";
    public string Code { get; init; } = "";
    public string ShortDescription { get; init; } = "";
    public string PageText { get; init; } = "";

    public static ObjectiveListItem From(ReviewObjectiveDto o)
    {
        var desc = o.Description;
        if (desc.Length > 120) desc = desc[..117] + "…";
        return new ObjectiveListItem
        {
            TemporaryId = o.TemporaryId,
            DecisionText = o.Decision,
            ModifiedBadge = o.WasManuallyModified ? "Modificado" : "",
            Code = string.IsNullOrWhiteSpace(o.Code) ? o.ExtractedCode : o.Code!,
            ShortDescription = desc,
            PageText = o.PageStart is null ? "" : $"p. {o.PageStart}" + (o.PageEnd is null || o.PageEnd == o.PageStart ? "" : $"-{o.PageEnd}")
        };
    }
}
