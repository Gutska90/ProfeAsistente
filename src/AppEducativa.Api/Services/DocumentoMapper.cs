using System.Text.Json;
using AppEducativa.Api.Models;
using AppEducativa.Api.Models.Curriculum;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Services;

public static class DocumentoMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static DocumentoDto ToDto(Documento documento)
    {
        return new DocumentoDto
        {
            Id = documento.Id,
            Tipo = documento.Tipo,
            ClaseId = documento.ClaseId,
            NivelId = documento.NivelId,
            AsignaturaId = documento.AsignaturaId,
            UnidadId = documento.UnidadId,
            Nivel = documento.Nivel,
            Asignatura = documento.Asignatura,
            Unidad = documento.Unidad,
            Tema = documento.Tema,
            ObjetivoAprendizaje = documento.ObjetivoAprendizaje,
            Instrucciones = documento.Instrucciones,
            ContenidoGenerado = documento.ContenidoGenerado,
            FechaCreacion = documento.FechaCreacion,
            Estado = documento.Estado,
            ObjetivoAprendizajeIds = documento.ObjetivosSeleccionados?
                .Select(o => o.ObjetivoAprendizajeId).ToList() ?? [],
            Items = documento.Items
                .OrderBy(i => i.Orden)
                .Select(ToDto)
                .ToList(),
            Sesiones = (documento.Sesiones ?? [])
                .OrderBy(s => s.Numero)
                .Select(ToDto)
                .ToList()
        };
    }

    public static ItemDto ToDto(Item item)
    {
        List<string> alternativas;
        try { alternativas = JsonSerializer.Deserialize<List<string>>(item.AlternativasJson) ?? []; }
        catch { alternativas = []; }

        return new ItemDto
        {
            Id = item.Id,
            DocumentoId = item.DocumentoId,
            Tipo = item.Tipo,
            Enunciado = item.Enunciado,
            Alternativas = alternativas,
            RespuestaCorrecta = item.RespuestaCorrecta,
            Puntaje = item.Puntaje,
            Orden = item.Orden,
            IndicadorEvaluacionId = item.IndicadorEvaluacionId,
            IndicadorEvaluacion = item.IndicadorEvaluacion?.Descripcion,
            NivelBloom = item.NivelBloom,
            VerboBloom = item.VerboBloom
        };
    }

    public static SesionPlanificadaDto ToDto(SesionPlanificada s) => new()
    {
        Id = s.Id,
        DocumentoId = s.DocumentoId,
        Numero = s.Numero,
        Descripcion = s.Descripcion,
        Actividades = s.Actividades,
        NivelBloom = s.NivelBloom,
        VerboBloom = s.VerboBloom,
        ObjetivoAprendizajeId = s.ObjetivoAprendizajeId,
        IndicadorEvaluacion = s.IndicadorEvaluacion,
        CriterioLogro = s.CriterioLogro,
        MinutosEstimados = s.MinutosEstimados
    };

    public static Documento FromGemini(
        GenerarDocumentoRequest request,
        GeminiContentDto content,
        Nivel nivel,
        Asignatura asignatura,
        Unidad unidad,
        IReadOnlyList<ObjetivoAprendizaje> oas,
        IReadOnlyDictionary<string, Guid> indicadorLookup)
    {
        var docId = Guid.NewGuid();
        var oaTexto = string.Join("; ", oas.Select(o => $"{o.Codigo}: {o.Descripcion}"));
        var oaPrincipal = oas.First().Id;

        var items = MapItems(content, request, docId, indicadorLookup);
        var sesiones = MapSesiones(content, request, docId, oaPrincipal, indicadorLookup);

        var instrucciones = content.Instrucciones;
        if (!string.IsNullOrWhiteSpace(content.PropositoUnidad) || !string.IsNullOrWhiteSpace(content.HabilidadFocal))
        {
            var extra = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(content.PropositoUnidad))
                extra.AppendLine($"Propósito de la unidad: {content.PropositoUnidad}");
            if (!string.IsNullOrWhiteSpace(content.HabilidadFocal))
                extra.AppendLine($"Habilidad que se complejiza: {content.HabilidadFocal}");
            if (!string.IsNullOrWhiteSpace(instrucciones))
                extra.AppendLine().Append(instrucciones);
            instrucciones = extra.ToString().Trim();
        }

        return new Documento
        {
            Id = docId,
            Tipo = request.Tipo,
            NivelId = nivel.Id,
            AsignaturaId = asignatura.Id,
            UnidadId = unidad.Id,
            Nivel = nivel.Nombre,
            Asignatura = asignatura.Nombre,
            Unidad = $"{unidad.Numero}. {unidad.Nombre}",
            Tema = string.IsNullOrWhiteSpace(request.Tema) ? unidad.Nombre : request.Tema.Trim(),
            ObjetivoAprendizaje = oaTexto,
            Instrucciones = instrucciones,
            ContenidoGenerado = JsonSerializer.Serialize(content, JsonOptions),
            FechaCreacion = DateTime.UtcNow,
            Estado = EstadoDocumento.Borrador,
            ObjetivosSeleccionados = oas.Select(o => new DocumentoObjetivoAprendizaje
            {
                DocumentoId = docId,
                ObjetivoAprendizajeId = o.Id
            }).ToList(),
            Items = items,
            Sesiones = sesiones
        };
    }

    private static List<Item> MapItems(
        GeminiContentDto content,
        GenerarDocumentoRequest request,
        Guid docId,
        IReadOnlyDictionary<string, Guid> indicadorLookup)
    {
        if (request.Tipo == TipoDocumento.PlanificacionUnidad || content.Items.Count == 0)
            return [];

        var geminiItems = content.Items.ToList();
        if (request.UsarTaxonomiaBloom)
        {
            geminiItems = geminiItems
                .OrderBy(i => NivelBloomHelper.Orden(i.NivelBloom))
                .ThenBy(i => i.Enunciado)
                .ToList();
        }

        return geminiItems.Select((g, index) => new Item
        {
            Id = Guid.NewGuid(),
            DocumentoId = docId,
            Tipo = ParseTipoItem(g.Tipo),
            Enunciado = g.Enunciado,
            AlternativasJson = JsonSerializer.Serialize(g.Alternativas ?? []),
            RespuestaCorrecta = g.RespuestaCorrecta,
            Puntaje = g.Puntaje > 0 ? g.Puntaje : 1,
            Orden = index + 1,
            IndicadorEvaluacionId = ResolverIndicadorId(g.IndicadorCodigoODescripcion, indicadorLookup),
            NivelBloom = NivelBloomHelper.Normalizar(g.NivelBloom),
            VerboBloom = g.VerboBloom
        }).ToList();
    }

    private static List<SesionPlanificada> MapSesiones(
        GeminiContentDto content,
        GenerarDocumentoRequest request,
        Guid docId,
        Guid oaPrincipalId,
        IReadOnlyDictionary<string, Guid> _)
    {
        if (request.Tipo != TipoDocumento.PlanificacionUnidad || content.Sesiones.Count == 0)
            return [];

        return content.Sesiones
            .OrderBy(s => s.Numero > 0 ? s.Numero : 99)
            .Select((s, index) => new SesionPlanificada
            {
                Id = Guid.NewGuid(),
                DocumentoId = docId,
                Numero = index + 1,
                Descripcion = string.IsNullOrWhiteSpace(s.Descripcion) ? $"Sesión {index + 1}" : s.Descripcion.Trim(),
                Actividades = string.IsNullOrWhiteSpace(s.Actividades) ? s.Descripcion : s.Actividades.Trim(),
                NivelBloom = NivelBloomHelper.Normalizar(s.NivelBloom),
                VerboBloom = s.VerboBloom,
                ObjetivoAprendizajeId = oaPrincipalId,
                IndicadorEvaluacion = s.IndicadorCodigoODescripcion,
                CriterioLogro = s.CriterioLogro,
                MinutosEstimados = s.MinutosEstimados is > 0 ? s.MinutosEstimados : 45
            }).ToList();
    }

    private static Guid? ResolverIndicadorId(string? texto, IReadOnlyDictionary<string, Guid> indicadorLookup)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var key = texto.Trim();
        if (indicadorLookup.TryGetValue(key, out var id))
            return id;

        var match = indicadorLookup.FirstOrDefault(kv =>
            key.Contains(kv.Key, StringComparison.OrdinalIgnoreCase) ||
            kv.Key.Contains(key, StringComparison.OrdinalIgnoreCase));
        return match.Key is null ? null : match.Value;
    }

    public static TipoItem ParseTipoItem(string tipo) => tipo.Trim().ToLowerInvariant() switch
    {
        "desarrollo" or "actividad" or "tarea" => TipoItem.Desarrollo,
        "verdadero_falso" or "verdaderofalso" or "vf" => TipoItem.VerdaderoFalso,
        "seleccion_multiple" or "sm" or "alternativa" => TipoItem.SeleccionMultiple,
        _ => TipoItem.Desarrollo
    };

    public static string FormatearContenidoEditable(GeminiContentDto content, string? unidad = null, string? oas = null)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(content.Titulo);
        if (!string.IsNullOrWhiteSpace(unidad))
            sb.AppendLine($"Unidad: {unidad}");
        if (!string.IsNullOrWhiteSpace(oas))
            sb.AppendLine($"OA: {oas}");
        if (!string.IsNullOrWhiteSpace(content.PropositoUnidad))
            sb.AppendLine($"Propósito: {content.PropositoUnidad}");
        if (!string.IsNullOrWhiteSpace(content.HabilidadFocal))
            sb.AppendLine($"Habilidad focal: {content.HabilidadFocal}");
        sb.AppendLine();
        sb.AppendLine(content.Instrucciones);
        sb.AppendLine();

        if (content.Sesiones.Count > 0)
        {
            sb.AppendLine($"Planificación: {content.Sesiones.Count} sesiones (progresión Bloom)");
            sb.AppendLine();
            foreach (var s in content.Sesiones.OrderBy(x => x.Numero))
            {
                var bloom = NivelBloomHelper.Normalizar(s.NivelBloom) ?? "Sin nivel Bloom";
                sb.AppendLine($"=== SESIÓN {s.Numero} · {bloom.ToUpperInvariant()} ===");
                if (!string.IsNullOrWhiteSpace(s.VerboBloom))
                    sb.AppendLine($"Verbo: {s.VerboBloom}");
                sb.AppendLine(s.Descripcion);
                sb.AppendLine();
                sb.AppendLine("Actividades:");
                sb.AppendLine(s.Actividades);
                if (!string.IsNullOrWhiteSpace(s.IndicadorCodigoODescripcion))
                    sb.AppendLine($"→ Indicador: {s.IndicadorCodigoODescripcion}");
                if (!string.IsNullOrWhiteSpace(s.CriterioLogro))
                    sb.AppendLine($"Criterio de logro: {s.CriterioLogro}");
                if (s.MinutosEstimados is > 0)
                    sb.AppendLine($"Tiempo estimado: {s.MinutosEstimados} min");
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        sb.AppendLine($"Secuencia: {content.Items.Count} actividades (progresión Bloom)");
        sb.AppendLine();

        string? bloomActual = null;
        var ordenados = content.Items
            .Select((item, i) => (item, i))
            .OrderBy(x => NivelBloomHelper.Orden(x.item.NivelBloom))
            .ThenBy(x => x.i)
            .Select(x => x.item)
            .ToList();

        var n = 0;
        foreach (var item in ordenados)
        {
            n++;
            var bloom = NivelBloomHelper.Normalizar(item.NivelBloom) ?? "Sin nivel Bloom";
            if (!string.Equals(bloom, bloomActual, StringComparison.OrdinalIgnoreCase))
            {
                bloomActual = bloom;
                sb.AppendLine($"=== {bloom.ToUpperInvariant()} ===");
                if (!string.IsNullOrWhiteSpace(item.VerboBloom))
                    sb.AppendLine($"(verbo: {item.VerboBloom})");
                sb.AppendLine();
            }

            var pts = item.Puntaje > 0 ? item.Puntaje : 1;
            sb.AppendLine($"{n}. [{bloom}] {item.Enunciado} ({pts} pt)");
            foreach (var alt in item.Alternativas)
                sb.AppendLine($"   {alt}");
            if (!string.IsNullOrWhiteSpace(item.IndicadorCodigoODescripcion))
                sb.AppendLine($"   → Indicador OA: {item.IndicadorCodigoODescripcion}");
            sb.AppendLine();
        }

        if (ordenados.Any(i => !string.IsNullOrWhiteSpace(i.RespuestaCorrecta)))
        {
            sb.AppendLine("--- Clave / orientaciones de corrección (docente) ---");
            for (var i = 0; i < ordenados.Count; i++)
                sb.AppendLine($"{i + 1}. {ordenados[i].RespuestaCorrecta ?? "(abierta)"}");
        }

        return sb.ToString().TrimEnd();
    }
}
