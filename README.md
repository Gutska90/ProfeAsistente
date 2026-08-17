# ProfeAsistente

**Asistente de trabajo docente basado en el Currículum Nacional de Chile.**

Ayuda a planificar, preparar la clase, generar material alineado a OA, evaluar, leer evidencia y crear refuerzo — sin convertirse en un ERP escolar (no es SIGE / Lirmi / Napsis).

Namespaces y proyectos: `ProfeAsistente.*` (API, MAUI, Shared, CurriculumImporter).

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

- **P0–P4** Navegación, clase Copilot, biblioteca, evidencia, limpieza técnica  
- **P5** Consolidación técnica  
- **P6** Flujo clase end-to-end  
- **P7** Offline robusto  
- **P8** Rename `AppEducativa` → `ProfeAsistente`  
- **P9** Empaquetado piloto Mac Catalyst  

Detalle en `docs/desarrollo-por-etapas.md`.

## Estructura

```text
ProfeAsistente.sln
src/
  ProfeAsistente.Api/
  ProfeAsistente.Maui/
  ProfeAsistente.Shared/
  ProfeAsistente.CurriculumImporter/
tests/
  ProfeAsistente.Api.Tests/
  ProfeAsistente.CurriculumImporter.Tests/
docs/
```

## Cómo ejecutar

```bash
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"

dotnet restore ProfeAsistente.sln
dotnet build ProfeAsistente.sln
dotnet test tests/ProfeAsistente.Api.Tests

# API
cd src/ProfeAsistente.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://127.0.0.1:5180

# MAUI (macOS Catalyst, desarrollo)
./run-maui.sh

# Piloto empaquetado (Release)
./scripts/smoke-release.sh
./scripts/publish-piloto-mac.sh
./scripts/start-piloto.sh
```

Demo (si el seed está activo): usuario `admin` / `Admin!Pass123` · API `http://127.0.0.1:5180`.

Variables útiles: `GEMINI_API_KEY`, y en no-Development `PROFEASISTENTE_JWT_KEY` (+ `PROFEASISTENTE_ADMIN_*` en el primer arranque).

## Material canónico vs legado

| Modelo | Uso |
|--------|-----|
| **EducationalDocument** | Flujo actual (clase, biblioteca, exportación Word nueva) |
| **Documento** (`/api/documentos`) | **Retirado (P5)** — responde **410 Gone**; no expandir |

## CORS

En **Development** se permite cualquier origen (salvo `Cors:RestrictInDevelopment=true`).  
En otros ambientes solo `Cors:AllowedOrigins` (vacío = sin orígenes web). Ver `appsettings.json`.

## IA

Los servicios de estructura de clase y materiales usan `IAiProvider` (hoy `GeminiAiProvider`). Gemini HTTP queda detrás de `IGeminiClient`.

## Fuera de alcance (por diseño)

SIGE, libro de clases legal, PME ministerial, DocenteMás, pagos, sync nube multi-colegio.
