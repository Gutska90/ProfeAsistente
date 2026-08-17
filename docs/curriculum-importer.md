# Importador oficial de currículum (Prompt 3)

Flujo vertical para **4° Básico · Matemática · Programa de Estudio** desde Currículum Nacional.

```text
Fuente oficial (JSON) → Descarga HTTPS → App_Data → Extracción PdfPig
→ Parser MAT 4B → Validación → Vista previa editable → Aprobación
→ Importación transaccional SQLite → API pública / MAUI
```

No se usa Gemini en esta iteración. La aprobación humana es obligatoria antes de publicar.

## Arquitectura

| Capa | Proyecto | Rol |
|------|----------|-----|
| Configuración | `AppEducativa.CurriculumImporter/Configuration` | `curriculum-sources.json`, perfiles de parser |
| Descarga | `Services/Download` + `Download/HttpSourceDownloader` | HTTPS, dominio permitido, SHA-256, ETag/304 |
| Almacenamiento | `Services/Storage` | `App_Data/Curriculum/{Downloads,Extracted,Imports}` |
| Extracción | `PdfProgramStudyExtractor` (UglyToad.PdfPig) | Páginas + texto normalizado |
| Parser | `MathematicsFourthGradeProgramParser` | Unidad, OA, indicadores, habilidades, actitudes |
| Validación / Diff / Import | servicios del importer + orquestador API | Revisión, diferencias, transacción |
| API admin | `CurriculumAdminController` | Endpoints bajo `/api/admin/curriculum` |
| UI | `AppEducativa.Maui/Views/Admin` | Fuentes, lotes, detalle, preview |

## Configurar la URL oficial

Archivo: `src/AppEducativa.CurriculumImporter/Configuration/curriculum-sources.json`  
(también se copia a la salida de la API: `Configuration/curriculum-sources.json`).

URL verificada del Programa de Estudio Matemática 4° Básico:

`https://www.curriculumnacional.cl/614/articles-18979_programa.pdf`

Si debe cambiarse:

1. Edite `url` en el JSON (HTTPS + host `www.curriculumnacional.cl`).
2. Reinicie la API o llame `POST /api/admin/curriculum/sources/reload`.
3. **No** incruste URLs en servicios ni extractores.

## Ubicación de archivos

Rutas relativas al content root de la API (configurable con `Curriculum:StorageRoot`):

| Artefacto | Ruta típica |
|-----------|-------------|
| PDF descargado | `App_Data/Curriculum/Downloads/` |
| Texto extraído | `App_Data/Curriculum/Extracted/` |
| JSON intermedio / corregido | `App_Data/Curriculum/Imports/` |
| SQLite | `appeducativa.db` (content root de la API) |

La API pública **nunca** expone rutas locales de archivos.

## Flujo HTTP (Swagger / cURL)

Base: `http://127.0.0.1:5180` (o el puerto configurado). Swagger: `/swagger`.

```http
POST /api/admin/curriculum/sources/reload
POST /api/admin/curriculum/imports
Content-Type: application/json

{ "sourceId": "matematica-4-basico-programa" }
```

```http
POST /api/admin/curriculum/imports/{id}/process
GET  /api/admin/curriculum/imports/{id}/preview
PUT  /api/admin/curriculum/imports/{id}/preview
GET  /api/admin/curriculum/imports/{id}/issues
GET  /api/admin/curriculum/imports/{id}/diff
POST /api/admin/curriculum/imports/{id}/approve
POST /api/admin/curriculum/imports/{id}/import
```

`process` ejecuta download + extract + validate; **nunca** aprueba ni importa automáticamente.

## Estados del lote

`Created` → `Downloaded` → `Extracted` → `Validated` / `PendingReview` → `Approved` | `Rejected` → `Imported`  
También: `Failed`.

No se permite saltar de `Created` a `Imported`. Un lote ya `Imported` no se vuelve a importar.

## Validaciones y revisión

Errores `Blocking` impiden aprobar. Se puede editar la vista previa; los cambios quedan en `CurriculumReviewChanges` y se conservan JSON original y corregido.

## Diferencias

`GET .../diff` compara el lote contra OA vigentes (New / Modified / Unchanged / …). No elimina historial.

## Seed demostrativo

```json
{ "Curriculum": { "IncludeDemoData": true } }
```

Con datos oficiales aprobados para 4B MAT, la API prioriza oficiales y marca el seed como demostrativo. Ponga `IncludeDemoData: false` para ocultarlo.

## Seguridad

- Política `CurriculumAdmin`: en Development permite todo; en Production exige claim `CurriculumAdmin=true`.
- Solo fuentes registradas; no URLs arbitrarias en el request.
- Path traversal / tamaño / timeout controlados en el downloader.
- TLS siempre validado.

## Agregar otro nivel o asignatura

1. Añada entrada en `curriculum-sources.json`.
2. Cree `Configuration/ParserProfiles/{id}.json` con patrones de unidad/OA/indicadores.
3. Registre un `IProgramStudyParser` específico o extienda el existente con perfil.
4. Cubra con pruebas sintéticas (sin PDFs oficiales completos en el repo si la licencia lo impide).

## Limitaciones conocidas

- Un solo perfil vertical (MAT 4B) está endurecido.
- PDFs escaneados sin texto requieren revisión manual (sin OCR automático).
- La extracción heurística puede necesitar correcciones en preview antes de aprobar.
- AngleSharp tiene aviso NU1902 (HTML; no es el camino PDF de este vertical).

## Recuperación ante errores

Si falla la importación: la transacción se revierte, el lote queda disponible (`Failed` o estado previo), y se puede corregir preview / revalidar / aprobar de nuevo. No se borran planificaciones existentes al migrar.
