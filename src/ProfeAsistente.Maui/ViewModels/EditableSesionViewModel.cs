using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProfeAsistente.Maui.ViewModels;

public partial class EditableSesionViewModel : ObservableObject
{
    public Guid Id { get; set; }
    public Guid DocumentoId { get; set; }
    public Guid? ObjetivoAprendizajeId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TituloSesion))]
    [NotifyPropertyChangedFor(nameof(ChipTexto))]
    private int numero;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TituloSesion))]
    [NotifyPropertyChangedFor(nameof(ChipTexto))]
    [NotifyPropertyChangedFor(nameof(ChipColor))]
    private string? nivelBloom;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TituloSesion))]
    [NotifyPropertyChangedFor(nameof(ChipTexto))]
    private string? verboBloom;

    [ObservableProperty]
    private string descripcion = string.Empty;

    [ObservableProperty]
    private string actividades = string.Empty;

    [ObservableProperty]
    private string? indicadorEvaluacion;

    [ObservableProperty]
    private string? criterioLogro;

    [ObservableProperty]
    private int? minutosEstimados = 45;

    public string ChipTexto =>
        string.IsNullOrWhiteSpace(NivelBloom)
            ? $"S{Numero}"
            : $"{NivelBloom}{(string.IsNullOrWhiteSpace(VerboBloom) ? "" : $" · {VerboBloom}")}";

    public string TituloSesion =>
        $"Sesión {Numero}" + (string.IsNullOrWhiteSpace(NivelBloom) ? "" : $" · {NivelBloom}");

    /// <summary>Color de chip por nivel Bloom (simple, legible en claro).</summary>
    public Color ChipColor => NivelBloomHelper.Normalizar(NivelBloom) switch
    {
        "Recordar" => Color.FromArgb("#5B8C5A"),
        "Comprender" => Color.FromArgb("#3D7A9E"),
        "Aplicar" => Color.FromArgb("#C47B2B"),
        "Analizar" => Color.FromArgb("#8B5E9A"),
        "Evaluar" => Color.FromArgb("#B04A4A"),
        "Crear" => Color.FromArgb("#2F6F6F"),
        _ => Color.FromArgb("#666666")
    };

    public static EditableSesionViewModel FromDto(SesionPlanificadaDto dto) => new()
    {
        Id = dto.Id,
        DocumentoId = dto.DocumentoId,
        Numero = dto.Numero,
        Descripcion = dto.Descripcion,
        Actividades = dto.Actividades,
        NivelBloom = dto.NivelBloom,
        VerboBloom = dto.VerboBloom,
        ObjetivoAprendizajeId = dto.ObjetivoAprendizajeId,
        IndicadorEvaluacion = dto.IndicadorEvaluacion,
        CriterioLogro = dto.CriterioLogro,
        MinutosEstimados = dto.MinutosEstimados
    };

    public SesionPlanificadaDto ToDto() => new()
    {
        Id = Id == Guid.Empty ? Guid.NewGuid() : Id,
        DocumentoId = DocumentoId,
        Numero = Numero,
        Descripcion = Descripcion.Trim(),
        Actividades = Actividades.Trim(),
        NivelBloom = NivelBloomHelper.Normalizar(NivelBloom),
        VerboBloom = VerboBloom,
        ObjetivoAprendizajeId = ObjetivoAprendizajeId,
        IndicadorEvaluacion = IndicadorEvaluacion,
        CriterioLogro = CriterioLogro,
        MinutosEstimados = MinutosEstimados
    };
}
