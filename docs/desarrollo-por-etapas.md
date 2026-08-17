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

## Cola

No hay etapa P10 en cola. Siguientes temas solo con acuerdo explícito (calidad pedagógica, plantillas, evidencia avanzada, tiendas).

### Fuera de cola

SIGE, libro legal, PME ministerial, DocenteMás, pagos, tiendas, sync nube multi-usuario, ERP escolar (Lirmi/Napsis).

## Cómo pedir trabajo

Indique un bug, mejora de producto o nueva etapa acordada. P0–P9 están cerradas.
