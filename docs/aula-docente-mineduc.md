# Aula docente y marco MINEDUC

La aplicación cubre el **ciclo pedagógico local** que el ministerio pide al profesor en el aula:

- Bases Curriculares (OA, indicadores, habilidades, actitudes).
- Planificación de unidad alineable a PEI/PME (`PeiAlignment`, `PmeAction`).
- DUA en la clase y planes PIE / Decreto 83 por estudiante.
- Evaluación diagnóstica, formativa y sumativa alineada a OA.
- Asistencia y evidencias de clase realizada.
- Cobertura planificada vs ejecutada.

## Qué no reemplaza

No es SIGE, ni el libro de clases oficial, ni la plataforma DocenteMás, ni el PME en la plataforma de subvenciones. Es un **registro de apoyo** para que el docente cumpla la planificación, la diversificación y la evaluación para el aprendizaje.

## API

```
GET  /api/teacher/dashboard
GET/POST /api/institutions/{id}/students
GET/POST /api/courses/{id}/roster
GET/POST /api/students/{id}/support-plans
GET/POST /api/clases/{id}/dua
GET/PUT  /api/clases/{id}/asistencia
POST /api/clases/{id}/completar
GET/POST /api/evaluaciones
GET/PUT  /api/evaluaciones/{id}/puntajes
GET      /api/evaluaciones/{id}/evidencia
```
