using AppEducativa.Api.Models;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Services;

public static class PlanificacionMapper
{
    private static string NombreAsignatura(Planificacion? p) =>
        p?.NivelAsignatura?.NombreEnNivel
        ?? p?.NivelAsignatura?.Asignatura?.Nombre
        ?? "";

    public static PlanificacionResumenDto ToResumen(Planificacion p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Nivel = p.Nivel?.Nombre ?? "",
        Asignatura = NombreAsignatura(p),
        Unidad = p.Unidad is null ? "" : $"{p.Unidad.Numero}. {p.Unidad.Nombre}",
        SchoolCourseId = p.SchoolCourseId,
        FechaInicio = p.FechaInicio,
        FechaFin = p.FechaFin,
        Estado = p.Estado,
        CantidadClases = p.Clases?.Count ?? 0,
        FechaCreacion = p.FechaCreacion
    };

    public static PlanificacionDetalleDto ToDetalle(Planificacion p) => new()
    {
        Id = p.Id,
        NivelId = p.NivelId,
        AsignaturaId = p.NivelAsignaturaId,
        UnidadId = p.UnidadId,
        Nombre = p.Nombre,
        Nivel = p.Nivel?.Nombre ?? "",
        Asignatura = NombreAsignatura(p),
        Unidad = p.Unidad is null ? "" : $"{p.Unidad.Numero}. {p.Unidad.Nombre}",
        FechaInicio = p.FechaInicio,
        FechaFin = p.FechaFin,
        Estado = p.Estado,
        FechaCreacion = p.FechaCreacion,
        Clases = (p.Clases ?? [])
            .OrderBy(c => c.Numero)
            .Select(ToResumen)
            .ToList()
    };

    public static ClaseResumenDto ToResumen(Clase c)
    {
        var docs = c.Documentos ?? [];
        return new ClaseResumenDto
        {
            Id = c.Id,
            PlanificacionId = c.PlanificacionId,
            Numero = c.Numero,
            Fecha = c.Fecha,
            ObjetivoAprendizajeId = c.ObjetivoAprendizajeId,
            ObjetivoCodigo = c.ObjetivoAprendizaje?.Codigo ?? "",
            ObjetivoResumen = Truncar(c.ObjetivoAprendizaje?.Descripcion, 80),
            NivelBloom = c.NivelBloom,
            Estado = c.Estado,
            TieneEstructura = !string.IsNullOrWhiteSpace(c.DescripcionInicio)
                              || !string.IsNullOrWhiteSpace(c.DescripcionDesarrollo)
                              || !string.IsNullOrWhiteSpace(c.DescripcionCierre),
            TieneGuia = docs.Any(d => d.Tipo == TipoDocumento.Guia),
            TieneEjercicios = docs.Any(d => d.Tipo == TipoDocumento.Ejercicios),
            TienePrueba = docs.Any(d => d.Tipo == TipoDocumento.Prueba)
        };
    }

    public static ClaseDetalleDto ToDetalle(Clase c)
    {
        var plan = c.Planificacion;
        return new ClaseDetalleDto
        {
            Id = c.Id,
            PlanificacionId = c.PlanificacionId,
            Numero = c.Numero,
            Fecha = c.Fecha,
            ObjetivoAprendizajeId = c.ObjetivoAprendizajeId,
            ObjetivoCodigo = c.ObjetivoAprendizaje?.Codigo ?? "",
            ObjetivoDescripcion = c.ObjetivoAprendizaje?.Descripcion ?? "",
            NivelBloom = c.NivelBloom,
            DescripcionInicio = c.DescripcionInicio,
            DescripcionDesarrollo = c.DescripcionDesarrollo,
            DescripcionCierre = c.DescripcionCierre,
            Estado = c.Estado,
            IndicadorEvaluacionIds = (c.Indicadores ?? []).Select(i => i.IndicadorEvaluacionId).ToList(),
            Indicadores = (c.Indicadores ?? [])
                .Select(i => i.IndicadorEvaluacion?.Descripcion ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList(),
            Documentos = (c.Documentos ?? [])
                .OrderByDescending(d => d.FechaCreacion)
                .Select(DocumentoMapper.ToDto)
                .ToList(),
            NivelId = plan?.NivelId ?? Guid.Empty,
            AsignaturaId = plan?.NivelAsignaturaId ?? Guid.Empty,
            UnidadId = plan?.UnidadId ?? Guid.Empty,
            Nivel = plan?.Nivel?.Nombre ?? "",
            Asignatura = NombreAsignatura(plan),
            Unidad = plan?.Unidad is null ? "" : $"{plan.Unidad.Numero}. {plan.Unidad.Nombre}"
        };
    }

    public static string SugerirSiguienteBloom(string? bloomAnterior)
    {
        var nombres = NivelBloomHelper.Nombres;
        if (string.IsNullOrWhiteSpace(bloomAnterior))
            return nombres[0];
        var orden = NivelBloomHelper.Orden(bloomAnterior);
        if (orden >= nombres.Count)
            return nombres[^1];
        return nombres[orden];
    }

    private static string Truncar(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        text = text.Trim();
        return text.Length <= max ? text : text[..(max - 1)] + "…";
    }
}
