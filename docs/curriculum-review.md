# Revisión curricular (Prompt 4)

Flujo humano obligatorio antes de publicar contenido oficial:

```text
Lote extraído → Sesión de revisión → Corrección → Revalidación → Diff
→ Ready (hash) → Aprobación → Importación (Draft) → Publicación
```

La revisión trabaja sobre un **modelo intermedio** (`ReviewableCurriculumPackage`) con IDs temporales estables (`unit-001`, `oa-001`, `oa-001-ind-001`, …).  
**Nunca** se sobrescribe `OriginalExtractionJson`.

## Estados

| Ámbito | Valores |
|--------|---------|
| Import batch | `PendingReview` → `ReadyForApproval` → `Approved` → `Imported` (+ `PublishedAt`) |
| Review session | `NotStarted` → `InProgress` → `ReadyForApproval` → `Approved` / `Rejected` / `Closed` |
| Registro | `Pending` · `Accepted` · `Corrected` · `Rejected` · `Ignored` |
| Publicación | `Draft` (tras import) · `Published` · `Archived` |

## Cómo iniciar una revisión

```http
POST /api/admin/curriculum/imports/{batchId}/review/start
```

Requisito: lote en `PendingReview` (u otro estado revisable).  
En MAUI: detalle del lote → **Iniciar revisión**.

## Cómo corregir un OA

```http
PUT /api/admin/curriculum/imports/{id}/review/objectives/{temporaryId}
```

```json
{
  "code": "OA 1",
  "description": "Texto corregido…",
  "decision": "Corrected",
  "reason": "El extractor unió texto de actividades",
  "rowVersion": "…"
}
```

## Cómo mover un indicador

```http
PUT /api/admin/curriculum/imports/{id}/review/indicators/{temporaryId}
```

```json
{
  "objectiveTemporaryId": "oa-002",
  "decision": "Corrected",
  "reason": "Indicador pertenecía al OA 2",
  "rowVersion": "…"
}
```

## Validar / Diff / Ready / Aprobar / Importar / Publicar

```http
POST .../review/revalidate
GET  .../review/diff
POST .../review/ready
POST .../approve
POST .../import
POST .../publish
```

`ready` exige: sin blocking, sin pendientes, sin comentarios bloqueantes abiertos, validación y diff posteriores al último cambio, al menos una unidad y un OA aceptados/corregidos. Calcula **SHA-256** del JSON revisado.

La aprobación comprueba el hash. Si el contenido cambió, vuelve a `InProgress`.

La importación usa `FinalReviewJson` (propuesta revisada), no la extracción cruda. Los registros quedan en `Draft` hasta `publish`.

Los endpoints públicos solo muestran `Published` (o seed `AprobadoParaPruebas`).

## Operaciones estructurales

- `POST .../review/objectives/{id}/split`
- `POST .../review/merge`
- `DELETE .../review/{entityType}/{temporaryId}` (lógico)
- `POST .../review/{entityType}/{temporaryId}/restore`
- `POST .../review/changes/{changeId}/revert`
- Comentarios: `GET/POST .../review/comments`, `PUT .../comments/{id}/resolve`

## Concurrencia

Cada mutación envía `rowVersion` (Base64). Conflicto → HTTP **409**.

## Migración SQLite

```bash
export DOTNET_ROOT="$HOME/.dotnet" PATH="$DOTNET_ROOT:$PATH"
cd /Users/user/ProfeAsistente
dotnet ef database update --project src/AppEducativa.Api --startup-project src/AppEducativa.Api
# o simplemente iniciar la API (Database.Migrate al arrancar)
```

Migración: `20260802012726_CurriculumReviewModule` (sesiones, comentarios, decisiones, releases, campos de revisión/publicación).

## MAUI

Rutas: `adminReviewDashboard`, `adminReviewUnits`, `adminReviewObjectives`, `adminReviewObjectiveDetail`, `adminReviewIssues`, `adminReviewChanges`, `adminReviewDiff`, `adminReviewComments`.
