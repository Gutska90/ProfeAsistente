# Piloto 0.1 — instrumentación

Cerrada (código): 2026-08-16.

## Objetivo

Correr un piloto con **3–5 docentes** midiendo:

1. % de materiales **exportados**
2. Feedback 👍/👎
3. Cobertura de **evidencia** en clases con material
4. Autoreporte de **minutos ahorrados** + “¿cuánto sin la app?”
5. Costo/latencia IA (P11)

## Qué quedó en producto

| Pieza | Detalle |
|-------|---------|
| `GET /api/pilot/metrics` | Resumen del periodo (default 30 días) |
| `POST /api/pilot/session-reports` | Minutos, tramo sin app, uso en clase |
| Configuración (MAUI) | Resumen + autoreporte |
| Script | `scripts/pilot-metrics.sh` |

## Tres tareas obligatorias

### Tarea A — Prepara tu clase de mañana
Mide: tiempo, dudas, material generado.

### Tarea B — Crea una guía que usarías
Mide: ediciones, regeneraciones, si exporta.

### Tarea C — Evaluación + evidencia
Mide: si entiende la evidencia y si confía.

Al cerrar cada tarea, en **Configuración** registre minutos ahorrados y el tramo “sin ProfeAsistente”.

## Métricas que importan

```text
Clases preparadas
Materiales exportados (%)
Evaluaciones usadas
Tiempo ahorrado (autoreporte)
Retorno la semana siguiente
```

No celebre solo “generaciones IA”.

## Checklist operativo

- [ ] `./scripts/publish-piloto-mac.sh`
- [ ] Mac docente: .NET 8 + `./start-piloto.sh`
- [ ] Flujo Hoy → clase → guía → evaluación → evidencia → export
- [ ] Feedback + minutos en Configuración
- [ ] `./scripts/pilot-metrics.sh` al cierre de semana

## Fuera de esta entrega

Reclutar profesores (operación). Corpus golden → [corpus-pedagogico-p13.md](corpus-pedagogico-p13.md).

## Verificación

```bash
dotnet test tests/ProfeAsistente.Api.Tests --filter "FullyQualifiedName~Pilot"
dotnet build src/ProfeAsistente.Maui/ProfeAsistente.Maui.csproj -f net8.0-maccatalyst
```
