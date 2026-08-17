# Exportación DOCX

Exportación a Microsoft Word (Open XML) sin Interop ni Word instalado. Usa `DocumentFormat.OpenXml` 3.2.0.

## Tipos

| Tipo | Descripción |
|------|-------------|
| Planning | Planificación completa |
| ClassPlan | Clase individual |
| LearningGuide / Exercises / Assessment | Material educativo |
| AnswerKey | Clave (solo docente) |
| SpecificationTable | Tabla de especificaciones |
| PlanningPackage | ZIP con planificación, clases y materiales |

Audiencias: `Student`, `Teacher`, `Administrative`.

## Endpoints (rutas en español del proyecto)

```text
POST /api/exports
GET  /api/exports/{exportId}
GET  /api/exports/{exportId}/download
DELETE /api/exports/{exportId}

POST /api/planificaciones/{id}/export
POST /api/planificaciones/{id}/export-package
POST /api/clases/{id}/export
POST /api/educational-documents/{id}/export
POST /api/educational-documents/{id}/export-answer-key
POST /api/educational-documents/{id}/export-specification-table

POST /api/admin/exports/cleanup
GET  /api/admin/exports/storage-summary
```

## Almacenamiento temporal

Configurable en `Export:RootPath` (default `App_Data/Exports/...`).  
No se guardan DOCX como BLOB en SQLite. Retención: `KeepFilesForDays` (30).

## Seguridad versión estudiante

Se construye desde datos sin `IsCorrect`, `ExpectedAnswer`, `Explanation`, `TeacherNotes`.  
Además se inspecciona el texto del DOCX antes de marcar Completed.

## Configuración

```json
"Export": {
  "RootPath": "App_Data/Exports",
  "KeepFilesForDays": 30,
  "MaximumFileSizeMb": 50,
  "UseBackgroundQueue": false,
  "AllowOutdatedDocuments": false
}
```

## Migración

```powershell
dotnet ef migrations add AddDocumentExports `
  --project src/AppEducativa.Api `
  --startup-project src/AppEducativa.Api

dotnet ef database update `
  --project src/AppEducativa.Api `
  --startup-project src/AppEducativa.Api
```

## MAUI

- `ExportOptionsPage` — opciones y tipo
- `ExportProgressPage` — descarga y guardado en Documents/AppEducativa
- `ExportHistoryPage` — historial

## Limitaciones

- No hay cola en segundo plano (`UseBackgroundQueue=false`).
- El selector de carpeta nativo no está integrado; se guarda en Documents/AppEducativa.
- PDF de materiales educativos nuevos no forma parte de este módulo (el legado QuestPDF sigue en `ExportService`).
- Compatibilidad Word/LibreOffice: validada con OpenXmlValidator; apertura visual manual recomendada.
