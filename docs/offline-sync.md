# Offline y cola de envío (etapa 5)

La app guarda en el dispositivo una **copia de lectura** de lo que ya se abrió con la API y una **cola FIFO** de escrituras del flujo diario. Al reconectar, esas escrituras se envían a la API local. No hay sync entre dispositivos ni con la nube.

## Qué funciona sin red (si se abrió una vez en línea)

- Lista y detalle de planificaciones, ficha de clase, dashboard.
- Guardar clase, marcar realizada, asistencia, DUA de la clase, puntajes.

## Qué no está cubierto

- Crear planificación, clase o evaluación nueva (hace falta el Id del servidor).
- Generar IA, exportar, estructura avanzada, materiales.
- Sync entre dos teléfonos, Google, SIGE.

## Conflictos

La cola se procesa en orden de creación. Si un envío falla, **se detiene** para no reordenar operaciones. Reintente con «Sincronizar ahora» en Inicio o Perfil.

## Archivo local

`FileSystem.AppDataDirectory/offline-sync.json` (caché + outbox).
