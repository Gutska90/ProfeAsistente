using System.Collections.ObjectModel;
using ProfeAsistente.Maui.Services;
using ProfeAsistente.Maui.Services.Auth;
using ProfeAsistente.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProfeAsistente.Maui.ViewModels;

[QueryProperty(nameof(PreferredCourseId), "courseId")]
public partial class NuevaPlanificacionViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly IAuthenticationService _auth;
    private readonly LocalApiLauncher _launcher;
    private bool _suspend;
    private Guid? _preferredCourseId;

    public NuevaPlanificacionViewModel(IApiClient api, IAuthenticationService auth, LocalApiLauncher launcher)
    {
        _api = api;
        _auth = auth;
        _launcher = launcher;
    }

    public string PreferredCourseId
    {
        get => _preferredCourseId?.ToString() ?? string.Empty;
        set => _preferredCourseId = Guid.TryParse(value, out var id) ? id : null;
    }

    public ObservableCollection<NivelDto> Niveles { get; } = [];
    public ObservableCollection<AsignaturaDto> Asignaturas { get; } = [];
    public ObservableCollection<UnidadDto> Unidades { get; } = [];
    public ObservableCollection<SchoolCourseDto> Cursos { get; } = [];

    [ObservableProperty] private NivelDto? nivelSeleccionado;
    [ObservableProperty] private AsignaturaDto? asignaturaSeleccionada;
    [ObservableProperty] private UnidadDto? unidadSeleccionada;
    [ObservableProperty] private SchoolCourseDto? cursoSeleccionado;
    [ObservableProperty] private string? nombre;
    [ObservableProperty] private DateTime fechaInicio = DateTime.Today;
    [ObservableProperty] private DateTime fechaFin = DateTime.Today.AddDays(21);
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensajeEstado;

    partial void OnNivelSeleccionadoChanged(NivelDto? value)
    {
        if (_suspend) return;
        FiltrarCursos();
        _ = RecargarAsignaturasAsync();
    }

    partial void OnAsignaturaSeleccionadaChanged(AsignaturaDto? value)
    {
        if (_suspend) return;
        _ = RecargarUnidadesAsync();
    }

    [RelayCommand]
    public async Task InicializarAsync()
    {
        try
        {
            IsBusy = true;
            try { await _launcher.EnsureRunningAsync(); } catch { /* se usa error de API abajo */ }
            var niveles = await _api.GetNivelesAsync();
            var keepNivel = NivelSeleccionado?.Id;
            _suspend = true;
            Niveles.Clear();
            foreach (var n in niveles) Niveles.Add(n);
            NivelSeleccionado = Niveles.FirstOrDefault(n => n.Id == keepNivel)
                                ?? Niveles.FirstOrDefault(n => n.Nombre == "4° básico")
                                ?? Niveles.FirstOrDefault();
            _suspend = false;
            await CargarCursosAsync();
            if (_preferredCourseId is Guid preferred)
            {
                var preferredCourse = _cursosInstitucion.FirstOrDefault(c => c.Id == preferred);
                if (preferredCourse is not null)
                {
                    _suspend = true;
                    NivelSeleccionado = Niveles.FirstOrDefault(n => n.Id == preferredCourse.LevelId) ?? NivelSeleccionado;
                    _suspend = false;
                }
            }
            FiltrarCursos();
            if (_preferredCourseId is Guid preferredId)
                CursoSeleccionado = Cursos.FirstOrDefault(c => c.Id == preferredId) ?? CursoSeleccionado;
            await RecargarAsignaturasAsync();
            MensajeEstado = CursoSeleccionado is null
                ? $"{Niveles.Count} nivel(es). Desplace la lista y toque para elegir."
                : $"Curso preseleccionado: {CursoSeleccionado.DisplayName}.";
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

    private async Task RecargarAsignaturasAsync()
    {
        Asignaturas.Clear();
        Unidades.Clear();
        UnidadSeleccionada = null;
        if (NivelSeleccionado is null) return;
        try
        {
            var list = await _api.GetAsignaturasAsync(NivelSeleccionado.Id);
            _suspend = true;
            foreach (var a in list) Asignaturas.Add(a);
            AsignaturaSeleccionada = Asignaturas.FirstOrDefault(a => a.Nombre.Contains("Matem", StringComparison.OrdinalIgnoreCase))
                                    ?? Asignaturas.FirstOrDefault();
            _suspend = false;
            await RecargarUnidadesAsync();
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
    }

    private List<SchoolCourseDto> _cursosInstitucion = [];

    private async Task CargarCursosAsync()
    {
        _cursosInstitucion = [];
        if (_auth.ActiveInstitutionId is not Guid inst) return;
        try
        {
            _cursosInstitucion = (await _api.GetCoursesAsync(inst)).ToList();
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
    }

    private void FiltrarCursos()
    {
        Cursos.Clear();
        var nivelId = NivelSeleccionado?.Id;
        foreach (var c in _cursosInstitucion.Where(c => nivelId is null || c.LevelId == nivelId))
            Cursos.Add(c);
        CursoSeleccionado = Cursos.FirstOrDefault();
    }

    private async Task RecargarUnidadesAsync()
    {
        Unidades.Clear();
        UnidadSeleccionada = null;
        if (AsignaturaSeleccionada is null) return;
        try
        {
            var list = await _api.GetUnidadesAsync(AsignaturaSeleccionada.Id);
            foreach (var u in list) Unidades.Add(u);
            UnidadSeleccionada = Unidades.FirstOrDefault();
        }
        catch (Exception ex)
        {
            MensajeEstado = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CrearAsync()
    {
        if (NivelSeleccionado is null || AsignaturaSeleccionada is null || UnidadSeleccionada is null)
        {
            MensajeEstado = "Completa Nivel → Asignatura → Unidad.";
            return;
        }

        if (FechaFin.Date < FechaInicio.Date)
        {
            MensajeEstado = "La fecha fin debe ser posterior al inicio.";
            return;
        }

        try
        {
            IsBusy = true;
            MensajeEstado = "Creando planificación...";
            var plan = await _api.CrearPlanificacionAsync(new CrearPlanificacionRequest
            {
                NivelId = NivelSeleccionado.Id,
                AsignaturaId = AsignaturaSeleccionada.Id,
                UnidadId = UnidadSeleccionada.Id,
                Nombre = string.IsNullOrWhiteSpace(Nombre) ? null : Nombre.Trim(),
                FechaInicio = DateOnly.FromDateTime(FechaInicio),
                FechaFin = DateOnly.FromDateTime(FechaFin),
                InstitutionId = _auth.ActiveInstitutionId,
                SchoolCourseId = CursoSeleccionado?.Id
            });
            await Shell.Current.GoToAsync($"planificacionDetalle?id={plan.Id}");
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
}
