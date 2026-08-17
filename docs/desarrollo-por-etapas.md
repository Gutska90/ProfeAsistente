# Desarrollo por etapas

Regla: **una etapa activa a la vez**. No abrir sync nube, pulido total de UI, SIGE ni más módulos de aula hasta cerrar la etapa en curso.

Cada etapa termina cuando:

1. Compila la solución.
2. Pasan las pruebas existentes (no se bajan).
3. Hay un flujo usable en MAUI o API documentado.
4. Se anota qué quedó fuera.

No lanzar API, MAUI y pruebas en paralelo de forma indiscriminada: un cambio de dominio → migración → API → una pantalla → pruebas → parar.

## Ya cerrado (no reabrir salvo bugs)

| Etapa | Qué quedó |
|-------|-----------|
| A. Currículum | Importar, revisar, publicar OA |
| B. Planificación | Plan, clases, calendario, secuencia, cobertura |
| C. Materiales | Estructura, guías, pruebas, DOCX |
| D. Seguridad | Usuarios, JWT, establecimientos, cursos |
| E. Aula local | Nómina, PIE/DUA, asistencia, completar clase, dashboard |
| 1. Flujo diario | Inscripción al curso, DUA en ficha de clase, asistencia desde nómina, menú por permiso |
| 2. Calendario usable | Vista mes/semana (lunes–domingo), abrir clase, reprogramar al día elegido |
| 3. Evaluación en clase | Formativa/sumativa desde la ficha, puntajes y nivel de logro de la nómina |
| 4. Interfaz docente | Tipografía, color, tarjetas y menos botones en el flujo diario |
| 5. Offline / cola | Caché de lectura + outbox FIFO hacia la API local. Sin sync entre dispositivos ni nube |

## Cola

No hay etapa siguiente en cola.

### Fuera de cola (no implementar)

SIGE, libro de clases legal, PME ministerial, DocenteMás, pagos, tiendas, colaboración en tiempo real, sync nube / multi-dispositivo.

## Cómo pedir trabajo

Si aparece un bug de una etapa cerrada, descríbalo. No pedir “haz el 100%” ni varias etapas juntas.
