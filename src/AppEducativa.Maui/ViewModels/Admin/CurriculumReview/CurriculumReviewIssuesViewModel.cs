using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Admin.CurriculumReview;

[QueryProperty(nameof(BatchIdText), "id")]
public partial class CurriculumReviewIssuesViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public CurriculumReviewIssuesViewModel(IApiClient api) => _api = api;

    public ObservableCollection<IssueListItem> Issues { get; } = [];

    [ObservableProperty] private string batchIdText = "";
    [ObservableProperty] private Guid batchId;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

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
            Issues.Clear();
            var package = await _api.GetCurriculumReviewAsync(BatchId);
            if (package is not null)
            {
                foreach (var o in package.Objectives.Where(x => !x.IsDeleted))
                {
                    foreach (var issue in o.Issues)
                        Issues.Add(IssueListItem.From("OA", o.Code ?? o.ExtractedCode, issue));
                }

                foreach (var i in package.Indicators.Where(x => !x.IsDeleted))
                {
                    foreach (var issue in i.Issues)
                        Issues.Add(IssueListItem.From("Indicador", i.Code ?? i.TemporaryId, issue));
                }
            }

            foreach (var issue in await _api.GetCurriculumImportIssuesAsync(BatchId))
            {
                Issues.Add(new IssueListItem
                {
                    Scope = "Lote",
                    Entity = "—",
                    Severity = issue.Severity + (issue.Blocking ? " (bloqueante)" : ""),
                    Message = issue.Message,
                    FieldName = ""
                });
            }

            MensajeEstado = $"{Issues.Count} problemas";
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}

public sealed class IssueListItem
{
    public string Scope { get; init; } = "";
    public string Entity { get; init; } = "";
    public string Severity { get; init; } = "";
    public string Message { get; init; } = "";
    public string FieldName { get; init; } = "";

    public static IssueListItem From(string scope, string entity, ReviewFieldIssueDto issue) => new()
    {
        Scope = scope,
        Entity = entity,
        Severity = issue.Severity,
        Message = issue.Message,
        FieldName = issue.FieldName ?? ""
    };
}
