# Offline y cola de envío (P7)

La app guarda en el dispositivo una **copia de lectura** de lo que ya se abrió con la API y una **cola FIFO** de escrituras del flujo diario. Al reconectar, esas escrituras se envían a la API local. No hay sync entre dispositivos ni con la nube.

## Qué funciona sin red (si se abrió una vez en línea)

- Lista y detalle de planificaciones, ficha de clase, dashboard.
- Guardar clase, marcar realizada, asistencia, DUA de la clase, puntajes.
- Al abrir una clase **en línea**, se precargan nómina, asistencia, DUA, evaluaciones y puntajes recientes (mejor experiencia offline después).

## Robustez (P7)

| Mejora | Detalle |
|--------|---------|
| Coalesce PUT | Varios `PUT` al mismo path → solo el cuerpo más reciente |
| Error visible | `StatusText` / Hoy / Perfil muestran el último error de flush |
| Auto-flush | Al recuperar red (`Connectivity`), intenta vaciar la cola |
| Guardado atómico | `offline-sync.json` vía archivo `.tmp` + `Move` |
| Intentos | Cada fallo incrementa `Attempts`; tras 5 se alerta en el mensaje |

## Qué no está cubierto

- Crear planificación, clase o evaluación nueva (hace falta el Id del servidor).
- Generar IA, exportar, estructura avanzada, materiales.
- Sync entre dos teléfonos, Google, SIGE.

## Conflictos

La cola se procesa en orden de creación (tras coalesce). Si un envío falla, **se detiene** para no reordenar operaciones. Reintente con «Sincronizar ahora» en Hoy o Configuración/Perfil.

## Archivo local

`FileSystem.AppDataDirectory/offline-sync.json` (caché + outbox + última sync/error).
