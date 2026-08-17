# P6 — Flujo clase end-to-end

Cerrada: 2026-08-16.

## Objetivo

Que la ficha de clase guíe el ciclo completo del profesor sin que tenga que “saber” los módulos internos:

**Planificar → Material → Evaluar → Evidencia → Refuerzo**

## Qué cambió

### Hub Copilot (`ClaseDetallePage`)
- Contexto (OA / Bloom)
- Checklist **Ciclo de esta clase** con siguiente paso accionable
- Crear en grilla: Guía / Actividad / Evaluación / Ticket
- Adaptar: Simplificar / Apoyo visual / Andamiaje / DUA
- Lista inline de **Materiales de esta clase** (abre el editor)
- En el aula: Asistencia · **Evaluar / evidencia** · Guardar · Clase realizada
- Al volver a la página se refresca el ciclo (`OnAppearing`)

### Evaluación
- Texto que conecta puntajes → lectura por OA → siguiente paso
- Botón **Volver a la clase** para cerrar el ciclo en la UI

## Fuera de P6

- Golden tests / sanitizer IA / feedback 👍👎 (calidad pedagógica profunda)
- Reutilización / plantillas / importar material
- Evidencia ítem→OA avanzada, Excel, clase de refuerzo automática
- Rename `ProfeAsistente` → `ProfeAsistente`

## Verificación

```bash
DOTNET_ROOT=$HOME/.dotnet PATH=$DOTNET_ROOT:$PATH
dotnet test tests/ProfeAsistente.Api.Tests/ProfeAsistente.Api.Tests.csproj
dotnet build src/ProfeAsistente.Maui/ProfeAsistente.Maui.csproj -f net8.0-maccatalyst
```
