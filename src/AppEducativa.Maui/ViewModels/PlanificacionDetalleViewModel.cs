using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels;

[QueryProperty(nameof(PlanificacionId), "id")]
public partial class PlanificacionDetalleViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly IOfflineSyncService _sync;

    public PlanificacionDetalleViewModel(IApiClient api, IOfflineSyncService sync)
    {
        _api = api;
        _sync = sync;
    }

    public ObservableCollection<ClaseFilaViewModel> Clases { get; } = [];

    [ObservableProperty] private string planificacionId = string.Empty;
    [ObservableProperty] private PlanificacionDetalleDto? planificacion;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    partial void OnPlanificacionIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = CargarAsync();
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (!Guid.TryParse(PlanificacionId, out var id)) return;
        try
        {
            IsBusy = true;
            Planificacion = await _sync.GetPlanificacionAsync(id);
            Clases.Clear();
            if (Planificacion is null)
            {
                MensajeEstado = "Planificación no encontrada.";
                return;
            }

            foreach (var c in Planificacion.Clases.OrderBy(x => x.Numero))
                Clases.Add(ClaseFilaViewModel.From(c));
            MensajeEstado = $"{Clases.Count} clase(s) · {Planificacion.Unidad}";
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
    private async Task AgregarClaseAsync()
    {
        if (Planificacion is null) return;
        try
        {
            IsBusy = true;
            MensajeEstado = "Agregando clase (Bloom sugerido)...";
            await _api.AgregarClaseAsync(Planificacion.Id);
            await CargarAsync();
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
    private async Task AbrirClaseAsync(ClaseFilaViewModel? fila)
    {
        if (fila is null) return;
        await Shell.Current.GoToAsync($"claseDetalle?id={fila.Id}");
    }

    [RelayCommand]
    private async Task CalendarioAsync()
    {
        if (Planificacion is null) return;
        await Shell.Current.GoToAsync($"planningCalendar?planningId={Planificacion.Id}");
    }

    [RelayCommand]
    private async Task SecuenciaAsync()
    {
        if (Planificacion is null) return;
        await Shell.Current.GoToAsync($"planningSequence?planningId={Planificacion.Id}");
    }

    [RelayCommand]
    private async Task CoberturaAsync()
    {
        if (Planificacion is null) return;
        await Shell.Current.GoToAsync($"planningCoverage?planningId={Planificacion.Id}");
    }

    [RelayCommand]
    private async Task ExportarAsync()
    {
        if (Planificacion is null) return;
        await Shell.Current.GoToAsync($"exportOptions?planningId={Planificacion.Id}&context=planning");
    }

    [RelayCommand]
    private async Task HistorialExportacionesAsync()
    {
        await Shell.Current.GoToAsync("exportHistory");
    }

    private static string PickSavePath(string fileName)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AppEducativa");
        Directory.CreateDirectory(folder);
        var full = Path.Combine(folder, fileName);
        if (File.Exists(full))
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            full = Path.Combine(folder, Path.GetFileNameWithoutExtension(fileName) + $"_{stamp}{Path.GetExtension(fileName)}");
        }
        return full;
    }
}

public partial class ClaseFilaViewModel : ObservableObject
{
    public Guid Id { get; set; }
    public int Numero { get; set; }
    public string FechaTexto { get; set; } = string.Empty;
    public string OaTexto { get; set; } = string.Empty;
    public string NivelBloom { get; set; } = string.Empty;
    public Color ChipColor { get; set; } = Colors.Gray;
    public string MaterialFlags { get; set; } = string.Empty;
    public string EstructuraFlag { get; set; } = string.Empty;

    public static ClaseFilaViewModel From(ClaseResumenDto c)
    {
        var flags = new List<string>();
        if (c.TieneGuia) flags.Add("Guía");
        if (c.TieneEjercicios) flags.Add("Ejerc.");
        if (c.TienePrueba) flags.Add("Prueba");
        return new ClaseFilaViewModel
        {
            Id = c.Id,
            Numero = c.Numero,
            FechaTexto = c.Fecha.ToString("dd/MM"),
            OaTexto = string.IsNullOrWhiteSpace(c.ObjetivoCodigo)
                ? c.ObjetivoResumen
                : $"{c.ObjetivoCodigo}: {c.ObjetivoResumen}",
            NivelBloom = c.NivelBloom,
            ChipColor = BloomChipHelper.ColorFor(c.NivelBloom),
            MaterialFlags = flags.Count == 0 ? "—" : string.Join(" · ", flags),
            EstructuraFlag = string.IsNullOrWhiteSpace(c.EstructuraEstado)
                ? (c.TieneEstructura ? "Estructura generada" : "Sin estructura")
                : c.EstructuraEstado
        };
    }
}
