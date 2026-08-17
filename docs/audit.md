# Auditoría de acceso

## Entidad `AuditEvent`

UserId, InstitutionId, Action, EntityType/EntityId, Success, Timestamp, Ip, UserAgent, TraceId, DetailsJson, FailureReason.

## Eventos registrados

Login exitoso/fallido, lockout, logout, refresh, reutilización de refresh, cambio/reset de contraseña, usuario creado/desactivado, roles, membresías, cursos, asignación docente, planificación (creación), exportaciones, currículum publicado, denegaciones relevantes.

## No registrar

Contraseñas, tokens, API keys, contenido completo de documentos, datos sensibles innecesarios.

## API

```
GET /api/admin/audit
GET /api/admin/audit/{id}
GET /api/admin/audit/export
```

Filtros: usuario, institución, acción, entidad, fechas, éxito, traceId. Paginación. Policy `CanViewAudit`.
