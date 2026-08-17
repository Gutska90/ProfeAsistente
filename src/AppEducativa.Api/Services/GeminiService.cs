using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AppEducativa.Api.Models;
using AppEducativa.Api.Models.Curriculum;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GeminiService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GeminiService(HttpClient http, IConfiguration config, ILogger<GeminiService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<GeminiContentDto> GenerarContenidoAsync(
        GenerarDocumentoRequest request,
        CurriculumGeneracionContext contexto,
        CancellationToken ct = default)
    {
        NormalizarCantidades(request);
        var esPlan = request.Tipo == TipoDocumento.PlanificacionUnidad;
        if (esPlan)
            request.UsarTaxonomiaBloom = true;

        var soloSm = request.SoloSeleccionMultiple;
        var cantidad = esPlan
            ? (request.CantidadSesiones ?? request.CantidadItems)
            : request.CantidadItems;

        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("GEMINI_API_KEY no configurada. Contenido de demostración.");
            return CrearContenidoDemo(request, contexto, soloSm, cantidad);
        }

        var model = _config["Gemini:Model"] ?? "gemini-3.1-flash-lite";
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
        var prompt = ConstruirPrompt(request, contexto, soloSm, cantidad);

        var payload = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.7, responseMimeType = "application/json" }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(httpRequest, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Error Gemini ({Status}): {Body}", response.StatusCode, errorBody);
            throw new InvalidOperationException($"Error al llamar a Gemini: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var generatedText = ExtraerTextoRespuesta(responseJson);
        if (string.IsNullOrWhiteSpace(generatedText))
            throw new InvalidOperationException("Gemini no devolvió contenido.");

        var parsed = JsonSerializer.Deserialize<GeminiContentDto>(LimpiarJson(generatedText), JsonOptions)
            ?? throw new InvalidOperationException("No se pudo interpretar la respuesta de Gemini como JSON válido.");

        if (esPlan)
        {
            if (parsed.Sesiones.Count == 0 && parsed.Items.Count > 0)
                parsed.Sesiones = ConvertirItemsASesiones(parsed.Items);

            if (parsed.Sesiones.Count == 0)
                throw new InvalidOperationException("Gemini no devolvió sesiones para la planificación.");

            if (parsed.Sesiones.Count > cantidad)
                parsed.Sesiones = parsed.Sesiones.Take(cantidad).ToList();

            for (var i = 0; i < parsed.Sesiones.Count; i++)
            {
                var s = parsed.Sesiones[i];
                s.Numero = i + 1;
                s.NivelBloom = NivelBloomHelper.Normalizar(s.NivelBloom);
            }

            AsegurarProgresionBloomSesiones(parsed.Sesiones);
            parsed.Items = [];
        }
        else
        {
            if (parsed.Items.Count == 0)
                throw new InvalidOperationException("Gemini no devolvió ítems.");

            if (parsed.Items.Count > cantidad)
                parsed.Items = parsed.Items.Take(cantidad).ToList();

            foreach (var item in parsed.Items)
                item.NivelBloom = NivelBloomHelper.Normalizar(item.NivelBloom);

            if (soloSm)
            {
                foreach (var item in parsed.Items)
                {
                    item.Tipo = "seleccion_multiple";
                    while (item.Alternativas.Count < 4)
                        item.Alternativas.Add($"{(char)('A' + item.Alternativas.Count)}) Opción");
                }
            }
        }

        return parsed;
    }

    private static void NormalizarCantidades(GenerarDocumentoRequest request)
    {
        if (request.Tipo == TipoDocumento.PlanificacionUnidad)
        {
            var n = request.CantidadSesiones ?? request.CantidadItems;
            n = Math.Clamp(n <= 0 ? 6 : n, 2, 12);
            request.CantidadSesiones = n;
            request.CantidadItems = n;
        }
        else
        {
            request.CantidadItems = Math.Clamp(request.CantidadItems <= 0 ? 5 : request.CantidadItems, 1, 30);
        }
    }

    private static List<GeminiSesionDto> ConvertirItemsASesiones(List<GeminiItemDto> items)
    {
        return items.Select((item, i) => new GeminiSesionDto
        {
            Numero = i + 1,
            Descripcion = item.Enunciado,
            Actividades = item.Enunciado,
            NivelBloom = item.NivelBloom,
            VerboBloom = item.VerboBloom,
            IndicadorCodigoODescripcion = item.IndicadorCodigoODescripcion,
            CriterioLogro = item.RespuestaCorrecta,
            MinutosEstimados = 45
        }).ToList();
    }

    /// <summary>Si el modelo no respeta la progresión, reasigna Bloom en orden ascendente.</summary>
    private static void AsegurarProgresionBloomSesiones(List<GeminiSesionDto> sesiones)
    {
        var nombres = NivelBloomHelper.Nombres;
        for (var i = 0; i < sesiones.Count; i++)
        {
            var esperado = nombres[Math.Min(i, nombres.Count - 1)];
            var actual = NivelBloomHelper.Normalizar(sesiones[i].NivelBloom);
            if (i == 0)
            {
                sesiones[i].NivelBloom = actual ?? esperado;
                continue;
            }

            var prev = NivelBloomHelper.Orden(sesiones[i - 1].NivelBloom);
            var cur = NivelBloomHelper.Orden(actual);
            if (actual is null || cur <= prev)
                sesiones[i].NivelBloom = esperado;
            else
                sesiones[i].NivelBloom = actual;
        }
    }

    private static string ConstruirPrompt(
        GenerarDocumentoRequest request, CurriculumGeneracionContext ctx, bool soloSm, int cantidad)
    {
        var esPlan = request.Tipo == TipoDocumento.PlanificacionUnidad;

        var tipoNombre = request.Tipo switch
        {
            TipoDocumento.PlanificacionUnidad => "planificación de unidad (secuencia de sesiones/clases)",
            TipoDocumento.Prueba when soloSm => "prueba de selección múltiple",
            TipoDocumento.Prueba => "prueba / evaluación",
            TipoDocumento.Ejercicios => "ficha de ejercicios con progresión de complejidad",
            _ => "secuencia de actividades alineada al OA"
        };

        var oaBlock = string.Join("\n", ctx.Objetivos.Select(o =>
        {
            var inds = o.Indicadores.Count == 0
                ? "  (sin indicadores cargados)"
                : string.Join("\n", o.Indicadores.Select(i => $"  - {i.Descripcion}"));
            return $"- {o.Codigo}: {o.Descripcion}\n  Indicadores de evaluación:\n{inds}";
        }));

        var contenidos = ctx.Contenidos.Count == 0 ? "(no especificados)" : string.Join("; ", ctx.Contenidos);
        var habilidades = ctx.Habilidades.Count == 0 ? "(no especificadas)" : string.Join("; ", ctx.Habilidades);
        var foco = string.IsNullOrWhiteSpace(request.Tema) ? "(ninguno; basarse en OA y unidad)" : request.Tema;

        if (esPlan)
        {
            var esquemaSesiones = """
                {
                  "titulo": "string",
                  "propositoUnidad": "string — para qué sirve esta planificación respecto del OA",
                  "habilidadFocal": "string — habilidad que se complejiza sesión a sesión",
                  "instrucciones": "string — orientaciones breves al docente",
                  "sesiones": [
                    {
                      "numero": 1,
                      "descripcion": "enfoque de la clase (qué se trabaja)",
                      "actividades": "secuencia de actividades concretas de la sesión",
                      "nivelBloom": "Recordar",
                      "verboBloom": "identificar",
                      "indicadorCodigoODescripcion": "texto del indicador OA",
                      "criterioLogro": "cómo se evidencia el logro en esta sesión",
                      "minutosEstimados": 45
                    }
                  ],
                  "items": []
                }
                """;

            return $"""
                Eres un asesor pedagógico experto en Currículum Nacional chileno (MINEDUC) y Taxonomía de Bloom.
                Genera una {tipoNombre} en español chileno.

                Los profesores NO necesitan material "bonito". Necesitan planificar la unidad para abordar el OA
                y complejizar la habilidad con Bloom (Recordar → Comprender → Aplicar → Analizar → Evaluar → Crear).

                MARCO CURRICULAR (obligatorio — NO inventes códigos OA ni indicadores fuera de esta lista):
                - Nivel: {ctx.Nivel.Nombre} ({ctx.Nivel.Ciclo})
                - Asignatura: {ctx.Asignatura.Nombre}
                - Unidad {ctx.Unidad.Numero}: {ctx.Unidad.Nombre}
                  {ctx.Unidad.Descripcion}
                - Contenidos / foco de unidad: {contenidos}
                - Habilidades asignatura: {habilidades}
                - OA seleccionados (todas las sesiones apuntan a estos OA):
                {oaBlock}
                - Foco opcional: {foco}

                REGLAS DE PLANIFICACIÓN (obligatorias):
                - Usa SOLO los códigos OA e indicadores del JSON de entrada; no inventes OA.
                - EXACTAMENTE {cantidad} sesiones (clases), numeradas 1..{cantidad}.
                - Todas las sesiones trabajan el MISMO OA (o los OA seleccionados).
                - Cada sesión DEBE tener un nivelBloom ESTRICTAMENTE MAYOR que la sesión anterior
                  (progresión: Recordar → Comprender → Aplicar → Analizar → Evaluar → Crear).
                  Si hay más de 6 sesiones, puedes repetir el nivel Crear en las últimas, pero NUNCA bajar.
                - Cada sesión incluye: descripcion, actividades, nivelBloom, verboBloom, indicador, criterioLogro, minutosEstimados.
                - Las actividades deben apuntar explícitamente a los indicadores de evaluación.
                - Incluye propositoUnidad y habilidadFocal.
                - NO priorices formato visual; prioriza coherencia OA → indicador → Bloom → sesión.

                Responde SOLO JSON válido con el array "sesiones" (items vacío):
                {esquemaSesiones}
                """;
        }

        var reglas = request.UsarTaxonomiaBloom
            ? $"""
              ENFOQUE PEDAGÓGICO:
              - El material debe ABORDAR el/los OA y sus indicadores.
              - EXACTAMENTE {cantidad} ítems, con distribución de niveles Bloom (no todos iguales).
              - Cada ítem DEBE incluir: nivelBloom, verboBloom, indicadorCodigoODescripcion.
              - Incluye propositoUnidad y habilidadFocal.
              """
            : soloSm
                ? $"""
                  REGLAS (prueba SM):
                  - EXACTAMENTE {cantidad} ítems SM con 4 alternativas.
                  - Cada ítem apunta a un indicador del OA.
                  """
                : $"""
                  REGLAS:
                  - EXACTAMENTE {cantidad} ítems alineados a indicadores del OA.
                  - Usa Taxonomía de Bloom para graduar complejidad (nivelBloom + verboBloom).
                  """;

        const string esquemaJson = """
            {
              "titulo": "string",
              "propositoUnidad": "string",
              "habilidadFocal": "string",
              "instrucciones": "string",
              "sesiones": [],
              "items": [
                {
                  "tipo": "actividad",
                  "nivelBloom": "Recordar|Comprender|Aplicar|Analizar|Evaluar|Crear",
                  "verboBloom": "identificar|explicar|resolver|comparar|justificar|diseñar",
                  "enunciado": "string con el verbo visible",
                  "alternativas": [],
                  "respuestaCorrecta": "criterio de logro o clave",
                  "puntaje": 1,
                  "indicadorCodigoODescripcion": "texto del indicador OA"
                }
              ]
            }
            """;

        return $"""
            Eres un asesor pedagógico experto en Currículum Nacional chileno (MINEDUC) y en Taxonomía de Bloom.
            Genera una {tipoNombre} en español chileno.

            MARCO CURRICULAR (obligatorio — NO inventes códigos OA ni indicadores fuera de esta lista):
            - Nivel: {ctx.Nivel.Nombre} ({ctx.Nivel.Ciclo})
            - Asignatura: {ctx.Asignatura.Nombre}
            - Unidad {ctx.Unidad.Numero}: {ctx.Unidad.Nombre}
              {ctx.Unidad.Descripcion}
            - Contenidos / foco de unidad: {contenidos}
            - Habilidades asignatura: {habilidades}
            - OA seleccionados:
            {oaBlock}
            - Foco opcional: {foco}

            {reglas}

            Responde SOLO JSON válido:
            {esquemaJson}
            """;
    }

    private static string? ExtraerTextoRespuesta(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return null;
        var parts = candidates[0].GetProperty("content").GetProperty("parts");
        return parts.GetArrayLength() == 0 ? null : parts[0].GetProperty("text").GetString();
    }

    private static string LimpiarJson(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```"))
        {
            trimmed = Regex.Replace(trimmed, @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
            trimmed = Regex.Replace(trimmed, @"\s*```$", "");
        }
        return trimmed.Trim();
    }

    private static GeminiContentDto CrearContenidoDemo(
        GenerarDocumentoRequest request, CurriculumGeneracionContext ctx, bool soloSm, int cantidad)
    {
        var oa = ctx.Objetivos.First();
        var ind = oa.Indicadores.FirstOrDefault()?.Descripcion ?? oa.Descripcion;
        var blooms = NivelBloomHelper.Nombres;
        var verbos = new[] { "Identificar", "Explicar", "Resolver", "Comparar", "Justificar", "Diseñar" };

        if (request.Tipo == TipoDocumento.PlanificacionUnidad)
        {
            var sesiones = new List<GeminiSesionDto>();
            for (var i = 0; i < cantidad; i++)
            {
                var bloom = blooms[Math.Min(i, blooms.Count - 1)];
                var verbo = verbos[Math.Min(i, verbos.Length - 1)];
                sesiones.Add(new GeminiSesionDto
                {
                    Numero = i + 1,
                    Descripcion = $"Sesión {i + 1}: {verbo.ToLowerInvariant()} aspectos de {oa.Codigo} ({bloom})",
                    Actividades =
                        $"1) Activación ({verbo}). 2) Desarrollo guiado sobre «{ctx.Unidad.Nombre}». 3) Cierre: evidencia del indicador.",
                    NivelBloom = bloom,
                    VerboBloom = verbo.ToLowerInvariant(),
                    IndicadorCodigoODescripcion = ind,
                    CriterioLogro = $"El estudiante evidencia «{ind}» en nivel {bloom}.",
                    MinutosEstimados = 45
                });
            }

            return new GeminiContentDto
            {
                Titulo = $"Planificación de unidad — {ctx.Unidad.Nombre}",
                PropositoUnidad = $"Abordar {oa.Codigo} con progresión Bloom sesión a sesión.",
                HabilidadFocal = ctx.Habilidades.FirstOrDefault() ?? "Habilidad del OA",
                Instrucciones = "Cada sesión sube un nivel Bloom. Mismo OA en toda la secuencia.",
                Sesiones = sesiones
            };
        }

        var items = new List<GeminiItemDto>();
        for (var i = 0; i < cantidad; i++)
        {
            var bloom = blooms[Math.Min(i, blooms.Count - 1)];
            var verbo = verbos[Math.Min(i, verbos.Length - 1)];
            items.Add(new GeminiItemDto
            {
                Tipo = soloSm ? "seleccion_multiple" : "actividad",
                NivelBloom = bloom,
                VerboBloom = verbo.ToLowerInvariant(),
                Enunciado = $"{verbo} un aspecto de «{oa.Codigo}» en el contexto de {ctx.Unidad.Nombre} (nivel Bloom: {bloom}).",
                Alternativas = soloSm
                    ? [$"A) Opción A-{i + 1}", $"B) Opción B-{i + 1}", $"C) Opción C-{i + 1}", $"D) Opción D-{i + 1}"]
                    : [],
                RespuestaCorrecta = soloSm ? $"A) Opción A-{i + 1}" : $"Criterio: evidencia el indicador «{ind}» en nivel {bloom}.",
                Puntaje = 1,
                IndicadorCodigoODescripcion = ind
            });
        }

        return new GeminiContentDto
        {
            Titulo = $"Secuencia Bloom — {ctx.Unidad.Nombre}",
            PropositoUnidad = $"Abordar {oa.Codigo} progresando la complejidad cognitiva.",
            HabilidadFocal = ctx.Habilidades.FirstOrDefault() ?? "Habilidad del OA seleccionada",
            Instrucciones = "Secuencia ordenada por Taxonomía de Bloom.",
            Items = items
        };
    }

    public async Task<EstructuraClaseDto> GenerarEstructuraClaseAsync(Clase clase, CancellationToken ct = default)
    {
        var plan = clase.Planificacion ?? throw new InvalidOperationException("La clase no tiene planificación cargada.");
        var oa = clase.ObjetivoAprendizaje ?? throw new InvalidOperationException("La clase no tiene OA cargado.");
        var indicadores = (clase.Indicadores ?? [])
            .Select(i => i.IndicadorEvaluacion?.Descripcion)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Cast<string>()
            .ToList();
        if (indicadores.Count == 0 && oa.Indicadores is not null)
            indicadores = oa.Indicadores.Select(i => i.Descripcion).ToList();

        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("GEMINI_API_KEY no configurada. Estructura de clase demo.");
            return CrearEstructuraDemo(clase, oa, indicadores);
        }

        var bloom = NivelBloomHelper.Normalizar(clase.NivelBloom) ?? "Aplicar";
        var inds = indicadores.Count == 0 ? "(sin indicadores)" : string.Join("\n- ", indicadores);
        var prompt = $$"""
            Eres un asesor pedagógico experto en Currículum Nacional chileno (MINEDUC).
            Genera la estructura de UNA clase en español chileno con formato Inicio / Desarrollo / Cierre.

            Contexto:
            - Nivel: {{plan.Nivel?.Nombre}}
            - Asignatura: {{plan.NivelAsignatura?.NombreEnNivel ?? plan.NivelAsignatura?.Asignatura?.Nombre}}
            - Unidad: {{plan.Unidad?.Nombre}}
            - Clase N° {{clase.Numero}} ({{clase.Fecha:yyyy-MM-dd}})
            - OA: {{oa.Codigo}} — {{oa.Descripcion}}
            - Nivel Bloom de ESTA clase: {{bloom}} (la demanda cognitiva debe coincidir con este nivel)
            - Indicadores a evidenciar (usa solo estos; no inventes códigos OA):
            - {{inds}}

            Reglas:
            - No inventes códigos de OA distintos al indicado.
            - Inicio: motivación y activación de conocimientos previos (breve).
            - Desarrollo: actividades principales alineadas al OA e indicadores, con verbo propio del nivel Bloom.
            - Cierre: síntesis, retroalimentación o evaluación formativa breve.
            - Fondo pedagógico, no diseño visual.
            - Responde SOLO JSON:
            {
              "inicio": "string",
              "desarrollo": "string",
              "cierre": "string",
              "verboBloom": "string",
              "propositoClase": "string"
            }
            """;

        var text = await LlamarGeminiAsync(apiKey, prompt, ct);
        var parsed = JsonSerializer.Deserialize<EstructuraClaseDto>(LimpiarJson(text), JsonOptions)
            ?? throw new InvalidOperationException("No se pudo interpretar la estructura de clase.");
        if (string.IsNullOrWhiteSpace(parsed.Inicio) || string.IsNullOrWhiteSpace(parsed.Desarrollo) ||
            string.IsNullOrWhiteSpace(parsed.Cierre))
            throw new InvalidOperationException("La estructura generada está incompleta.");
        return parsed;
    }

    public async Task<GeminiContentDto> GenerarMaterialClaseAsync(
        Clase clase,
        GenerarMaterialClaseRequest request,
        IReadOnlyList<IndicadorEvaluacion> indicadores,
        CurriculumGeneracionContext contexto,
        CancellationToken ct = default)
    {
        var oa = clase.ObjetivoAprendizaje ?? contexto.Objetivos.First();
        var bloom = NivelBloomHelper.Normalizar(clase.NivelBloom) ?? "Aplicar";
        var cantidad = Math.Clamp(request.CantidadItems, 1, 20);
        var tipoNombre = request.Tipo switch
        {
            TipoDocumento.Prueba => "prueba / evaluación",
            TipoDocumento.Ejercicios => "ficha de ejercicios",
            _ => "guía de aprendizaje"
        };

        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var demoReq = new GenerarDocumentoRequest
            {
                Tipo = request.Tipo,
                CantidadItems = cantidad,
                SoloSeleccionMultiple = request.SoloSeleccionMultiple,
                UsarTaxonomiaBloom = true
            };
            return CrearContenidoDemo(demoReq, contexto, request.SoloSeleccionMultiple, cantidad);
        }

        var inds = indicadores.Count == 0
            ? "(usar descripción del OA)"
            : string.Join("\n- ", indicadores.Select(i => i.Descripcion));

        var reglaSm = request.SoloSeleccionMultiple
            ? "Todos SM con 4 alternativas."
            : "Mezcla desarrollo/actividad; SM solo si aporta.";
        var prompt = $$"""
            Eres un asesor pedagógico experto en Currículum Nacional chileno.
            Genera una {{tipoNombre}} en español chileno para UNA clase específica.

            - Nivel: {{contexto.Nivel.Nombre}}
            - Asignatura: {{contexto.Asignatura.Nombre}}
            - Unidad: {{contexto.Unidad.Nombre}}
            - Clase N° {{clase.Numero}}
            - OA: {{oa.Codigo}} — {{oa.Descripcion}}
            - Nivel Bloom de la clase: {{bloom}}
            - Indicadores de la clase:
            - {{inds}}

            REGLAS:
            - EXACTAMENTE {{cantidad}} ítems.
            - Cada ítem apunta a un indicador y respeta (o progresa levemente desde) el nivel Bloom {{bloom}}.
            - Incluye nivelBloom y verboBloom en cada ítem.
            - {{reglaSm}}
            - Fondo pedagógico, no diseño.

            JSON:
            {
              "titulo": "string",
              "propositoUnidad": "string",
              "habilidadFocal": "string",
              "instrucciones": "string",
              "sesiones": [],
              "items": [
                {
                  "tipo": "actividad",
                  "nivelBloom": "{{bloom}}",
                  "verboBloom": "string",
                  "enunciado": "string",
                  "alternativas": [],
                  "respuestaCorrecta": "string",
                  "puntaje": 1,
                  "indicadorCodigoODescripcion": "string"
                }
              ]
            }
            """;

        var text = await LlamarGeminiAsync(apiKey, prompt, ct);
        var parsed = DeserializarContenidoFlexible(LimpiarJson(text))
            ?? throw new InvalidOperationException("No se pudo interpretar el material generado.");
        if (parsed.Items.Count == 0)
            throw new InvalidOperationException("Gemini no devolvió ítems de material.");
        if (parsed.Items.Count > cantidad)
            parsed.Items = parsed.Items.Take(cantidad).ToList();
        foreach (var item in parsed.Items)
            item.NivelBloom = NivelBloomHelper.Normalizar(item.NivelBloom) ?? bloom;
        parsed.Sesiones = [];
        return parsed;
    }

    /// <summary>Ignora "sesiones" mal formadas que a veces devuelve Gemini en material.</summary>
    private static GeminiContentDto? DeserializarContenidoFlexible(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var dto = new GeminiContentDto
            {
                Titulo = root.TryGetProperty("titulo", out var t) ? t.GetString() ?? "" : "",
                Instrucciones = root.TryGetProperty("instrucciones", out var i) ? i.GetString() ?? "" : "",
                PropositoUnidad = root.TryGetProperty("propositoUnidad", out var p) ? p.GetString() : null,
                HabilidadFocal = root.TryGetProperty("habilidadFocal", out var h) ? h.GetString() : null
            };

            if (root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in itemsEl.EnumerateArray())
                {
                    var item = el.Deserialize<GeminiItemDto>(JsonOptions);
                    if (item is not null && !string.IsNullOrWhiteSpace(item.Enunciado))
                        dto.Items.Add(item);
                }
            }

            return dto;
        }
        catch
        {
            return JsonSerializer.Deserialize<GeminiContentDto>(json, JsonOptions);
        }
    }

    private async Task<string> LlamarGeminiAsync(string apiKey, string prompt, CancellationToken ct)
    {
        var model = _config["Gemini:Model"] ?? "gemini-3.1-flash-lite";
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
        var payload = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.7, responseMimeType = "application/json" }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(httpRequest, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Error Gemini ({Status}): {Body}", response.StatusCode, errorBody);
            throw new InvalidOperationException($"Error al llamar a Gemini: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var generatedText = ExtraerTextoRespuesta(responseJson);
        if (string.IsNullOrWhiteSpace(generatedText))
            throw new InvalidOperationException("Gemini no devolvió contenido.");
        return generatedText;
    }

    private static EstructuraClaseDto CrearEstructuraDemo(Clase clase, ObjetivoAprendizaje oa, List<string> indicadores)
    {
        var bloom = NivelBloomHelper.Normalizar(clase.NivelBloom) ?? "Aplicar";
        var ind = indicadores.FirstOrDefault() ?? oa.Descripcion;
        return new EstructuraClaseDto
        {
            PropositoClase = $"Abordar {oa.Codigo} en nivel Bloom {bloom}.",
            VerboBloom = bloom.ToLowerInvariant() switch
            {
                "recordar" => "identificar",
                "comprender" => "explicar",
                "aplicar" => "resolver",
                "analizar" => "comparar",
                "evaluar" => "justificar",
                _ => "diseñar"
            },
            Inicio = $"Activación: recuperar ideas previas sobre «{oa.Codigo}». Motivación breve con un ejemplo cotidiano. Presentar el propósito de la clase (nivel {bloom}).",
            Desarrollo = $"Actividades principales para {bloom.ToLowerInvariant()} el OA. Trabajo guiado y práctica alineada al indicador: {ind}. El verbo observable debe ser visible en las consignas.",
            Cierre = $"Síntesis colectiva de lo aprendido. Evaluación formativa breve (ticket de salida) vinculada al indicador. Anticipar la siguiente clase (mayor complejidad Bloom)."
        };
    }
}
