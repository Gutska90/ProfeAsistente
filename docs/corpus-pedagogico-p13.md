# P13 — Corpus pedagógico

Estado: **en curso**. Meta: **30–50 golden cases** antes de hinchar producto.

## Objetivo

Demostrar (con fixtures + validador determinista) que el material cumple expectativas mínimas por nivel/asignatura/tipo — no solo que “compila JSON”.

## Cobertura mínima objetivo

| Área | Niveles ejemplo |
|------|-----------------|
| Matemática | 1°B, 4°B, 6°B, 8°B, 2°M |
| Lenguaje | 2°B, 4°B, 7°B, 1°M |
| Ciencias | 4°B, 6°B, 8°B |
| Historia | 5°B, 7°B, 2°M |

Tipos: Guía · Actividad · Evaluación. Incluir **casos adversariales** (inyección, OA incorrecto, ítems duplicados).

## Layout

```text
tests/ProfeAsistente.Pedagogy.Tests/golden/
  matematica/
  lenguaje/
  ciencias/
  historia/
  adversarial/
```

Cada JSON: `id`, `course`, `subject`, `objectiveCode`, `documentType`, `expectations`, `sampleDocument`.

## Qué mide cada caso

OA, cantidad de ítems, tipos, respuesta/clave, indicadores, Bloom, sin HTML ejecutable, sin duplicados obvios.

## Fuera de P13

- Revisión manual por experto de cada fixture (operación humana)
- Evaluador IA subjetivo
- Validación aritmética completa de todas las claves

## Verificación

```bash
dotnet test tests/ProfeAsistente.Pedagogy.Tests
```
