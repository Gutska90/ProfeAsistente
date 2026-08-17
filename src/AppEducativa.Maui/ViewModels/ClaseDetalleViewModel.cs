using System.Collections.ObjectModel;
using AppEducativa.Maui.Services;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppEducativa.Maui.ViewModels;

[QueryProperty(nameof(ClaseId), "id")]
public partial class ClaseDetalleViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly IOfflineSyncService _sync;

    public ClaseDetalleViewModel(IApiClient api, IOfflineSyncService sync)
    {
        _api = api;
        _sync = sync;
    }

    public ObservableCollection<ObjetivoAprendizajeDto> Objetivos { get; } = [];
    public ObservableCollection<string> NivelesBloom { get; } = new(NivelBloomHelper.Nombres);
    public ObservableCollection<EditableItemViewModel> ItemsEditables { get; } = [];
    public ObservableCollection<DuaStrategyRow> DuaStrategies { get; } = [];
    public IReadOnlyList<string> DuaPrincipleNames { get; } = ["Participación", "Representación", "Acción y expresión"];

    [ObservableProperty] private string claseId = string.Empty;
    [ObservableProperty] private ClaseDetalleDto? clase;
    [ObservableProperty] private ObjetivoAprendizajeDto? oaSeleccionado;
    [ObservableProperty] private string? nivelBloomSeleccionado;
    [ObservableProperty] private string? descripcionInicio;
    [ObservableProperty] private string? descripcionDesarrollo;
    [ObservableProperty] private string? descripcionCierre;
    [ObservableProperty] private string indicadoresTexto = string.Empty;
    [ObservableProperty] private Color chipColor = Colors.Gray;
    [ObservableProperty] private bool generarGuia = true;
    [ObservableProperty] private bool generarEjercicios;
    [ObservableProperty] private bool generarPrueba;
    [ObservableProperty] private DocumentoDto? documentoActual;
    [ObservableProperty] private string contenidoEditable = string.Empty;
    [ObservableProperty] private bool mostrarEditorMaterial;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty] private string? estructuraEstado;
    [ObservableProperty] private bool estructuraRequiereRevision;
    [ObservableProperty] private bool estructuraDesactualizada;
    [ObservableProperty] private bool tieneEstructuraAvanzada;
    [ObservableProperty] private string duaPrincipleSeleccionado = "Representación";
    [ObservableProperty] private string duaEstrategia = string.Empty;
    [ObservableProperty] private bool mostrarHerramientasAvanzadas;

    partial void OnClaseIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = CargarAsync();
    }

    partial void OnNivelBloomSeleccionadoChanged(string? value) =>
        ChipColor = BloomChipHelper.ColorFor(value);

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        try
        {
            IsBusy = true;
            Clase = await _sync.GetClaseAsync(id);
            if (Clase is null)
            {
                MensajeEstado = "Clase no encontrada.";
                return;
            }

            DescripcionInicio = Clase.DescripcionInicio;
            DescripcionDesarrollo = Clase.DescripcionDesarrollo;
            DescripcionCierre = Clase.DescripcionCierre;
            NivelBloomSeleccionado = Clase.NivelBloom;
            IndicadoresTexto = Clase.Indicadores.Count == 0
                ? "(sin indicadores asociados)"
                : string.Join("\n", Clase.Indicadores.Select(i => "• " + i));

            Objetivos.Clear();
            var oas = await _api.GetObjetivosAsync(Clase.UnidadId);
            foreach (var o in oas) Objetivos.Add(o);
            OaSeleccionado = Objetivos.FirstOrDefault(o => o.Id == Clase.ObjetivoAprendizajeId)
                             ?? Objetivos.FirstOrDefault();

            DocumentoActual = Clase.Documentos.OrderByDescending(d => d.FechaCreacion).FirstOrDefault();
            if (DocumentoActual is not null)
                CargarMaterial(DocumentoActual);

            await CargarEstadoEstructuraAsync(id);
            await CargarDuaAsync(id);

            MensajeEstado = $"Clase {Clase.Numero} · {Clase.Fecha:dd/MM/yyyy} · {Clase.ObjetivoCodigo}";
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
    private async Task ConfigurarGenerarEstructuraAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        await Shell.Current.GoToAsync($"classStructureGeneration?id={id}");
    }

    [RelayCommand]
    private async Task AbrirMaterialesEducativosAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        await Shell.Current.GoToAsync($"educationalDocuments?id={id}");
    }

    [RelayCommand]
    private async Task GenerarDocumentoEducativoAsync(string? tipo)
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        await Shell.Current.GoToAsync($"educationalDocumentGeneration?id={id}&type={tipo ?? "Assessment"}");
    }

    [RelayCommand]
    private async Task ExportarClaseDocxAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        await Shell.Current.GoToAsync($"exportOptions?classId={id}&context=class");
    }

    [RelayCommand]
    private async Task VerEstructuraActualAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        try
        {
            IsBusy = true;
            var current = await _api.GetCurrentStructureAsync(id);
            if (current is null)
            {
                MensajeEstado = "No hay estructura avanzada vigente. Usa «Configurar y generar estructura».";
                return;
            }

            await Shell.Current.GoToAsync(
                $"classStructureEditor?generationId={current.GenerationId}&id={id}");
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
    private async Task GuardarClaseAsync()
    {
        if (Clase is null) return;
        try
        {
            IsBusy = true;
            var request = new ActualizarClaseRequest
            {
                ObjetivoAprendizajeId = OaSeleccionado?.Id,
                NivelBloom = NivelBloomSeleccionado,
                DescripcionInicio = DescripcionInicio,
                DescripcionDesarrollo = DescripcionDesarrollo,
                DescripcionCierre = DescripcionCierre
            };
            Clase.DescripcionInicio = DescripcionInicio;
            Clase.DescripcionDesarrollo = DescripcionDesarrollo;
            Clase.DescripcionCierre = DescripcionCierre;
            Clase.NivelBloom = NivelBloomSeleccionado ?? Clase.NivelBloom;
            Clase = await _sync.SaveClaseAsync(Clase.Id, request, Clase);
            MensajeEstado = _sync.PendingCount > 0
                ? "Clase guardada en el dispositivo. Se enviará al reconectar."
                : "Clase guardada.";
            await RefrescarIndicadoresAsync();
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
    private async Task CompletarClaseAsync()
    {
        if (Clase is null) return;
        try
        {
            IsBusy = true;
            await GuardarSilenciosoAsync();
            await _sync.CompleteClaseAsync(Clase.Id, new CompleteClassRequest
            {
                ActualDate = DateOnly.FromDateTime(DateTime.Now),
                Observation = "Clase registrada como realizada desde la app docente."
            });
            MensajeEstado = _sync.PendingCount > 0
                ? "Clase marcada en el dispositivo. Se sincronizará con la API."
                : "Clase marcada como realizada. La cobertura ejecutada se actualizó.";
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
    private async Task AbrirAsistenciaAsync()
    {
        if (Clase is null) return;
        await Shell.Current.GoToAsync($"asistencia?classId={Clase.Id}");
    }

    [RelayCommand]
    private async Task AbrirEvaluacionAsync()
    {
        if (Clase is null) return;
        await Shell.Current.GoToAsync($"evaluacionClase?classId={Clase.Id}");
    }

    [RelayCommand]
    private void ToggleHerramientas() => MostrarHerramientasAvanzadas = !MostrarHerramientasAvanzadas;

    private async Task CargarDuaAsync(Guid classId)
    {
        DuaStrategies.Clear();
        foreach (var d in await _sync.GetDuaAsync(classId))
            DuaStrategies.Add(DuaStrategyRow.FromDto(d));
    }

    [RelayCommand]
    private async Task AgregarDuaAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id) || string.IsNullOrWhiteSpace(DuaEstrategia))
        {
            MensajeEstado = "Escriba una estrategia DUA para esta clase.";
            return;
        }

        var principle = DuaPrincipleSeleccionado switch
        {
            "Participación" => DuaPrinciple.Engagement,
            "Acción y expresión" => DuaPrinciple.ActionAndExpression,
            _ => DuaPrinciple.Representation
        };
        await _sync.AddDuaAsync(id, new AddClassDuaStrategyRequest
        {
            Principle = principle,
            Strategy = DuaEstrategia.Trim()
        });
        DuaEstrategia = string.Empty;
        await CargarDuaAsync(id);
        MensajeEstado = _sync.PendingCount > 0
            ? "DUA guardado en el dispositivo. Se enviará al reconectar."
            : "Estrategia DUA registrada en esta clase.";
    }

    [RelayCommand]
    private async Task GenerarEstructuraAsync()
    {
        if (Clase is null) return;
        try
        {
            IsBusy = true;
            MensajeEstado = "Generando Inicio / Desarrollo / Cierre...";
            await GuardarSilenciosoAsync();
            Clase = await _api.GenerarEstructuraClaseAsync(Clase.Id);
            DescripcionInicio = Clase.DescripcionInicio;
            DescripcionDesarrollo = Clase.DescripcionDesarrollo;
            DescripcionCierre = Clase.DescripcionCierre;
            await CargarEstadoEstructuraAsync(Clase.Id);
            MensajeEstado = "Estructura generada. Puedes editarla libremente.";
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
    private async Task GenerarMaterialAsync()
    {
        if (Clase is null) return;
        var tipos = new List<TipoDocumento>();
        if (GenerarGuia) tipos.Add(TipoDocumento.Guia);
        if (GenerarEjercicios) tipos.Add(TipoDocumento.Ejercicios);
        if (GenerarPrueba) tipos.Add(TipoDocumento.Prueba);
        if (tipos.Count == 0)
        {
            MensajeEstado = "Marca al menos un tipo de material (Guía recomendada en MVP).";
            return;
        }

        try
        {
            IsBusy = true;
            await GuardarSilenciosoAsync();
            DocumentoDto? ultimo = null;
            foreach (var tipo in tipos)
            {
                MensajeEstado = $"Generando {tipo}...";
                ultimo = await _api.GenerarMaterialClaseAsync(Clase.Id, new GenerarMaterialClaseRequest
                {
                    Tipo = tipo,
                    CantidadItems = 5,
                    SoloSeleccionMultiple = tipo == TipoDocumento.Prueba
                });
            }

            if (ultimo is not null)
            {
                DocumentoActual = ultimo;
                CargarMaterial(ultimo);
            }

            Clase = await _api.GetClaseAsync(Clase.Id);
            MensajeEstado = "Material generado.";
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
    private async Task GuardarMaterialAsync()
    {
        if (DocumentoActual is null) return;
        try
        {
            IsBusy = true;
            DocumentoActual = await _api.ActualizarDocumentoAsync(DocumentoActual.Id, new ActualizarDocumentoRequest
            {
                ContenidoGenerado = ContenidoEditable,
                Items = ItemsEditables.Select(i => i.ToDto()).ToList()
            });
            MensajeEstado = "Material guardado.";
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
    private async Task ExportarMaterialAsync()
    {
        if (DocumentoActual is null)
        {
            MensajeEstado = "No hay material para exportar.";
            return;
        }

        try
        {
            IsBusy = true;
            await GuardarMaterialAsync();
            var (bytes, fileName) = await _api.ExportarDocumentoAsync(DocumentoActual.Id);
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AppEducativa");
            Directory.CreateDirectory(folder);
            var dest = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(dest, bytes);
            MensajeEstado = $"Exportado: {dest}";
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

    private async Task GuardarSilenciosoAsync()
    {
        if (Clase is null) return;
        Clase = await _sync.SaveClaseAsync(Clase.Id, new ActualizarClaseRequest
        {
            ObjetivoAprendizajeId = OaSeleccionado?.Id,
            NivelBloom = NivelBloomSeleccionado,
            DescripcionInicio = DescripcionInicio,
            DescripcionDesarrollo = DescripcionDesarrollo,
            DescripcionCierre = DescripcionCierre
        }, Clase);
    }

    private async Task RefrescarIndicadoresAsync()
    {
        if (OaSeleccionado is null) return;
        var detalle = await _api.GetObjetivoDetalleAsync(OaSeleccionado.Id);
        if (detalle is null) return;
        IndicadoresTexto = detalle.Indicadores.Count == 0
            ? "(sin indicadores)"
            : string.Join("\n", detalle.Indicadores.Select(i => "• " + i));
    }

    private void CargarMaterial(DocumentoDto doc)
    {
        ItemsEditables.Clear();
        foreach (var item in doc.Items.OrderBy(i => i.Orden))
            ItemsEditables.Add(EditableItemViewModel.FromDto(item));
        MostrarEditorMaterial = true;
        const string marker = "\n---\n";
        var idx = doc.ContenidoGenerado.IndexOf(marker, StringComparison.Ordinal);
        ContenidoEditable = idx >= 0 ? doc.ContenidoGenerado[..idx].TrimEnd() : doc.ContenidoGenerado;
    }

    private async Task CargarEstadoEstructuraAsync(Guid claseId)
    {
        EstructuraEstado = null;
        EstructuraRequiereRevision = false;
        EstructuraDesactualizada = false;
        TieneEstructuraAvanzada = false;

        try
        {
            var current = await _api.GetCurrentStructureAsync(claseId);
            if (current is null) return;

            TieneEstructuraAvanzada = true;
            EstructuraRequiereRevision = current.RequiresReview;
            EstructuraDesactualizada = current.IsOutdated;

            var parts = new List<string>
            {
                $"Estructura avanzada v{current.GenerationNumber}",
                current.Status
            };
            if (current.IsCurrentVersion) parts.Add("vigente");
            if (current.RequiresReview) parts.Add("requiere revisión");
            if (current.IsOutdated) parts.Add("desactualizada");
            EstructuraEstado = string.Join(" · ", parts);
        }
        catch
        {
            // Endpoint opcional / sin generación aún
        }
    }
}

public sealed class DuaStrategyRow
{
    public Guid Id { get; init; }
    public string PrincipleName { get; init; } = string.Empty;
    public string Strategy { get; init; } = string.Empty;

    public static DuaStrategyRow FromDto(ClassDuaStrategyDto dto) => new()
    {
        Id = dto.Id,
        PrincipleName = dto.Principle switch
        {
            DuaPrinciple.Engagement => "Participación",
            DuaPrinciple.ActionAndExpression => "Acción y expresión",
            _ => "Representación"
        },
        Strategy = dto.Strategy
    };
}
