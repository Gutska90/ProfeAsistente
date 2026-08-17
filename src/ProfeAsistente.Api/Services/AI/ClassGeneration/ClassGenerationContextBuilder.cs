using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.AI;
using ProfeAsistente.Api.Models.Curriculum;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services.AI.ClassGeneration;

public sealed class ClassGenerationContextBuilder
{
    private const int MaxFreeTextLength = 2000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly ProfeAsistenteDbContext _db;
    private readonly IAiContextSanitizer _sanitizer;
    private readonly ILogger<ClassGenerationContextBuilder> _logger;

    public ClassGenerationContextBuilder(
        ProfeAsistenteDbContext db,
        IAiContextSanitizer sanitizer,
        ILogger<ClassGenerationContextBuilder> logger)
    {
        _db = db;
        _sanitizer = sanitizer;
        _logger = logger;
    }

    public async Task<ClassGenerationContext> BuildAsync(
        Guid classId,
        GenerateClassStructureRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequestBasics(request);

        var clase = await _db.Clases
            .Include(c => c.ObjetivoAprendizaje)!.ThenInclude(o => o!.Indicadores)
            .Include(c => c.ObjetivoAprendizaje)!.ThenInclude(o => o!.EjeCurricular)
            .Include(c => c.Indicadores).ThenInclude(i => i.IndicadorEvaluacion)
            .Include(c => c.CurriculumSnapshot)
            .Include(c => c.Planificacion)!.ThenInclude(p => p!.Nivel)
            .Include(c => c.Planificacion)!.ThenInclude(p => p!.NivelAsignatura)!.ThenInclude(n => n!.Asignatura)
            .Include(c => c.Planificacion)!.ThenInclude(p => p!.Unidad)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new ClassGenerationException("Clase no encontrada.", "ClassNotFound", 404);

        var plan = clase.Planificacion
            ?? throw new ClassGenerationException("La clase no tiene planificación.", "PlanningMissing", 400);
        var oa = clase.ObjetivoAprendizaje
            ?? throw new ClassGenerationException("La clase no tiene OA cargado.", "ObjectiveMissing", 400);

        EnsureObjectiveUsable(oa);

        var indicatorIds = request.EvaluationIndicatorIds.Count > 0
            ? request.EvaluationIndicatorIds.Distinct().ToList()
            : clase.Indicadores.Select(i => i.IndicadorEvaluacionId).Distinct().ToList();

        if (indicatorIds.Count == 0 && oa.Indicadores.Count > 0)
            throw new ClassGenerationException(
                "Debe seleccionar al menos un indicador de evaluación.",
                "IndicatorsRequired",
                400);

        var oaIndicatorIds = oa.Indicadores.Select(i => i.Id).ToHashSet();
        var unknown = indicatorIds.Where(id => !oaIndicatorIds.Contains(id)).ToList();
        if (unknown.Count > 0)
            throw new ClassGenerationException(
                "Uno o más indicadores no pertenecen al OA de la clase.",
                "InvalidIndicators",
                400);

        var indicators = oa.Indicadores
            .Where(i => indicatorIds.Contains(i.Id) && i.Vigente)
            .OrderBy(i => i.Orden)
            .Select(i => new CurriculumIndicatorRef
            {
                Id = i.Id,
                Code = i.Codigo,
                Description = i.Descripcion
            })
            .ToList();

        var skills = await _db.Habilidades.AsNoTracking()
            .Where(h => h.NivelAsignaturaId == plan.NivelAsignaturaId && h.Vigente
                        && (h.EstadoRevision == EstadoRevision.Aprobado
                            || h.EstadoRevision == EstadoRevision.AprobadoParaPruebas))
            .OrderBy(h => h.Codigo)
            .Select(h => new CurriculumSkillRef
            {
                Id = h.Id,
                Code = h.Codigo,
                Description = h.Descripcion
            })
            .ToListAsync(cancellationToken);

        var attitudes = await _db.Actitudes.AsNoTracking()
            .Where(a => a.NivelId == plan.NivelId && a.Vigente
                        && (a.EstadoRevision == EstadoRevision.Aprobado
                            || a.EstadoRevision == EstadoRevision.AprobadoParaPruebas))
            .OrderBy(a => a.Codigo)
            .Select(a => new CurriculumAttitudeRef
            {
                Id = a.Id,
                Code = a.Codigo,
                Description = a.Descripcion
            })
            .ToListAsync(cancellationToken);

        var oats = new List<CurriculumTransversalRef>();
        if (request.TransversalObjectiveIds.Count > 0)
        {
            var oatIds = request.TransversalObjectiveIds.Distinct().ToList();
            oats = await _db.Oats.AsNoTracking()
                .Where(o => oatIds.Contains(o.Id) && o.Vigente
                            && (o.NivelId == null || o.NivelId == plan.NivelId)
                            && (o.EstadoRevision == EstadoRevision.Aprobado
                                || o.EstadoRevision == EstadoRevision.AprobadoParaPruebas))
                .Select(o => new CurriculumTransversalRef
                {
                    Id = o.Id,
                    Code = o.Codigo,
                    Description = o.Descripcion
                })
                .ToListAsync(cancellationToken);

            if (oats.Count != oatIds.Count)
                throw new ClassGenerationException(
                    "Uno o más OAT no existen, no están publicados o no corresponden al nivel.",
                    "InvalidTransversalObjectives",
                    400);
        }

        var release = await ResolveCurriculumReleaseAsync(oa, cancellationToken);
        var levelName = plan.Nivel?.Nombre ?? string.Empty;
        var subjectName = plan.NivelAsignatura?.NombreEnNivel
                          ?? plan.NivelAsignatura?.Asignatura?.Nombre
                          ?? string.Empty;
        var unitName = plan.Unidad?.Nombre ?? string.Empty;
        var axisName = oa.EjeCurricular?.Nombre;

        var snapshot = await UpsertSnapshotAsync(
            clase, oa, indicators, skills, attitudes, release, levelName, subjectName, unitName, axisName, cancellationToken);

        var warnings = new List<string>();
        var previousKnowledge = SanitizeFreeText(request.PreviousKnowledge, warnings, nameof(request.PreviousKnowledge));
        var availableResources = SanitizeFreeText(request.AvailableResources, warnings, nameof(request.AvailableResources));
        var studentContext = SanitizeFreeText(request.StudentContext, warnings, nameof(request.StudentContext));
        var teacherInstructions = SanitizeFreeText(request.TeacherInstructions, warnings, nameof(request.TeacherInstructions));

        var bloom = string.IsNullOrWhiteSpace(clase.NivelBloom) ? "Aplicar" : clase.NivelBloom.Trim();
        var fingerprint = ComputeFingerprint(oa.Id, indicators.Select(i => i.Id), bloom, request.DurationMinutes);

        _logger.LogInformation(
            "ClassGenerationContextBuilt ClassId={ClassId} Objective={Code} Indicators={Count} SnapshotId={SnapshotId}",
            classId, oa.Codigo, indicators.Count, snapshot.Id);

        return new ClassGenerationContext
        {
            ClassId = classId,
            Level = levelName,
            Subject = subjectName,
            Unit = unitName,
            Axis = axisName,
            Objective = new CurriculumObjectiveRef
            {
                Id = oa.Id,
                Code = oa.Codigo,
                Description = oa.Descripcion
            },
            Indicators = indicators,
            Skills = skills,
            Attitudes = attitudes,
            TransversalObjectives = oats,
            BloomLevel = bloom,
            DurationMinutes = request.DurationMinutes,
            PreviousKnowledge = previousKnowledge,
            AvailableResources = availableResources,
            StudentContext = studentContext,
            TeacherInstructions = teacherInstructions,
            CurriculumRelease = release,
            SnapshotId = snapshot.Id,
            ConfigurationFingerprint = fingerprint,
            IncludeFormativeAssessment = request.IncludeFormativeAssessment,
            IncludeDifferentiation = request.IncludeDifferentiation,
            Warnings = warnings
        };
    }

