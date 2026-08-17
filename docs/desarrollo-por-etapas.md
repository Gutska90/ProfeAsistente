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

## Cola

No hay etapa P5 en la cola actual. Siguientes temas solo si se acuerdan explícitamente (rename de solución, migración de datos legado, etc.).

### Fuera de cola

SIGE, libro legal, PME ministerial, DocenteMás, pagos, tiendas, sync nube, ERP escolar (Lirmi/Napsis).

## Cómo pedir trabajo

Indique el objetivo concreto (bug, mejora de producto o limpieza adicional). Las etapas P0–P4 están cerradas.
