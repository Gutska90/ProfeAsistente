# Establecimientos y cursos

## EducationalInstitution

Campos: Name, ShortName, Rbd (opcional), InstitutionType, dirección, TimeZoneId, IsActive, soft-delete.

Tipos: Municipal, SubsidizedPrivate, Private, PublicLocalService, TechnicalProfessional, Other.

## Membresía

`POST /api/institutions/{id}/members` con `userId` y `role`.

## Período académico

`POST /api/institutions/{institutionId}/academic-periods`

Validaciones: fechas coherentes, un período actual por establecimiento, cierre vía `/api/academic-periods/{id}/close`.

## Curso

`POST /api/institutions/{institutionId}/courses` — ligado a `LevelId` curricular publicado.

Asignaturas: `POST /api/courses/{id}/subjects` (`SubjectId` = asignatura global).

## Profesores

`POST /api/course-subjects/{id}/teachers`

Tipos: PrimaryTeacher, CoTeacher, Substitute, Reviewer, Assistant.

Como máximo un profesor principal activo por asignatura (salvo override administrativo futuro).

## Planificación asociada

Al crear planificación incluir `InstitutionId`, `AcademicPeriodId`, `SchoolCourseId`, `CourseSubjectId`, `Visibility`.

Visibilidades: Private, CourseTeachers, Institution (`SharedByLink` deshabilitado en MVP).
