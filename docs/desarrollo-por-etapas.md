# Desarrollo por etapas

Regla: **una etapa activa a la vez**. No abrir sync nube, SIGE ni ERP escolar hasta cerrar la etapa en curso.

Cada etapa termina cuando:

1. Compila la solución.
2. Pasan las pruebas existentes (no se bajan).
3. Hay un flujo usable en MAUI o API documentado.
4. Se anota qué quedó fuera.

## Ya cerrado (no reabrir salvo bugs)

| Etapa | Qué quedó |
|-------|-----------|
| A–E, 1–5 | Currículum, planificación, materiales, seguridad, aula, calendario, evaluación, UI, offline |
| **P0** | Navegación Hoy / Mis cursos / Planificaciones / Configuración |
| **P1** | Clase hub Copilot |
| **P2** | Biblioteca + lenguaje UI |
| **P3** | Evaluación + evidencia por OA → refuerzo |
| **P4** | README, `IAiProvider`, CORS por ambiente, Documento legado deprecado |
| **P5** | Consolidación técnica — [consolidacion-tecnica-p5.md](consolidacion-tecnica-p5.md) |
| **P6** | Flujo clase end-to-end — [flujo-clase-e2e-p6.md](flujo-clase-e2e-p6.md) |
| **P7** | Offline más robusto — [offline-robusto-p7.md](offline-robusto-p7.md) |
| **P8** | Rename `AppEducativa` → `ProfeAsistente` — [rename-profeasistente-p8.md](rename-profeasistente-p8.md) |
| **P9** | Empaquetado piloto Mac — [release-mac-p9.md](release-mac-p9.md) |
| **P10** | Calidad pedagógica — [calidad-pedagogica-p10.md](calidad-pedagogica-p10.md) |
| **P11** | Observabilidad IA — [observabilidad-ia-p11.md](observabilidad-ia-p11.md) |
| **P12** | Reutilización — [reutilizacion-p12.md](reutilizacion-p12.md) |
| **Piloto 0.1** | Instrumentación — [piloto-0.1.md](piloto-0.1.md) |

## Cola (validación de producto, no más plataforma)

Objetivo inmediato: **correr el piloto con 3–5 docentes** usando la instrumentación (métricas + autoreporte).

| Orden | Etapa | Trabajo | Prioridad |
|-------|-------|---------|-----------|
| 1 | — | **Ejecutar piloto** con docentes reales (checklist en piloto-0.1.md) | 🔴 |
| 2 | **P13** | Evidencia avanzada: ítem→OA, Excel, clase de refuerzo | 🟠 |
| 3 | **P14** | Windows packaging (después de validar producto) | 🟡 |

### Fuera de cola

SIGE, libro legal, PME, portal apoderados/estudiantes, chat, LMS, microservicios, App Store/notarización antes de validar ahorro de tiempo, Windows antes del piloto Mac.

## Cómo pedir trabajo

`Ejecuta el piloto` (checklist en piloto-0.1.md) o `Sigue con P13` (evidencia avanzada). P0–P12 + instrumentación piloto cerradas; no hinchar módulos sin validación docente.
