using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels.Planning;

[QueryProperty(nameof(PlanningId), "planningId")]
public partial class PlanningSequenceGeneratorViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public PlanningSequenceGeneratorViewModel(IApiClient api) => _api = api;

    public ObservableCollection<PlanningSequenceProposalItemDto> Items { get; } = [];
    public ObservableCollection<string> Warnings { get; } = [];

    [ObservableProperty] private string planningId = string.Empty;
    [ObservableProperty] private int step = 1;
    [ObservableProperty] private bool includeDiagnostic = true;
    [ObservableProperty] private bool includeReview = true;
    [ObservableProperty] private bool includeAssessment = true;
    [ObservableProperty] private string? deficitText;
    [ObservableProperty] private Guid? proposalId;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    partial void OnPlanningIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = CargarAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (!Guid.TryParse(PlanningId, out var id)) return;
        try
        {
            var current = await _api.GetCurrentSequenceProposalAsync(id);
            if (current is null) return;
            ProposalId = current.Id;
            BindProposal(current);
            Step = 4;
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
    }

    [RelayCommand]
    private void NextStep() => Step = Math.Min(5, Step + 1);

    [RelayCommand]
    private void PrevStep() => Step = Math.Max(1, Step - 1);

    [RelayCommand]
    private async Task GenerarPropuestaAsync()
    {
        if (!Guid.TryParse(PlanningId, out var id)) return;
        try
        {
            IsBusy = true;
            MensajeEstado = "Generando secuencia determinista…";
            var plan = await _api.GetPlanificacionAsync(id)
                       ?? throw new InvalidOperationException("Planificación no encontrada.");
            var objetivos = await _api.GetObjetivosAsync(plan.UnidadId);
            if (objetivos.Count == 0)
                throw new InvalidOperationException("La unidad no tiene OA publicados.");

            var request = new GeneratePlanningSequenceRequest
            {
                Objectives = objetivos.Select((o, i) => new ObjectiveCoverageRequest
                {
                    ObjectiveId = o.Id,
                    MinimumSessions = 1,
                    Priority = i + 1
                }).ToList(),
                IncludeDiagnosticClass = IncludeDiagnostic,
                IncludeReviewClasses = IncludeReview,
                IncludeAssessmentClass = IncludeAssessment,
                BloomProgression = new BloomProgressionSettingsRequest
                {
                    InitialLevel = NivelBloom.Recordar,
                    TargetLevel = NivelBloom.Aplicar
                }
            };

            var proposal = await _api.GenerateSequenceProposalAsync(id, request);
            ProposalId = proposal.Id == Guid.Empty ? null : proposal.Id;
            BindProposal(proposal);
            Step = proposal.Deficit is null ? 4 : 3;
            MensajeEstado = proposal.Deficit is null
                ? $"{proposal.Items.Count} clases propuestas."
                : $"Déficit: {proposal.Deficit.Deficit} sesiones.";
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmarAsync()
    {
        if (ProposalId is null) return;
        try
        {
            IsBusy = true;
            await _api.ConfirmSequenceProposalAsync(ProposalId.Value);
            MensajeEstado = "Secuencia confirmada. Clases creadas.";
            Step = 5;
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task IrCoberturaAsync()
        => await Shell.Current.GoToAsync($"planningCoverage?planningId={PlanningId}");

    private void BindProposal(PlanningSequenceProposalDto proposal)
    {
        Items.Clear();
        foreach (var i in proposal.Items.OrderBy(x => x.Order))
            Items.Add(i);
        Warnings.Clear();
        foreach (var w in proposal.Warnings)
            Warnings.Add(w);
        DeficitText = proposal.Deficit is null
            ? null
            : $"Disponibles {proposal.Deficit.AvailableSessions}, mínimas {proposal.Deficit.RequiredMinimumSessions}, déficit {proposal.Deficit.Deficit}.";
    }
}