    public static string ComputeFingerprint(
        Guid objectiveId,
        IEnumerable<Guid> indicatorIds,
        string bloomLevel,
        int durationMinutes)
    {
        var sorted = string.Join(",", indicatorIds.OrderBy(x => x).Select(x => x.ToString("N")));
        var raw = $"{objectiveId:N}|{sorted}|{bloomLevel.Trim().ToLowerInvariant()}|{durationMinutes}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private static void ValidateRequestBasics(GenerateClassStructureRequest request)
    {
        if (request.DurationMinutes is < 30 or > 240)
            throw new ClassGenerationException(
                "La duración debe estar entre 30 y 240 minutos.",
                "InvalidDuration",
                400);
    }

    private static void EnsureObjectiveUsable(ObjetivoAprendizaje oa)
    {
        if (!oa.Vigente)
            throw new ClassGenerationException(
                "El OA de la clase no está vigente.",
                "ObjectiveNotPublished",
                400);

        var demoOk = oa.EstadoRevision == EstadoRevision.AprobadoParaPruebas;
        var publishedOk = oa.PublicationStatus == CurriculumPublicationStatus.Published
                          && (oa.EstadoRevision == EstadoRevision.Aprobado
                              || oa.EstadoRevision == EstadoRevision.AprobadoParaPruebas);

        if (oa.EsContenidoOficial && oa.PublicationStatus != CurriculumPublicationStatus.Published && !demoOk)
            throw new ClassGenerationException(
                "El OA oficial no está publicado. No se puede generar estructura con contenido no publicado.",
                "ObjectiveNotPublished",
                400);

        if (!publishedOk && !demoOk)
            throw new ClassGenerationException(
                "El OA debe estar aprobado/publicado o aprobado para pruebas.",
                "ObjectiveNotPublished",
                400);
    }

    private async Task<string> ResolveCurriculumReleaseAsync(ObjetivoAprendizaje oa, CancellationToken ct)
    {
        if (oa.CurriculumReleaseId is Guid releaseId)
        {
            var release = await _db.CurriculumReleases.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == releaseId, ct);
            if (release is not null)
                return string.IsNullOrWhiteSpace(release.Version) ? release.Name : $"{release.Name} {release.Version}".Trim();
        }

        var latest = await _db.CurriculumReleases.AsNoTracking()
            .Where(r => r.Status == CurriculumPublicationStatus.Published)
            .OrderByDescending(r => r.PublishedAt)
            .FirstOrDefaultAsync(ct);

        if (latest is not null)
            return string.IsNullOrWhiteSpace(latest.Version) ? latest.Name : $"{latest.Name} {latest.Version}".Trim();

        return oa.Version;
    }

