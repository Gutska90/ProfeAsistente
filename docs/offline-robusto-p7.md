# P7 — Offline más robusto

Cerrada: 2026-08-16.

## Objetivo

Endurecer la cola local y la caché de lectura **sin** sync multi-dispositivo ni nube.

## Qué cambió

- `OutboxProcessor.Coalesce`: fusiona `PUT` repetidos al mismo path
- Flush reporta error/intentos; snapshot guarda `LastSuccessfulFlushAt` / `LastFlushError`
- Auto-flush al recuperar conectividad
- Guardado atómico del JSON
- Prefetch al abrir clase (nómina, asistencia, DUA, evaluaciones)
- UI Hoy/Perfil con estado de sync más claro
- Tests: `OfflineOutboxTests`

Ver [offline-sync.md](offline-sync.md).

## Fuera de P7

Sync nube, conflictos multi-dispositivo, crear entidades nuevas offline, cola de generaciones IA.
