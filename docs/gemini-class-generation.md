# Generación de estructura de clase con Gemini

Flujo para generar **Inicio / Desarrollo / Cierre** de una clase usando únicamente currículum aprobado y publicado. No incluye guías, ejercicios, pruebas ni exportación DOCX.

## Configuración

En `appsettings.json` (sin API key):

```json
{
  "Gemini": {
    "ApiKeyEnvironmentVariable": "GEMINI_API_KEY",
    "Model": "gemini-2.5-flash",
    "BaseUrl": "https://generativelanguage.googleapis.com",
    "TimeoutSeconds": 60,
    "MaxRetries": 2,
    "Temperature": 0.3,
    "MaxOutputTokens": 5000,
    "EnableGeneration": true,
    "PersistRequestPayloads": true,
    "PromptVersion": "class-structure-v1"
  },
  "AiUsage": {
    "MaximumGenerationsPerClassPerDay": 10,
    "MaximumConcurrentGenerations": 2
  }
}
```

La API key se lee solo desde la variable de entorno indicada (por defecto `GEMINI_API_KEY`). Si falta, la API arranca, pero `POST .../generate-structure` responde con error de configuración claro.

### PowerShell (desarrollo local)

```powershell
$env:GEMINI_API_KEY="TU_API_KEY"
dotnet run --project src/ProfeAsistente.Api
```

Para persistir la variable en la sesión de usuario (Windows) sin subirla al repositorio:

```powershell
[System.Environment]::SetEnvironmentVariable("GEMINI_API_KEY", "TU_API_KEY", "User")
```

En macOS/Linux:

```bash
export GEMINI_API_KEY="TU_API_KEY"
dotnet run --project src/ProfeAsistente.Api
```

Archivos locales ignorados por Git: `.env`, `appsettings.Development.local.json`, `appsettings.*.local.json`.

## Cambiar el modelo

Editar `Gemini:Model` en configuración. El código usa siempre `IOptions<GeminiOptions>.Model`; no hay nombres de modelo hardcodeados en el cliente.

## Deshabilitar Gemini

```json
"Gemini": { "EnableGeneration": false }
```

## Arquitectura

1. MAUI / cliente → `POST /api/clases/{id}/generate-structure`
2. `ClassStructureGenerationService` carga clase, planificación y currículum publicado
3. `ClassGenerationContextBuilder` arma contexto + snapshot curricular
4. `IGeminiClient` envía system prompt (`Prompts/class-structure-system-prompt.txt`, versión `class-structure-v1`) + JSON de contexto
5. `ClassGenerationValidator` valida currículum, duraciones y contenido
6. Si hay errores corregibles: un único intento de reparación
7. Persistencia en `ClassStructureGeneration` + revisión inicial; payloads opcionales en `App_Data/AI/ClassGeneration/`

La IA no puede inventar códigos de OA, indicadores, habilidades, actitudes ni versiones curriculares: esos datos salen de SQLite y se revalidan en la respuesta.

## Endpoints

| Método | Ruta |
|--------|------|
| POST | `/api/clases/{classId}/generate-structure` |
| GET | `/api/clases/{classId}/structure-generations` |
| GET | `/api/clases/{classId}/structure-generations/current` |
| GET | `/api/clases/{classId}/generation-context` (desarrollo / admin) |
| GET | `/api/structure-generations/{generationId}` |
| POST | `/api/structure-generations/{generationId}/retry` |
| POST | `/api/structure-generations/{generationId}/set-current` |
| PUT | `/api/structure-generations/{generationId}/content` |
| DELETE | `/api/structure-generations/{generationId}` |

Compatibilidad: también existe `POST /api/clases/{id}/generar-estructura`.

## Validaciones y reparación

- Duración total 30–240 min; suma de fases; ajuste automático ≤ 5 min con advertencia
- Diferencia > 5 min → rechazo y un reintento de corrección
- IDs/códigos deben coincidir con el contexto enviado
- Sin HTML ejecutable; textos libres del profesor sanitizados y tratados como contexto, no como instrucciones del sistema

## Snapshots y versiones

Antes de generar se crea/recupera `ClaseCurriculumSnapshot`. Cada generación es inmutable; las ediciones humanas crean `ClassStructureRevision`. Si cambian OA, indicadores, Bloom o duración relevante, la generación vigente se marca `IsOutdated`.

## Límites

- Máximo de generaciones por clase y día (`AiUsage:MaximumGenerationsPerClassPerDay`)
- Una generación `Processing` por clase (HTTP 409 `GenerationAlreadyInProgress`)

## Pruebas

```bash
dotnet test ProfeAsistente.sln
```

Prueba manual opcional contra la API real (no CI):

```bash
export RUN_GEMINI_INTEGRATION_TESTS=true
export GEMINI_API_KEY="TU_API_KEY"
# ejecutar solo el test marcado para integración real, si está presente
```

Las pruebas unitarias usan `HttpMessageHandler` falso y no requieren internet.
