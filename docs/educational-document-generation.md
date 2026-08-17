# Generación de guías, ejercicios y pruebas

Flujo para generar **material educativo** (guía, ejercicios, prueba) alineado con OA, indicadores, Bloom y la estructura vigente de la clase. Sin exportación DOCX (Prompt 7).

## Tipos

| Enum | Valores |
|------|---------|
| `EducationalDocumentType` | LearningGuide, Exercises, Assessment |
| `EducationalDocumentStatus` | Draft → UnderReview → Reviewed → Final (también Archived, Outdated) |
| `EducationalItemType` | MultipleChoice, TrueFalse, ShortAnswer, OpenResponse, Matching, Completion, ProblemSolving, PracticalActivity, Reflection |
| `ItemDifficulty` | Basic, Intermediate, Advanced |

## Generar desde API

```http
POST /api/clases/{classId}/educational-documents/generate
```

Ejemplos:

- Guía: `"documentType": "LearningGuide"`
- Ejercicios: `"documentType": "Exercises"`
- Prueba: `"documentType": "Assessment"`

Requiere `GEMINI_API_KEY` y currículum publicado. La API arranca sin la key, pero la generación responde con error de configuración.

## Prompts

| Tipo | Archivo | Versión |
|------|---------|---------|
| Guía | `Prompts/learning-guide-system-prompt.txt` | `learning-guide-v1` |
| Ejercicios | `Prompts/exercises-system-prompt.txt` | `exercises-v1` |
| Prueba | `Prompts/assessment-system-prompt.txt` | `assessment-v1` |

## Vistas

- **Docente:** `GET /api/educational-documents/{id}` — incluye respuestas, `isCorrect`, explicaciones.
- **Estudiante:** `GET /api/educational-documents/{id}/student-view` — sin clave ni notas.
- **Clave:** `GET /api/educational-documents/{id}/answer-key` — endpoint separado.

## Estados

Solo `Draft` y `UnderReview` se editan libremente. `Final` exige validación y no se modifica directamente (duplicar o archivar). Si cambia OA/Bloom/indicadores de la clase, el material se marca `Outdated`.

## Límites

```json
"AiUsage": {
  "MaximumDocumentGenerationsPerClassPerDay": 10,
  "MaximumItemRegenerationsPerDocumentPerDay": 30
}
```

## MAUI

Desde detalle de clase:

- Ver materiales
- Generar guía / ejercicios / prueba
- Editor con validar, estados, vista estudiante/docente, regenerar ítem, comparar

## Pruebas

```bash
dotnet test ProfeAsistente.sln
```

Prueba manual opcional:

```bash
export RUN_GEMINI_DOCUMENT_TESTS=true
export GEMINI_API_KEY="TU_API_KEY"
```

No se ejecuta en CI por defecto.