    private async Task<ClaseCurriculumSnapshot> UpsertSnapshotAsync(
        Models.Clase clase,
        ObjetivoAprendizaje oa,
        List<CurriculumIndicatorRef> indicators,
        List<CurriculumSkillRef> skills,
        List<CurriculumAttitudeRef> attitudes,
        string release,
        string levelName,
        string subjectName,
        string unitName,
        string? axisName,
        CancellationToken ct)
    {
        var snapshot = await _db.ClaseCurriculumSnapshots
            .FirstOrDefaultAsync(s => s.ClaseId == clase.Id, ct);

        if (snapshot is null)
        {
            snapshot = new ClaseCurriculumSnapshot
            {
                Id = Guid.NewGuid(),
                ClaseId = clase.Id
            };
            _db.ClaseCurriculumSnapshots.Add(snapshot);
        }

        snapshot.ObjetivoAprendizajeId = oa.Id;
        snapshot.CodigoOA = oa.Codigo;
        snapshot.DescripcionOA = oa.Descripcion;
        snapshot.IndicadoresJson = JsonSerializer.Serialize(indicators, JsonOptions);
        // Pack display names into HabilidadesJson until a migration adds dedicated columns.
        snapshot.HabilidadesJson = JsonSerializer.Serialize(new
        {
            items = skills,
            context = new
            {
                nivelNombre = levelName,
                asignaturaNombre = subjectName,
                unidadNombre = unitName,
                ejeNombre = axisName
            }
        }, JsonOptions);
        snapshot.ActitudesJson = JsonSerializer.Serialize(attitudes, JsonOptions);
        snapshot.VersionCurricular = release;
        snapshot.FechaSnapshot = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return snapshot;
    }

    private string? SanitizeFreeText(string? value, List<string> warnings, string fieldName)
    {
        var result = _sanitizer.Sanitize(value, fieldName, maxLength: MaxFreeTextLength);
        warnings.AddRange(result.Warnings);
        return result.Text;
    }
}
