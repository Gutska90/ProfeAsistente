# P12 — Reutilización de materiales

Cerrada: 2026-08-16.

## Objetivo

Que el docente pueda **usar un material ya bueno en otra clase** (y guardar plantillas), sin regenerar desde cero.

## Qué quedó

| Pieza | Detalle |
|-------|---------|
| Linaje | `SourceDocumentId` en `EducationalDocument` |
| Plantilla | `IsTemplate` + `POST .../save-as-template` + filtro biblioteca `templatesOnly` |
| Reutilizar | `POST /api/educational-documents/{id}/reuse` con `targetClassId` y `setAsCurrent` |
| Destinos | `GET .../reuse-targets` (mismas clases de curso/unidad, prioriza mismo OA) |
| Duplicar | Misma clase, con linaje; opcional `setAsCurrent` |
| Versión actual | `POST .../set-current` (ya existía; expuesto en editor) |
| MAUI | Editor: Usar en otra clase / Duplicar / Plantilla / Versión actual · Biblioteca: filtro Plantillas + botón reutilizar |

## Flujo piloto

1. Abrir material bueno en editor  
2. **Usar en otra clase** → elegir destino  
3. Si el OA cambia, el material queda en revisión con advertencia  
4. Opcional: **Guardar plantilla** → aparece en Biblioteca → Plantillas  

## Fuera de P12

- Adaptación automática de indicadores al OA destino  
- Marketplace / compartir entre escuelas  
- Diff visual rico entre versiones  

## Verificación

```bash
dotnet test tests/ProfeAsistente.Api.Tests --filter "FullyQualifiedName~Reuse_"
dotnet test tests/ProfeAsistente.Api.Tests
dotnet build src/ProfeAsistente.Maui/ProfeAsistente.Maui.csproj -f net8.0-maccatalyst
```
