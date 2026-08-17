# P5 — Consolidación técnica

Cerrada: 2026-08-16.

## Objetivo

Deuda técnica del MVP (P0–P4) sin cambiar el producto: servicios más chicos, reloj/zona horaria, health, demo solo en Development, retiro seguro del flujo Documento/Gemini legado, init de DB fuera de `Build`.

## Qué quedó

| Área | Cambio |
|------|--------|
| Classroom | `ClassroomService` como fachada; lógica en `TeacherDashboardService`, `CourseRosterService`, `StudentSupportService`, `ClassDuaService`, `AttendanceService`, `AssessmentService`, `ClassroomAccess` |
| Reloj | `IApplicationClock` + zona de institución/usuario en dashboard Hoy |
| Health | `GET /health/live`, `GET /health/ready` (DB + presencia de API key AI), `GET /api/health` |
| Demo | `Demo:Enabled`; seed e admin de prueba solo si **Development** y demo activo |
| Documento legado | `DocumentosController` → **410 Gone**; `IGeminiService`/`GeminiService` `[Obsolete]` y **sin DI** |
| Host | `DatabaseInitializer` se llama desde `Program` / `ApiTestHost` **después** de `Build` |
| Hoy (MAUI) | Bloque Resumen (conteos) debajo de acciones principales |

## Fuera de P5

- Rename de namespaces/solución `ProfeAsistente` → `ProfeAsistente`
- Migración de datos Documento → EducationalDocument
- Microservicios, SIGE, ERP escolar

## Verificación

```bash
DOTNET_ROOT=$HOME/.dotnet PATH=$DOTNET_ROOT:$PATH
dotnet test tests/ProfeAsistente.Api.Tests/ProfeAsistente.Api.Tests.csproj
dotnet build src/ProfeAsistente.Maui/ProfeAsistente.Maui.csproj -f net8.0-maccatalyst
```
