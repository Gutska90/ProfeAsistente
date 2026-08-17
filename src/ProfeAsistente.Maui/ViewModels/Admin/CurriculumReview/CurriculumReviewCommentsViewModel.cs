using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Admin.CurriculumReview;

[QueryProperty(nameof(BatchIdText), "id")]
public partial class CurriculumReviewCommentsViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public CurriculumReviewCommentsViewModel(IApiClient api) => _api = api;

    public ObservableCollection<ReviewCommentDto> Comments { get; } = [];

    [ObservableProperty] private string batchIdText = "";
    [ObservableProperty] private Guid batchId;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string nuevoComentario = "";

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
            Comments.Clear();
            foreach (var c in await _api.GetCurriculumReviewCommentsAsync(BatchId))
                Comments.Add(c);
            MensajeEstado = $"{Comments.Count} comentarios · {Comments.Count(c => !c.IsResolved)} abiertos";
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AgregarAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoComentario)) return;
        try
        {
            IsBusy = true;
            await _api.AddCurriculumReviewCommentAsync(BatchId, new AddReviewCommentRequest
            {
                Message = NuevoComentario.Trim(),
                Severity = CurriculumCommentSeverity.Info
            });
            NuevoComentario = "";
            await CargarAsync();
            MensajeEstado = "Comentario agregado.";
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ResolverAsync(ReviewCommentDto? comment)
    {
        if (comment is null || comment.IsResolved) return;
        try
        {
            IsBusy = true;
            await _api.ResolveCurriculumReviewCommentAsync(BatchId, comment.Id);
            await CargarAsync();
        }
        catch (Exception ex) { MensajeEstado = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}
