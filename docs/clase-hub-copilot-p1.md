# P1 — Clase como hub Copilot

## Qué cambió

La ficha de clase (`ClaseDetallePage`) es el hub de preparación:

| Acción | Qué hace |
|--------|----------|
| **Planificar clase** | Muestra Inicio / Desarrollo / Cierre; genera estructura o abre configuración avanzada |
| **Crear guía** | Generación `LearningGuide` |
| **Crear actividad** | Generación `Exercises` |
| **Crear evaluación** | Generación `Assessment` |
| **Crear ticket de salida** | `Assessment` con preset `intent=exitTicket` (pocos ítems, ~10 min, formativo) |
| **Adaptar material** | Atajos DUA: simplificar, andamiaje, apoyo visual + registro de estrategias |
| **Ver materiales** | Lista de documentos educativos de la clase |
| **En el aula** | Asistencia, puntajes, guardar, marcar realizada |

Herramientas avanzadas (export DOCX, material legado, estructura avanzada) quedan colapsadas.

## Reuso (sin módulos nuevos)

- Rutas existentes: `educationalDocumentGeneration`, `classStructureGeneration`, `educationalDocuments`, `asistencia`, `evaluacionClase`.
- Query `intent` en generación: `exitTicket` | `simplify` | `scaffold`.

## Fuera de P1

Biblioteca (P2), evidencia → refuerzo (P3), rename / unificar Documento (P4).
