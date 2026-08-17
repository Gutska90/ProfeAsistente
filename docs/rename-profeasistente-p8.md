# P8 — Rename AppEducativa → ProfeAsistente

Cerrada: 2026-08-16.

## Objetivo

Alinear nombres técnicos con el producto: solución, proyectos, namespaces, ensamblados y variables de entorno.

## Qué cambió

| Antes | Después |
|-------|---------|
| `AppEducativa.sln` | `ProfeAsistente.sln` |
| `src/AppEducativa.*` | `src/ProfeAsistente.*` |
| `tests/AppEducativa.*` | `tests/ProfeAsistente.*` |
| Namespaces `AppEducativa.*` | `ProfeAsistente.*` |
| `AppEducativaDbContext` | `ProfeAsistenteDbContext` |
| `APPEDUCATIVA_*` (admin, JWT) | `PROFEASISTENTE_*` |
| `appeducativa.db` (default) | `profeasistente.db` |

Bundle MAUI ya era `cl.profeasistente.app` (sin cambio).

## Migración local

Si tienes una base `appeducativa.db` con datos, renómbrala a `profeasistente.db` o ajusta `ConnectionStrings:DefaultConnection`.  
Variables de entorno antiguas `APPEDUCATIVA_*` dejan de leerse: usa `PROFEASISTENTE_ADMIN_*` y `PROFEASISTENTE_JWT_KEY`.

## Fuera de P8

- Empaquetado / release Mac (P9)
- Cambiar nombre de tablas EF históricas
- Publicar en tiendas
