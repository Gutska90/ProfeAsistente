using System.Text.Json;
using ProfeAsistente.Api.Models;
using ProfeAsistente.Shared.Enums;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OpenXmlDoc = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace ProfeAsistente.Api.Services;

public interface IExportService
{
    byte[] ExportarDocx(Documento documento, bool incluirClave = false);
    byte[] ExportarPdf(Documento documento, bool incluirClave = false);
    byte[] ExportarPlanificacionDocx(Planificacion planificacion);
}

public class ExportService : IExportService
{
    static ExportService() => QuestPDF.Settings.License = LicenseType.Community;

    public byte[] ExportarPlanificacionDocx(Planificacion planificacion)
    {
        using var stream = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new OpenXmlDoc(new Body());
            var body = mainPart.Document.Body!;

            body.AppendChild(P(planificacion.Nombre, bold: true, size: "32", center: true));
            var asigNombre = planificacion.NivelAsignatura?.NombreEnNivel
                             ?? planificacion.NivelAsignatura?.Asignatura?.Nombre
                             ?? "";
            body.AppendChild(P(
                $"{asigNombre} — {planificacion.Nivel?.Nombre}",
                size: "20", center: true));
            if (planificacion.Unidad is not null)
                body.AppendChild(P(
                    $"Unidad {planificacion.Unidad.Numero}: {planificacion.Unidad.Nombre}",
                    size: "18", center: true));
            body.AppendChild(P(
                $"Período: {planificacion.FechaInicio:dd-MM-yyyy} a {planificacion.FechaFin:dd-MM-yyyy}",
                size: "18", center: true));
            body.AppendChild(P("Docente: ____________________________    Curso: ____________", size: "20"));
            body.AppendChild(Espacio());

            var clases = (planificacion.Clases ?? []).OrderBy(c => c.Numero).ToList();
            body.AppendChild(P($"Total clases: {clases.Count}", size: "18"));
            body.AppendChild(Espacio());

            foreach (var c in clases)
            {
                var oa = c.ObjetivoAprendizaje;
                var bloom = NivelBloomHelper.Normalizar(c.NivelBloom) ?? c.NivelBloom;
                body.AppendChild(P(
                    $"Clase {c.Numero} — {c.Fecha:dd-MM-yyyy} — [{bloom}]",
                    bold: true, size: "24"));
                if (oa is not null)
                    body.AppendChild(P($"OA: {oa.Codigo} — {oa.Descripcion}", italic: true, size: "18"));

                body.AppendChild(P("Inicio:", bold: true, size: "20"));
                body.AppendChild(P(string.IsNullOrWhiteSpace(c.DescripcionInicio) ? "(sin generar)" : c.DescripcionInicio!, size: "20"));
                body.AppendChild(P("Desarrollo:", bold: true, size: "20"));
                body.AppendChild(P(string.IsNullOrWhiteSpace(c.DescripcionDesarrollo) ? "(sin generar)" : c.DescripcionDesarrollo!, size: "20"));
                body.AppendChild(P("Cierre:", bold: true, size: "20"));
                body.AppendChild(P(string.IsNullOrWhiteSpace(c.DescripcionCierre) ? "(sin generar)" : c.DescripcionCierre!, size: "20"));

                var mats = (c.Documentos ?? []).Select(d => d.Tipo.ToString()).Distinct().ToList();
                if (mats.Count > 0)
                    body.AppendChild(P($"Material: {string.Join(", ", mats)}", size: "18"));
                body.AppendChild(Espacio());
            }

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    public byte[] ExportarDocx(Documento documento, bool incluirClave = false)
    {
        using var stream = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new OpenXmlDoc(new Body());
            var body = mainPart.Document.Body!;
            var meta = ExtraerMeta(documento);
            var etiquetas = EtiquetasPorTipo(documento.Tipo);

            body.AppendChild(P(meta.Titulo, bold: true, size: "32", center: true));
            body.AppendChild(P($"{documento.Asignatura} — {documento.Nivel}", size: "20", center: true));
            if (!string.IsNullOrWhiteSpace(documento.Unidad))
                body.AppendChild(P($"Unidad: {documento.Unidad}", size: "18", center: true));
            body.AppendChild(P($"Tipo: {etiquetas.NombreTipo}", size: "18", center: true));
            body.AppendChild(Espacio());

            // En planificación no pedimos "nombre del alumno" como prueba; sí espacio docente
            if (documento.Tipo == TipoDocumento.PlanificacionUnidad)
                body.AppendChild(P("Docente: ____________________________    Curso: ____________", size: "20"));
            else
                body.AppendChild(P("Nombre: ________________________________    Fecha: ____________", size: "20"));
            body.AppendChild(Espacio());

            if (!string.IsNullOrWhiteSpace(documento.ObjetivoAprendizaje))
            {
                body.AppendChild(P($"OA: {documento.ObjetivoAprendizaje}", italic: true, size: "18"));
                body.AppendChild(Espacio());
            }

            var instrucciones = !string.IsNullOrWhiteSpace(documento.Instrucciones)
                ? documento.Instrucciones
                : meta.Instrucciones;
            if (!string.IsNullOrWhiteSpace(instrucciones))
            {
                body.AppendChild(P(instrucciones!, size: "20"));
                body.AppendChild(Espacio());
            }

            var sesiones = OrdenarSesiones(documento).ToList();
            if (documento.Tipo == TipoDocumento.PlanificacionUnidad && sesiones.Count > 0)
            {
                body.AppendChild(P($"Sesiones: {sesiones.Count}  |  Progresión Bloom sesión a sesión", size: "18"));
                body.AppendChild(Espacio());

                foreach (var s in sesiones)
                {
                    var bloom = NivelBloomHelper.Normalizar(s.NivelBloom) ?? "Sin nivel Bloom";
                    body.AppendChild(P(
                        $"Sesión {s.Numero} — [{bloom}]{(string.IsNullOrWhiteSpace(s.VerboBloom) ? "" : $" · {s.VerboBloom}")}",
                        bold: true, size: "24"));
                    body.AppendChild(P(s.Descripcion, size: "20"));
                    body.AppendChild(P("Actividades:", bold: true, size: "18"));
                    foreach (var line in s.Actividades.Split('\n'))
                        body.AppendChild(P(line.TrimEnd(), size: "20"));
                    if (!string.IsNullOrWhiteSpace(s.IndicadorEvaluacion))
                        body.AppendChild(P($"Indicador: {s.IndicadorEvaluacion}", italic: true, size: "18"));
                    if (!string.IsNullOrWhiteSpace(s.CriterioLogro))
                        body.AppendChild(P($"Criterio de logro: {s.CriterioLogro}", size: "18"));
                    if (s.MinutosEstimados is > 0)
                        body.AppendChild(P($"Tiempo estimado: {s.MinutosEstimados} min", size: "18"));
                    body.AppendChild(Espacio());
                }
            }
            else
            {
                var ordenados = OrdenarItems(documento).ToList();
                if (ordenados.Count > 0)
                {
                    var totalPts = ordenados.Sum(i => i.Puntaje);
                    body.AppendChild(P(
                        $"{etiquetas.ResumenCantidad(ordenados.Count)}  |  Puntaje total: {totalPts}",
                        size: "18"));
                    body.AppendChild(Espacio());

                    string? bloomActual = null;
                    for (var i = 0; i < ordenados.Count; i++)
                    {
                        var item = ordenados[i];
                        var bloom = NivelBloomHelper.Normalizar(item.NivelBloom);
                        if (!string.IsNullOrWhiteSpace(bloom))
                        {
                            var etiqueta = bloom;
                            if (!string.Equals(etiqueta, bloomActual, StringComparison.OrdinalIgnoreCase))
                            {
                                bloomActual = etiqueta;
                                body.AppendChild(P($"— {etiqueta.ToUpperInvariant()} —", bold: true, size: "24"));
                                if (!string.IsNullOrWhiteSpace(item.VerboBloom))
                                    body.AppendChild(P($"Verbo: {item.VerboBloom}", italic: true, size: "18"));
                                body.AppendChild(Espacio());
                            }
                        }

                        body.AppendChild(P(
                            $"{i + 1}. {item.Enunciado}  ({item.Puntaje} pt{(item.Puntaje == 1 ? "" : "s")})",
                            bold: true, size: "20"));

                        var alts = DeserializeAlternativas(item.AlternativasJson);
                        if (item.Tipo == TipoItem.VerdaderoFalso && alts.Count == 0)
                            alts = ["Verdadero", "Falso"];

                        foreach (var alt in alts)
                            body.AppendChild(P($"    ○  {alt}", size: "20"));

                        if (item.Tipo == TipoItem.Desarrollo)
                        {
                            body.AppendChild(P("________________________________________________________________", size: "20"));
                            body.AppendChild(P("________________________________________________________________", size: "20"));
                        }

                        body.AppendChild(Espacio());
                    }

                    if (incluirClave)
                        AgregarClaveDocx(body, ordenados);
                }
                else if (!string.IsNullOrWhiteSpace(meta.TextoPlano))
                {
                    foreach (var line in meta.TextoPlano.Split('\n'))
                        body.AppendChild(P(line.TrimEnd(), size: "20"));
                }
            }

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    public byte[] ExportarPdf(Documento documento, bool incluirClave = false)
    {
        var meta = ExtraerMeta(documento);
        var items = OrdenarItems(documento).ToList();
        var sesiones = OrdenarSesiones(documento).ToList();
        var etiquetas = EtiquetasPorTipo(documento.Tipo);
        var instrucciones = !string.IsNullOrWhiteSpace(documento.Instrucciones)
            ? documento.Instrucciones
            : meta.Instrucciones;
        var esPlan = documento.Tipo == TipoDocumento.PlanificacionUnidad && sesiones.Count > 0;

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.Letter);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Helvetica"));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(meta.Titulo).Bold().FontSize(16);
                    col.Item().AlignCenter().Text($"{documento.Asignatura} — {documento.Nivel}")
                        .FontSize(10).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrWhiteSpace(documento.Unidad))
                        col.Item().AlignCenter().Text($"Unidad: {documento.Unidad}").FontSize(10);
                    col.Item().AlignCenter().Text($"Tipo: {etiquetas.NombreTipo}")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(10).Text(
                        documento.Tipo == TipoDocumento.PlanificacionUnidad
                            ? "Docente: ____________________________    Curso: ____________"
                            : "Nombre: _______________________________    Fecha: ____________");
                    if (!string.IsNullOrWhiteSpace(documento.ObjetivoAprendizaje))
                        col.Item().PaddingTop(6).Text($"OA: {documento.ObjetivoAprendizaje}").Italic().FontSize(9);
                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    if (!string.IsNullOrWhiteSpace(instrucciones))
                        col.Item().Text(instrucciones);

                    if (esPlan)
                    {
                        col.Item().Text($"Sesiones: {sesiones.Count}  |  Progresión Bloom")
                            .FontSize(10).FontColor(Colors.Grey.Darken2);

                        foreach (var s in sesiones)
                        {
                            var bloom = NivelBloomHelper.Normalizar(s.NivelBloom) ?? "Sin nivel Bloom";
                            col.Item().PaddingTop(8).Text(
                                    $"Sesión {s.Numero} — [{bloom}]{(string.IsNullOrWhiteSpace(s.VerboBloom) ? "" : $" · {s.VerboBloom}")}")
                                .Bold().FontSize(12);
                            col.Item().Text(s.Descripcion);
                            col.Item().Text("Actividades:").Bold().FontSize(10);
                            col.Item().Text(s.Actividades);
                            if (!string.IsNullOrWhiteSpace(s.IndicadorEvaluacion))
                                col.Item().Text($"Indicador: {s.IndicadorEvaluacion}").Italic().FontSize(9);
                            if (!string.IsNullOrWhiteSpace(s.CriterioLogro))
                                col.Item().Text($"Criterio: {s.CriterioLogro}").FontSize(9);
                            if (s.MinutosEstimados is > 0)
                                col.Item().Text($"Tiempo: {s.MinutosEstimados} min").FontSize(9);
                        }

                        return;
                    }

                    if (items.Count > 0)
                    {
                        col.Item().Text($"{etiquetas.ResumenCantidad(items.Count)}  |  Puntaje: {items.Sum(i => i.Puntaje)}")
                            .FontSize(10).FontColor(Colors.Grey.Darken2);
                    }

                    if (items.Count == 0 && !string.IsNullOrWhiteSpace(meta.TextoPlano))
                    {
                        col.Item().Text(meta.TextoPlano);
                        return;
                    }

                    string? bloomActual = null;
                    for (var i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        var index = i;
                        var bloom = NivelBloomHelper.Normalizar(item.NivelBloom);
                        if (!string.IsNullOrWhiteSpace(bloom))
                        {
                            var etiqueta = bloom;
                            if (!string.Equals(etiqueta, bloomActual, StringComparison.OrdinalIgnoreCase))
                            {
                                bloomActual = etiqueta;
                                col.Item().PaddingTop(6).Text(etiqueta.ToUpperInvariant()).Bold().FontSize(12)
                                    .FontColor(Colors.Blue.Darken3);
                                if (!string.IsNullOrWhiteSpace(item.VerboBloom))
                                    col.Item().Text($"Verbo: {item.VerboBloom}").Italic().FontSize(9);
                            }
                        }

                        col.Item().Column(itemCol =>
                        {
                            itemCol.Item().Text(t =>
                            {
                                t.Span($"{index + 1}. ").Bold();
                                if (!string.IsNullOrWhiteSpace(bloom))
                                    t.Span($"[{bloom}] ").FontColor(Colors.Grey.Darken1);
                                t.Span(item.Enunciado).Bold();
                                t.Span($"  ({item.Puntaje} pt)").FontColor(Colors.Grey.Darken1);
                            });

                            var alts = DeserializeAlternativas(item.AlternativasJson);
                            if (item.Tipo == TipoItem.VerdaderoFalso && alts.Count == 0)
                                alts = ["Verdadero", "Falso"];

                            foreach (var alt in alts)
                                itemCol.Item().PaddingLeft(16).Text($"○  {alt}");

                            if (item.Tipo == TipoItem.Desarrollo)
                            {
                                itemCol.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                                itemCol.Item().PaddingTop(14).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("ProfeAsistente · Currículum Nacional MINEDUC · pág. ");
                    t.CurrentPageNumber();
                });
            });

            if (incluirClave && items.Count > 0 && !esPlan)
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.Letter);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Helvetica"));
                    page.Content().Column(col =>
                    {
                        col.Spacing(6);
                        col.Item().Text("CLAVE DE CORRECCIÓN — SOLO DOCENTE").Bold().FontSize(14).FontColor(Colors.Red.Darken2);
                        col.Item().Text(meta.Titulo);
                        col.Item().PaddingBottom(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        for (var i = 0; i < items.Count; i++)
                            col.Item().Text($"{i + 1}. {items[i].RespuestaCorrecta ?? "(sin clave)"}  ({items[i].Puntaje} pt)");
                    });
                });
            }
        }).GeneratePdf();
    }

    private static IEnumerable<Item> OrdenarItems(Documento documento)
    {
        var items = documento.Items.AsEnumerable();
        if (documento.Items.Any(i => !string.IsNullOrWhiteSpace(i.NivelBloom)))
        {
            return items
                .OrderBy(i => NivelBloomHelper.Orden(i.NivelBloom))
                .ThenBy(i => i.Orden);
        }

        return items.OrderBy(i => i.Orden);
    }

    private static IEnumerable<SesionPlanificada> OrdenarSesiones(Documento documento) =>
        (documento.Sesiones ?? []).OrderBy(s => s.Numero);

    private static (string NombreTipo, Func<int, string> ResumenCantidad) EtiquetasPorTipo(TipoDocumento tipo) =>
        tipo switch
        {
            TipoDocumento.PlanificacionUnidad => ("Planificación de unidad (Bloom)", n => $"Sesiones: {n}"),
            TipoDocumento.Prueba => ("Prueba", n => $"Total: {n} pregunta{(n == 1 ? "" : "s")}"),
            TipoDocumento.Ejercicios => ("Ejercicios", n => $"Total: {n} ejercicio{(n == 1 ? "" : "s")}"),
            _ => ("Secuencia de actividades", n => $"Total: {n} actividad{(n == 1 ? "" : "es")}")
        };

    private static void AgregarClaveDocx(Body body, List<Item> ordenados)
    {
        body.AppendChild(P("CLAVE DE CORRECCIÓN — SOLO DOCENTE", bold: true, size: "24"));
        body.AppendChild(P("(Separar esta hoja antes de entregar al estudiante.)", italic: true, size: "18"));
        body.AppendChild(Espacio());
        for (var i = 0; i < ordenados.Count; i++)
            body.AppendChild(P($"{i + 1}. {ordenados[i].RespuestaCorrecta ?? "(sin clave)"}  ({ordenados[i].Puntaje} pt)", size: "20"));
    }

    private static List<string> DeserializeAlternativas(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return []; }
    }

    private static (string Titulo, string? Instrucciones, string? TextoPlano) ExtraerMeta(Documento documento)
    {
        var contenido = documento.ContenidoGenerado ?? string.Empty;
        string? textoPlano = contenido;
        string? jsonPart = null;

        const string marker = "\n---\n";
        var idx = contenido.IndexOf(marker, StringComparison.Ordinal);
        if (idx >= 0)
        {
            textoPlano = contenido[..idx].Trim();
            jsonPart = contenido[(idx + marker.Length)..].Trim();
        }
        else if (contenido.TrimStart().StartsWith('{'))
        {
            jsonPart = contenido.Trim();
            textoPlano = null;
        }

        var titulo = string.IsNullOrWhiteSpace(documento.Tema) ? "Material educativo" : documento.Tema;
        string? instrucciones = documento.Instrucciones;

        if (!string.IsNullOrWhiteSpace(jsonPart))
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonPart);
                var root = doc.RootElement;
                if (root.TryGetProperty("titulo", out var t) || root.TryGetProperty("Titulo", out t))
                    titulo = t.GetString() ?? titulo;
                if (string.IsNullOrWhiteSpace(instrucciones) &&
                    (root.TryGetProperty("instrucciones", out var i) || root.TryGetProperty("Instrucciones", out i)))
                    instrucciones = i.GetString();
            }
            catch { /* ignore */ }
        }

        return (titulo, instrucciones, textoPlano);
    }

    private static Paragraph P(string text, bool bold = false, bool italic = false, string size = "22", bool center = false)
    {
        var runProps = new RunProperties(
            new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
            new FontSize { Val = size });
        if (bold) runProps.AppendChild(new Bold());
        if (italic) runProps.AppendChild(new Italic());

        var paraProps = new ParagraphProperties(new SpacingBetweenLines { After = "120" });
        if (center)
            paraProps.AppendChild(new Justification { Val = JustificationValues.Center });

        return new Paragraph(paraProps, new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static Paragraph Espacio() =>
        new(new ParagraphProperties(new SpacingBetweenLines { After = "200" }), new Run(new Text("")));
}
