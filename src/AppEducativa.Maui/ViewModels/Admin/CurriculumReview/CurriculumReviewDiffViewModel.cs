using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels.Admin.CurriculumReview;

[QueryProperty(nameof(BatchIdText), "id")]
public partial class CurriculumReviewDiffViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public CurriculumReviewDiffViewModel(IApiClient api) => _api = api;

    public ObservableCollection<DiffListItem> Items { get; } = [];

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
            Items.Clear();
            var diff = await _api.GetCurriculumReviewDiffAsync(BatchId);
            if (diff is null)
            {
                MensajeEstado = "Sin diff de revisión.";
                return;
            }

            foreach (var item in diff.Items)
            {
                var fields = string.Join("; ", item.Fields.Select(f =>
                    $"{f.Field}: '{Truncate(f.OldValue)}' → '{Truncate(f.NewValue)}' ({f.Significance})"));
                Items.Add(new DiffListItem
                {
                    EntityType = item.EntityType,
                    Code = item.Code ?? item.TemporaryId,
                    ChangeType = item.ChangeType,
                    FieldsSummary = string.IsNullOrWhiteSpace(fields) ? "(sin campos)" : fields
                });
            }

            MensajeEstado = $"{Items.Count} diferencias · {diff.GeneratedAt.ToLocalTime():g}";
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    private static string Truncate(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= 40 ? s : s[..37] + "…";
    }
}

public sealed class DiffListItem
{
    public string EntityType { get; init; } = "";
    public string Code { get; init; } = "";
    public string ChangeType { get; init; } = "";
    public string FieldsSummary { get; init; } = "";
}
