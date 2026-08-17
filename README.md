# ProfeAsistente

**Asistente de trabajo docente basado en el Currículum Nacional de Chile.**

Ayuda a planificar, preparar la clase, generar material alineado a OA, evaluar, leer evidencia y crear refuerzo — sin convertirse en un ERP escolar (no es SIGE / Lirmi / Napsis).

El código de solución todavía usa el prefijo técnico `AppEducativa.*`; el producto visible es **ProfeAsistente**.

## Flujo central

```text
Curso → Unidad / OA → Clase → Material → Evaluación → Evidencia → Refuerzo → siguiente clase
```

## Menú MVP

| Entrada | Qué hace |
|---------|----------|
| **Hoy** | Clases del día y pendientes |
| **Mis cursos** | Hub por curso |
| **Planificaciones** | Unidades / secuencia |
| **Biblioteca** | Guías, actividades y pruebas |
| **Configuración** | Perfil / sesión |

Administración curricular y usuarios solo con permisos.

## Etapas de producto (cerradas)

- **P0** Navegación Hoy + Mis cursos  
- **P1** Clase como hub Copilot  
- **P2** Biblioteca + lenguaje UI (Guía / Actividad / Prueba)  
- **P3** Evaluación → evidencia por OA → refuerzo  
- **P4** Limpieza técnica (README, `IAiProvider`, CORS, Documento legado deprecado)

Detalle en `docs/desarrollo-por-etapas.md`.

## Estructura

```text
AppEducativa.sln
src/
  AppEducativa.Api/
  AppEducativa.Maui/
  AppEducativa.Shared/
  AppEducativa.CurriculumImporter/
tests/
  AppEducativa.Api.Tests/
  AppEducativa.CurriculumImporter.Tests/
docs/
```

## Cómo ejecutar

```bash
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"

dotnet restore AppEducativa.sln
dotnet build AppEducativa.sln
dotnet test tests/AppEducativa.Api.Tests

# API
cd src/AppEducativa.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://127.0.0.1:5180

# MAUI (macOS Catalyst)
./run-maui.sh
```

Demo (si el seed está activo): usuario `admin` / `Admin!Pass123` · API `http://127.0.0.1:5180`.

Variable de entorno para IA: `GEMINI_API_KEY`.

## Material canónico vs legado

| Modelo | Uso |
|--------|-----|
| **EducationalDocument** | Flujo actual (clase, biblioteca, exportación Word nueva) |
| **Documento** (`/api/documentos`) | **Deprecado** — se mantiene por compatibilidad; no expandir |

## CORS

En **Development** se permite cualquier origen (salvo `Cors:RestrictInDevelopment=true`).  
En otros ambientes solo `Cors:AllowedOrigins` (vacío = sin orígenes web). Ver `appsettings.json`.

## IA

Los servicios de estructura de clase y materiales usan `IAiProvider` (hoy `GeminiAiProvider`). Gemini HTTP queda detrás de `IGeminiClient`.

## Fuera de alcance (por diseño)

SIGE, libro de clases legal, PME ministerial, DocenteMás, pagos, sync nube multi-colegio, rename completo de solución.
