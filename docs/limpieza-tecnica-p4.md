# P4 — Limpieza técnica

## Qué cambió

### README
Reposiciona el producto como **ProfeAsistente** (asistente docente / Currículum Nacional), con flujo central y etapas P0–P4.

### `IAiProvider`
- Interfaz `AppEducativa.Api.Services.AI.IAiProvider`
- Implementación `GeminiAiProvider` sobre `IGeminiClient`
- `EducationalDocumentGenerationService` y `ClassStructureGenerationService` dependen de `IAiProvider` (no del cliente HTTP de Gemini)

### CORS por ambiente
- Sección `Cors:AllowedOrigins` / `RestrictInDevelopment`
- Development: AllowAnyOrigin (por defecto)
- No-Development: solo orígenes listados; lista vacía = ningún origen web

### Documento vs EducationalDocument
- **Canónico:** `EducationalDocument` (clase, biblioteca, generación, specs)
- **Legado:** `DocumentosController` marcado `[Obsolete]` + cabecera `Deprecation: true` en `POST generar`
- MAUI: herramientas avanzadas de la clase redirigen al flujo actual; ya no llaman a `generar-material` inexistente

## Fuera de P4

- Renombrar ensamblados `AppEducativa` → `ProfeAsistente`
- Migración de datos `Documento` → `EducationalDocument`
- Eliminar por completo `IGeminiService` / `GeminiService` legado
- Blazor / SaaS multi-colegio
