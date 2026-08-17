# Calendario de planificación

## Configuración de horario

`PUT /api/planificaciones/{id}/calendario/configuracion`

- Rango `StartDate`/`EndDate` (DateOnly local escolar).
- Zona horaria por defecto: `America/Santiago`.
- Días activos con `StartTime`, `DurationMinutes` (30–240), `SessionsPerDay` (1–5).
- Fechas excluidas con motivo y `PlanningExclusionType`.

## Generación de sesiones

`POST .../calendario/generar` y `.../regenerar`

1. Recorre el rango.
2. Aplica días configurados.
3. Excluye feriados/actividades.
4. Numera cronológicamente.
5. Conserva sesiones manuales y bloqueadas.
6. No elimina clases con estructura o materiales (conflicto → confirmación).

Vista previa: `POST .../calendario/vista-previa`.

## Reprogramar / cancelar / bloquear

- `POST /api/calendario/sesiones/{id}/reprogramar` — conserva `ClassId`, estructura y materiales.
- `POST .../cancelar` — no borra la clase.
- `POST .../bloquear` / `desbloquear`.

## Almacenamiento

Entidades en SQLite: `PlanningScheduleConfiguration`, `WeeklyClassSchedule`, `PlanningExcludedDate`, `PlanningCalendarSession`, `PlanningSessionHistory`.

## Limitaciones

- Sin API externa de feriados; importación JSON/CSV manual.
- Cola en segundo plano no implementada.

## App docente (MAUI)

Desde el detalle de planificación → **Calendario**:

- **Mes:** grilla lunes–domingo; el día con clases se marca en verde. Toque para ver las sesiones.
- **Semana:** siete días con hora, OA y estado.
- **Lista:** todas las sesiones de la unidad.
- Abrir clase (si ya hay `ClassId`), cancelar, o mover la sesión elegida al día seleccionado.
- Horario: días de clase y exclusiones a mano (no hay feriados automáticos de Chile).
