# Autorización

## Roles (`ApplicationRole`)

- `SystemAdministrator` — usuarios, establecimientos, seguridad, auditoría, mantenimiento.
- `CurriculumAdministrator` — importación/revisión/publicación curricular.
- `SchoolAdministrator` — cursos, profesores, períodos del establecimiento.
- `Teacher` — planificaciones y materiales propios.
- `Reviewer` — revisión y comentarios.
- `ReadOnly` — solo consulta.

Los roles **agrupan** permisos (`PermissionCatalog`). No basar la seguridad solo en el nombre del rol.

## Permisos explícitos

Ejemplos: `Users.View`, `Planning.Create`, `Planning.ViewOwn`, `Curriculum.Publish`, `Materials.Export`, `Audit.View`, `System.Configure`.

Lista completa: `ProfeAsistente.Shared.Security.AppPermissions`.

## Policies

Configuradas en `SecurityRegistration`:

- `RequireSystemAdministrator`, `RequireCurriculumAdministrator`, `RequireSchoolAdministrator`, `RequireTeacher`
- `CanManageUsers`, `CanManageCurriculum`, `CanCreatePlanning`, `CanReviewPlanning`, `CanExportMaterials`, `CanViewAudit`
- `CurriculumAdmin` (compatibilidad; en Development permite acceso anónimo al importador local)

Uso: `[Authorize(Policy = AppPolicies.CanCreatePlanning)]`

## Servicio de usuario actual

`ICurrentUserService` expone UserId, Roles, Permisos, Instituciones y `ActiveInstitutionId` (claim o encabezado `X-Institution-Id` validado contra membresías).

No leer claims directamente en controllers.

## Autorización por recurso

`IResourceAuthorizationService`:

- Planificación: propietario, institución, asignación docente, visibilidad, permiso.
- Exportación: acceso a la planificación + `Materials.Export`.

Conocer el ID no otorga acceso; consultas denegadas responden 404/403.

## Membresía por establecimiento

`InstitutionMembership` define el rol **por establecimiento**. Un usuario puede ser Teacher en A y Reviewer en B.
