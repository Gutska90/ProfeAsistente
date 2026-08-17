# P2 — Biblioteca + lenguaje UI

## Qué cambió

### Biblioteca
- Menú **Biblioteca**: lista guías, actividades y pruebas del docente.
- Filtro por tipo (Todos / Guía / Actividad / Prueba) y búsqueda por título, OA o curso.
- Desde **Mis cursos → Materiales** se abre la biblioteca filtrada por curso.
- Abrir un material lleva al editor; «Ir a la clase» vuelve al hub Copilot.

### API
- `GET /api/biblioteca/materiales?courseId=&type=&q=`
- Los resúmenes incluyen `typeLabel`, `statusLabel`, `contextLine` (curso · clase · OA).

### Lenguaje UI
- En pantallas de materiales se muestran **Guía / Actividad / Prueba** y estados en español (**Borrador**, **En revisión**, etc.).
- No se exponen nombres técnicos (`LearningGuide`, `EducationalDocument`) al profesor.
- Helper compartido: `AppEducativa.Shared.Ui.MaterialUiLabels`.

## Fuera de P2

Evidencia → refuerzo (P3), unificar `Documento` legado (P4), rename de solución.
