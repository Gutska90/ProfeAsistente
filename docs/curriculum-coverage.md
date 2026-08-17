# Cobertura curricular

## Modos

- **Planificada** (`mode=Planned`): usa secuencia y clases existentes.
- **Ejecutada** (`mode=Executed`): solo clases `Realizada` + evidencias.

Niveles por OA: Asignado → Planificado → Trabajado → Evidenciado → Evaluado.

## Endpoints

```text
GET  /api/planificaciones/{id}/cobertura
GET  .../cobertura/planificada
GET  .../cobertura/ejecutada
GET  .../cobertura/matriz
POST .../cobertura/recalcular
GET  .../alertas
GET  .../sugerencias
POST /api/clases/{id}/completar
```

## Alertas (códigos)

`PLAN_NO_SESSIONS`, `PLAN_OBJECTIVE_WITHOUT_SESSION`, `PLAN_OBJECTIVE_BELOW_MINIMUM`,
`PLAN_INDICATOR_NOT_COVERED`, `PLAN_INDICATOR_EVALUATED_TOO_EARLY`, `PLAN_BLOOM_JUMP`,
`PLAN_NO_ASSESSMENT`, `PLAN_CLASS_OUTSIDE_RANGE`, …

## Matriz

Filas OA/indicador × columnas clase. Leyenda: I/P/F/E/R.

## Evidencias

`ClassLearningEvidence` distingue cobertura planificada de evidenciada.
