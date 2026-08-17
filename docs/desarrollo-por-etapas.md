# Desarrollo por etapas

Regla: **una etapa activa a la vez**. Congelar features grandes hasta validar pedagogía + piloto.

Cada etapa termina cuando:

1. Compila la solución.
2. Pasan las pruebas existentes (no se bajan).
3. Hay un flujo usable en MAUI o API documentado.
4. Se anota qué quedó fuera.

## Ya cerrado (no reabrir salvo bugs)

| Etapa | Qué quedó |
|-------|-----------|
| A–E, 1–5 | Currículum, planificación, materiales, seguridad, aula, calendario, evaluación, UI, offline |
| **P0–P4** | Navegación, hub clase, biblioteca, evidencia→refuerzo, `IAiProvider` |
| **P5–P9** | Consolidación, e2e, offline, rename, pack Mac |
| **P10** | Calidad pedagógica — [calidad-pedagogica-p10.md](calidad-pedagogica-p10.md) |
| **P11** | Observabilidad IA — [observabilidad-ia-p11.md](observabilidad-ia-p11.md) |
| **P12** | Reutilización — [reutilizacion-p12.md](reutilizacion-p12.md) |
| **Piloto 0.1 (código)** | Instrumentación — [piloto-0.1.md](piloto-0.1.md) |

## Cola (validación, no plataforma)

La incertidumbre ya no es C#/EF/MAUI. Es: ¿el docente entiende, confía y ahorra tiempo?

| Orden | Etapa | Trabajo | Prioridad |
|-------|-------|---------|-----------|
| 1 | **P13** | **Corpus pedagógico** 30–50 golden cases (+ adversariales) — [corpus-pedagogico-p13.md](corpus-pedagogico-p13.md) | 🔴 |
| 2 | **P14** | **Piloto docente** 3–5 profesores, 2 semanas (medir export, tiempo, retención) | 🔴 |
| 3 | **P15** | Solo mejoras que salgan del piloto (no decidirlas ahora) | 🟠 |

### Congelado hasta P14

Evidencia Excel avanzada, Windows packaging, adaptar-OA automático, evaluación subjetiva con otra IA, SIGE/ERP, App Store.

### Fuera de cola

SIGE, libro legal, PME, portal apoderados, chat, LMS, microservicios.

## Cómo pedir trabajo

`Sigue con P13` (corpus) · `Ejecuta el piloto` · o un bug concreto. No abrir módulos nuevos “por si acaso”.
