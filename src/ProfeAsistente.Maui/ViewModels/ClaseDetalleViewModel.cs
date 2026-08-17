using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels;

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
    public ObservableCollection<EducationalDocumentSummaryDto> Materiales { get; } = [];
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
    [ObservableProperty] private bool mostrarPlanificacion;
    [ObservableProperty] private bool mostrarAdaptar;
    [ObservableProperty] private bool mostrarEditarCurriculo;
    [ObservableProperty] private string tituloClase = "Clase";
    [ObservableProperty] private string subtituloClase = string.Empty;
    [ObservableProperty] private string resumenOa = string.Empty;
    [ObservableProperty] private string cicloResumen = string.Empty;
    [ObservableProperty] private string siguientePasoTexto = string.Empty;
    [ObservableProperty] private string siguientePasoBoton = "Continuar";
    [ObservableProperty] private bool tieneSiguientePaso;
    [ObservableProperty] private bool sinMateriales = true;
    [ObservableProperty] private bool tienePlanificacion;
    [ObservableProperty] private bool tieneMaterial;
    [ObservableProperty] private bool tieneEvaluacion;
    [ObservableProperty] private bool tieneEvidencia;
    [ObservableProperty] private bool necesitaRefuerzo;
    [ObservableProperty] private string siguientePasoAccion = string.Empty;

    partial void OnClaseIdChanged(string value)
    {
        if (Guid.TryParse(value, out _))
            _ = CargarAsync();
    }

    partial void OnNivelBloomSeleccionadoChanged(string? value) =>
        ChipColor = BloomChipHelper.ColorFor(value);

    partial void OnOaSeleccionadoChanged(ObjetivoAprendizajeDto? value)
    {
        if (value is null) return;
        ResumenOa = $"OA {value.Codigo}: {value.Descripcion}";
        _ = RefrescarIndicadoresAsync();
    }

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

            TituloClase = $"Clase {Clase.Numero}";
            SubtituloClase = $"{Clase.Asignatura} · {Clase.Unidad} · {Clase.Fecha:dd/MM/yyyy}";
            DescripcionInicio = Clase.DescripcionInicio;
            DescripcionDesarrollo = Clase.DescripcionDesarrollo;
            DescripcionCierre = Clase.DescripcionCierre;
            NivelBloomSeleccionado = Clase.NivelBloom;
            IndicadoresTexto = Clase.Indicadores.Count == 0
                ? "(sin indicadores asociados)"
                : string.Join("\n", Clase.Indicadores.Select(i => "• " + i));
            ResumenOa = $"OA {Clase.ObjetivoCodigo}: {Clase.ObjetivoDescripcion}";

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
            await RefrescarCicloAsync(id);

            // Si aún no hay momentos, abrir el bloque de planificación.
            MostrarPlanificacion = !TienePlanificacion;

            MensajeEstado = Clase.Estado == EstadoClase.Realizada ? "Clase realizada" : "Lista para preparar";
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

    /// <summary>Recarga materiales y estado del ciclo al volver de generación/evaluación.</summary>
    public Task RefreshOnAppearingAsync() =>
        Guid.TryParse(ClaseId, out var id) ? RefrescarCicloAsync(id) : Task.CompletedTask;

    private async Task RefrescarCicloAsync(Guid id)
    {
        Materiales.Clear();
        try
        {
            foreach (var m in await _api.GetEducationalDocumentsAsync(id))
                Materiales.Add(m);
        }
        catch
        {
            // Offline: lista vacía; el ciclo sigue con lo que haya en clase.
        }

        SinMateriales = Materiales.Count == 0;
        TieneMaterial = Materiales.Count > 0;
        TienePlanificacion = !string.IsNullOrWhiteSpace(DescripcionInicio)
                             || !string.IsNullOrWhiteSpace(DescripcionDesarrollo)
                             || !string.IsNullOrWhiteSpace(DescripcionCierre);

        TieneEvaluacion = false;
        TieneEvidencia = false;
        NecesitaRefuerzo = false;
        try
        {
            var assessments = await _sync.GetAssessmentsAsync(id);
            TieneEvaluacion = assessments.Count > 0;
            var first = assessments.FirstOrDefault();
            if (first is not null)
            {
                var evidence = await _api.GetAssessmentEvidenceAsync(first.Id);
                if (evidence is not null)
                {
                    var scored = evidence.CountLogrado + evidence.CountMedianamente + evidence.CountPorLograr;
                    TieneEvidencia = scored > 0;
                    NecesitaRefuerzo = evidence.NeedsReinforcement;
                }
            }
        }
        catch
        {
            // Sin API de evaluaciones: el checklist queda parcial.
        }

        var plan = TienePlanificacion ? "✓" : "○";
        var mat = TieneMaterial ? "✓" : "○";
        var eva = TieneEvaluacion ? "✓" : "○";
        var evi = TieneEvidencia ? "✓" : "○";
        CicloResumen = $"{plan} Planificar  {mat} Material  {eva} Evaluar  {evi} Evidencia";

        if (!TienePlanificacion)
        {
            SiguientePasoTexto = "Siguiente: planifique Inicio / Desarrollo / Cierre para esta clase.";
            SiguientePasoBoton = "Planificar clase";
            SiguientePasoAccion = "planificar";
            TieneSiguientePaso = true;
        }
        else if (!TieneMaterial)
        {
            SiguientePasoTexto = "Siguiente: cree una guía, actividad o evaluación alineada al OA.";
            SiguientePasoBoton = "Crear guía";
            SiguientePasoAccion = "guia";
            TieneSiguientePaso = true;
        }
        else if (!TieneEvaluacion)
        {
            SiguientePasoTexto = "Siguiente: registre una evaluación y niveles de logro de la nómina.";
            SiguientePasoBoton = "Evaluar / evidencia";
            SiguientePasoAccion = "evaluar";
            TieneSiguientePaso = true;
        }
        else if (!TieneEvidencia)
        {
            SiguientePasoTexto = "Siguiente: guarde puntajes para leer la evidencia por OA.";
            SiguientePasoBoton = "Registrar evidencia";
            SiguientePasoAccion = "evaluar";
            TieneSiguientePaso = true;
        }
        else if (NecesitaRefuerzo)
        {
            SiguientePasoTexto = "La evidencia sugiere refuerzo para este OA. Prepare una actividad de refuerzo.";
            SiguientePasoBoton = "Crear refuerzo";
            SiguientePasoAccion = "refuerzo";
            TieneSiguientePaso = true;
        }
        else if (Clase?.Estado != EstadoClase.Realizada)
        {
            SiguientePasoTexto = "Ciclo listo. Puede marcar la clase como realizada cuando corresponda.";
            SiguientePasoBoton = "Clase realizada";
            SiguientePasoAccion = "completar";
            TieneSiguientePaso = true;
        }
        else
        {
            SiguientePasoTexto = NecesitaRefuerzo
                ? "Clase realizada. Aún hay estudiantes que necesitan refuerzo en este OA."
                : "Ciclo cerrado para esta clase.";
            SiguientePasoBoton = "Crear refuerzo";
            SiguientePasoAccion = NecesitaRefuerzo ? "refuerzo" : string.Empty;
            TieneSiguientePaso = NecesitaRefuerzo;
        }
    }

    [RelayCommand]
    private async Task EjecutarSiguientePasoAsync()
    {
        switch (SiguientePasoAccion)
        {
            case "planificar":
                PlanificarClase();
                break;
            case "guia":
                await CrearGuiaAsync();
                break;
            case "evaluar":
                await AbrirEvaluacionAsync();
                break;
            case "refuerzo":
                if (Guid.TryParse(ClaseId, out var id))
                    await Shell.Current.GoToAsync($"educationalDocumentGeneration?id={id}&type=Exercises&intent=reinforce");
                break;
            case "completar":
                await CompletarClaseAsync();
                break;
        }
    }

    [RelayCommand]
    private async Task AbrirMaterialAsync(EducationalDocumentSummaryDto? doc)
    {
        if (doc is null || !Guid.TryParse(ClaseId, out _)) return;
        await Shell.Current.GoToAsync($"educationalDocumentEditor?documentId={doc.Id}&id={ClaseId}");
    }

    [RelayCommand]
    private void ToggleEditarCurriculo() => MostrarEditarCurriculo = !MostrarEditarCurriculo;

    [RelayCommand]
    private void PlanificarClase()
    {
        MostrarPlanificacion = true;
        MostrarAdaptar = false;
        MensajeEstado = "Edite los momentos o genere Inicio / Desarrollo / Cierre.";
    }

    [RelayCommand]
    private async Task CrearGuiaAsync()
        => await AbrirGeneracionAsync("LearningGuide");

    [RelayCommand]
    private async Task CrearActividadAsync()
        => await AbrirGeneracionAsync("Exercises");

    [RelayCommand]
    private async Task CrearEvaluacionMaterialAsync()
        => await AbrirGeneracionAsync("Assessment");

    [RelayCommand]
    private async Task CrearTicketAsync()
        => await AbrirGeneracionAsync("Assessment", "exitTicket");

    [RelayCommand]
    private void AdaptarMaterial()
    {
        MostrarAdaptar = true;
        MostrarPlanificacion = false;
        MensajeEstado = "Elija un atajo de adaptación o registre una estrategia DUA.";
    }

    [RelayCommand]
    private async Task AdaptarSimplificarAsync()
        => await AbrirGeneracionAsync("LearningGuide", "simplify");

    [RelayCommand]
    private async Task AdaptarAndamiajeAsync()
        => await AbrirGeneracionAsync("Exercises", "scaffold");

    [RelayCommand]
    private async Task AdaptarApoyoVisualAsync()
    {
        if (!Guid.TryParse(ClaseId, out _)) return;
        DuaPrincipleSeleccionado = "Representación";
        DuaEstrategia = "Incluir apoyos visuales (esquemas, pictogramas o ejemplos concretos) para representar el OA.";
        await AgregarDuaAsync();
        MostrarAdaptar = true;
    }

    private async Task AbrirGeneracionAsync(string type, string? intent = null)
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        await GuardarSilenciosoAsync();
        var q = $"educationalDocumentGeneration?id={id}&type={type}";
        if (!string.IsNullOrWhiteSpace(intent))
            q += $"&intent={intent}";
        await Shell.Current.GoToAsync(q);
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
        => await AbrirGeneracionAsync(tipo ?? "Assessment");

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
            if (Guid.TryParse(ClaseId, out var cid))
                await RefrescarCicloAsync(cid);
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
                : "Clase marcada como realizada. Continúe con evaluar / evidencia si aún no lo hizo.";
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
        if (!Guid.TryParse(ClaseId, out var id)) return;
        // Unificación P4: el material canónico es EducationalDocument (Guía/Actividad/Prueba).
        var type = GenerarPrueba ? "Assessment" : GenerarEjercicios ? "Exercises" : "LearningGuide";
        MensajeEstado = "Abriendo generación de material (flujo actual). El material legado quedó deprecado.";
        await AbrirGeneracionAsync(type);
    }

    [RelayCommand]
    private async Task GuardarMaterialAsync()
    {
        MensajeEstado = "Use el editor de materiales (Biblioteca o Ver materiales de la clase). El editor legado ya no guarda.";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportarMaterialAsync()
    {
        if (!Guid.TryParse(ClaseId, out var id)) return;
        MensajeEstado = "Exporte desde el material educativo abierto en el editor.";
        await Shell.Current.GoToAsync($"educationalDocuments?id={id}");
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
