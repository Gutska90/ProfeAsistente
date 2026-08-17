# AppEducativa / ProfeAsistente

Planificador de clases (Chile). **Núcleo estabilizado (Prompt 2):** currículum demo → planificaciones → clases.  
El importador oficial y Gemini quedan en el repo pero no son el foco de esta iteración.

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
```

## Comandos

```bash
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"

# Restaurar / compilar
dotnet restore AppEducativa.sln
dotnet build AppEducativa.sln

# Migraciones EF (herramienta local del repo)
dotnet tool restore
dotnet tool run dotnet-ef migrations add InitialCreate \
  --project src/AppEducativa.Api \
  --startup-project src/AppEducativa.Api \
  --output-dir Data/Migrations   # solo si aún no existe

dotnet tool run dotnet-ef database update \
  --project src/AppEducativa.Api \
  --startup-project src/AppEducativa.Api

# API
cd src/AppEducativa.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://127.0.0.1:5180
# Perfiles: http://localhost:5047 · https://localhost:7047 (launchSettings)

# Pruebas
dotnet test tests/AppEducativa.Api.Tests
dotnet test tests/AppEducativa.CurriculumImporter.Tests

# MAUI (macOS Catalyst)
./run-maui.sh
# Windows (en máquina Windows):
#   cd src/AppEducativa.Maui && dotnet build -t:Run -f net8.0-windows10.0.19041.0
```

## URLs

| Recurso | URL |
|---|---|
| Health | http://127.0.0.1:5180/api/health |
| Swagger | http://127.0.0.1:5180/swagger |
| SQLite | `src/AppEducativa.Api/appeducativa.db` (ruta absoluta se registra en logs al iniciar) |

## Seed demostrativo

Si la BD está vacía, se carga **4° básico · Matemática · unidad Fracciones (demo)** con OA `DEMO OA 01..03`.  
Marcadores: `FuenteTipo = SeedDemostracion`, `EsContenidoOficial = false`, `EstadoRevision = AprobadoParaPruebas`.  
**No es contenido oficial MINEDUC.**

## Endpoints mínimos

- `GET /api/health`
- `GET /api/curriculum/niveles|asignaturas|unidades|objetivos|objetivos/{id}/detalle`
- `POST/GET /api/planificaciones`, `GET /api/planificaciones/{id}`
- `POST /api/planificaciones/{id}/clases`
- `GET/PUT /api/clases/{id}`

## Pruebas manuales

1. Abrir Swagger → `GET /api/curriculum/niveles` (debe listar 4° básico).
2. Cascada: asignaturas → unidades → objetivos (códigos DEMO).
3. `POST /api/planificaciones` con IDs del seed y fechas válidas → 201.
4. `POST .../clases` con OA de la unidad y fecha dentro del rango → 201.
5. En MAUI: lista → nueva planificación → detalle → agregar clase.

## Compilación

Confirmada en este entorno (macOS arm64, .NET 8): API, Shared, Importer, Maui (Mac Catalyst) y tests.  
**MAUI Windows** no se ejecutó aquí (requiere Windows + workload); el `csproj` ya incluye `net8.0-windows10.0.19041.0` cuando `IsOSPlatform('windows')`.
