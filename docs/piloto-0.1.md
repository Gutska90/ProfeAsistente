# Piloto 0.1 — instrumentación

Cerrada (código): 2026-08-16.

## Objetivo

Correr un piloto con **3–5 docentes** midiendo:

1. % de materiales **exportados** (usados fuera de la app)  
2. Feedback 👍/👎  
3. Cobertura de **evidencia** en clases con material  
4. Autoreporte de **minutos ahorrados**  
5. Costo/latencia IA (P11)

## Qué quedó en producto

| Pieza | Detalle |
|-------|---------|
| `GET /api/pilot/metrics` | Resumen del periodo (default 30 días) |
| `POST /api/pilot/session-reports` | Autoreporte: minutos, ¿lo usó en clase?, ¿lo usaría otra vez? |
| Configuración (MAUI) | Ver resumen + registrar minutos ahorrados |
| Script | `scripts/pilot-metrics.sh` |

## Checklist operativo (humano)

- [ ] Empaquetar: `./scripts/publish-piloto-mac.sh`  
- [ ] En Mac docente: .NET 8 + `./start-piloto.sh`  
- [ ] Login demo o usuario real  
- [ ] Flujo: Hoy → clase → guía → evaluación → evidencia → (export DOCX)  
- [ ] Pedir feedback 👍/👎 en el editor  
- [ ] En Configuración: registrar minutos ahorrados  
- [ ] Al final de la semana: `./scripts/pilot-metrics.sh` o abrir Configuración  

## Fuera de esta entrega

- Reclutar profesores (operación, no código)  
- Dashboard web multi-escuela  
- P13 evidencia avanzada  

## Verificación

```bash
dotnet test tests/ProfeAsistente.Api.Tests --filter "FullyQualifiedName~Pilot"
dotnet build src/ProfeAsistente.Maui/ProfeAsistente.Maui.csproj -f net8.0-maccatalyst
```
