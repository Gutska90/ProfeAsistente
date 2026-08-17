# P3 — Evaluación + evidencia por OA → refuerzo

## Qué cambió

### Lectura por OA
Tras registrar puntajes/niveles de logro, la app resume la evidencia alineada al OA de la clase:

- Conteo **Logrado / Medianamente logrado / Por lograr**
- Indicadores de la clase
- Tabla de especificaciones si la evaluación está ligada a una **prueba** generada
- Recomendación de refuerzo cuando una parte relevante del curso está en «por lograr»

API: `GET /api/evaluaciones/{id}/evidencia`

### Evidencia persistida
Al guardar puntajes se crea/actualiza un registro en `ClassLearningEvidences` (tipo formativa/sumativa), usable también por cobertura ejecutada.

### Crear refuerzo
Botón **Crear refuerzo para este OA** abre la generación de **Actividad** con `intent=reinforce` (ítems formativos, sin inventar OA).

### Evaluación
- Al crear desde una clase se asocia automáticamente el OA y, si existe, la última prueba educativa de esa clase (tabla de especificaciones).
- UI muestra OA, lectura, especificaciones y estudiantes que necesitan apoyo.

## Fuera de P3

Análisis item-a-item automático, rúbricas avanzadas, sync a SIGE, unificar `Documento` legado (P4).
