using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProfeAsistente.Maui.ViewModels;

public partial class EditableItemViewModel : ObservableObject
{
    public Guid Id { get; set; }
    public Guid DocumentoId { get; set; }
    public TipoItem Tipo { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TituloItem))]
    private int orden;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TituloItem))]
    private string? nivelBloom;

    [ObservableProperty]
    private string? verboBloom;

    [ObservableProperty]
    private string enunciado = string.Empty;

    [ObservableProperty]
    private string alternativaA = string.Empty;

    [ObservableProperty]
    private string alternativaB = string.Empty;

    [ObservableProperty]
    private string alternativaC = string.Empty;

    [ObservableProperty]
    private string alternativaD = string.Empty;

    [ObservableProperty]
    private string? respuestaCorrecta;

    [ObservableProperty]
    private int puntaje = 1;

    [ObservableProperty]
    private bool esSeleccionMultiple;

    public string TituloItem =>
        string.IsNullOrWhiteSpace(NivelBloom)
            ? $"Actividad {Orden}"
            : $"{Orden}. [{NivelBloom}]{(string.IsNullOrWhiteSpace(VerboBloom) ? "" : $" · {VerboBloom}")}";

    public static EditableItemViewModel FromDto(ItemDto dto) => new()
    {
        Id = dto.Id,
        DocumentoId = dto.DocumentoId,
        Tipo = dto.Tipo,
        Orden = dto.Orden,
        NivelBloom = dto.NivelBloom,
        VerboBloom = dto.VerboBloom,
        Enunciado = dto.Enunciado,
        AlternativaA = TomarAlt(dto.Alternativas, 0),
        AlternativaB = TomarAlt(dto.Alternativas, 1),
        AlternativaC = TomarAlt(dto.Alternativas, 2),
        AlternativaD = TomarAlt(dto.Alternativas, 3),
        RespuestaCorrecta = dto.RespuestaCorrecta,
        Puntaje = dto.Puntaje,
        EsSeleccionMultiple = dto.Tipo == TipoItem.SeleccionMultiple || dto.Alternativas.Count >= 2
    };

    public ItemDto ToDto()
    {
        var alts = new List<string>();
        if (EsSeleccionMultiple || Tipo == TipoItem.SeleccionMultiple || Tipo == TipoItem.VerdaderoFalso)
        {
            if (!string.IsNullOrWhiteSpace(AlternativaA)) alts.Add(NormalizarAlt(AlternativaA, 'A'));
            if (!string.IsNullOrWhiteSpace(AlternativaB)) alts.Add(NormalizarAlt(AlternativaB, 'B'));
            if (!string.IsNullOrWhiteSpace(AlternativaC)) alts.Add(NormalizarAlt(AlternativaC, 'C'));
            if (!string.IsNullOrWhiteSpace(AlternativaD)) alts.Add(NormalizarAlt(AlternativaD, 'D'));
        }

        return new ItemDto
        {
            Id = Id == Guid.Empty ? Guid.NewGuid() : Id,
            DocumentoId = DocumentoId,
            Tipo = Tipo,
            Enunciado = Enunciado.Trim(),
            Alternativas = alts,
            RespuestaCorrecta = RespuestaCorrecta,
            Puntaje = Puntaje <= 0 ? 1 : Puntaje,
            Orden = Orden,
            NivelBloom = NivelBloomHelper.Normalizar(NivelBloom),
            VerboBloom = VerboBloom
        };
    }

    private static string TomarAlt(IReadOnlyList<string> alts, int index) =>
        index < alts.Count ? alts[index] : string.Empty;

    private static string NormalizarAlt(string value, char letter)
    {
        var v = value.Trim();
        if (v.Length >= 2 && char.ToUpperInvariant(v[0]) == letter && (v[1] == ')' || v[1] == '.'))
            return v;
        return $"{letter}) {v}";
    }
}
