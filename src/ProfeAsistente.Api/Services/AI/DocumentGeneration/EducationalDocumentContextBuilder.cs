using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.AI;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services.AI.DocumentGeneration;

public sealed class EducationalDocumentContextBuilder
{
    private const int MaxFreeTextLength = 2000;

    private readonly ProfeAsistenteDbContext _db;
    private readonly IAiContextSanitizer _sanitizer;
    private readonly ILogger<EducationalDocumentContextBuilder> _logger;

    public EducationalDocumentContextBuilder(
        ProfeAsistenteDbContext db,
        IAiContextSanitizer sanitizer,
        ILogger<EducationalDocumentContextBuilder> logger)
    {
        _db = db;
        _sanitizer = sanitizer;
        _logger = logger;
    }

    public async Task<EducationalDocumentGenerationContext> BuildAsync(
        Guid classId,
        GenerateEducationalDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var clase = await _db.Clases
            .Include(c => c.ObjetivoAprendizaje)!.ThenInclude(o => o!.Indicadores)
            .Include(c => c.Indicadores)
            .Include(c => c.CurriculumSnapshot)
            .Include(c => c.Planificacion)!.ThenInclude(p => p!.Nivel)
            .Include(c => c.Planificacion)!.ThenInclude(p => p!.NivelAsignatura)!.ThenInclude(n => n!.Asignatura)
            .Include(c => c.Planificacion)!.ThenInclude(p => p!.Unidad)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new EducationalDocumentGenerationException("Clase no encontrada.", "ClassNotFound", 404);

        var plan = clase.Planificacion
            ?? throw new EducationalDocumentGenerationException("La clase no tiene planificación.", "PlanningMissing", 400);
        var oa = clase.ObjetivoAprendizaje
            ?? throw new EducationalDocumentGenerationException("La clase no tiene OA.", "ObjectiveMissing", 400);

        if (oa.EstadoRevision is not (EstadoRevision.Aprobado or EstadoRevision.AprobadoParaPruebas) || !oa.Vigente)
            throw new EducationalDocumentGenerationException(
                "El OA debe estar aprobado/publicado.", "ObjectiveNotPublished", 400);

        var indicatorIds = request.EvaluationIndicatorIds.Count > 0
            ? request.EvaluationIndicatorIds.Distinct().ToList()
            : clase.Indicadores.Select(i => i.IndicadorEvaluacionId).Distinct().ToList();

        if (indicatorIds.Count == 0)
            throw new EducationalDocumentGenerationException(
                "Debe seleccionar al menos un indicador.", "IndicatorsRequired", 400);

        var oaIndicatorIds = oa.Indicadores.Select(i => i.Id).ToHashSet();
        if (indicatorIds.Any(id => !oaIndicatorIds.Contains(id)))
            throw new EducationalDocumentGenerationException(
                "Uno o más indicadores no pertenecen al OA de la clase.", "InvalidIndicators", 400);

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
            .Select(h => new CurriculumSkillRef { Id = h.Id, Code = h.Codigo, Description = h.Descripcion })
            .Take(20)
            .ToListAsync(cancellationToken);

        var attitudes = await _db.Actitudes.AsNoTracking()
            .Where(a => a.NivelId == plan.NivelId && a.Vigente
                        && (a.EstadoRevision == EstadoRevision.Aprobado
                            || a.EstadoRevision == EstadoRevision.AprobadoParaPruebas))
            .OrderBy(a => a.Codigo)
            .Select(a => new CurriculumAttitudeRef { Id = a.Id, Code = a.Codigo, Description = a.Descripcion })
            .Take(20)
            .ToListAsync(cancellationToken);

        var structure = await _db.ClassStructureGenerations.AsNoTracking()
            .Where(g => g.ClassId == classId && !g.IsDeleted && g.IsCurrentVersion
                        && g.Status == AiGenerationStatus.Completed)
            .OrderByDescending(g => g.GenerationNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var warnings = new List<string>();
        if (structure is null)
            warnings.Add("No hay estructura vigente; el material se generará solo con OA e indicadores.");
        else if (structure.IsOutdated)
            warnings.Add("La estructura vigente está desactualizada.");

        var release = await ResolveReleaseAsync(oa, cancellationToken);
        var teacher = _sanitizer.Sanitize(request.TeacherInstructions, nameof(request.TeacherInstructions), maxLength: MaxFreeTextLength);
        var student = _sanitizer.Sanitize(request.StudentInstructions, nameof(request.StudentInstructions), maxLength: MaxFreeTextLength);
        warnings.AddRange(teacher.Warnings);
        warnings.AddRange(student.Warnings);

        var allowed = request.AllowedItemTypes.Count > 0
            ? request.AllowedItemTypes.Distinct().ToList()
            : DefaultItemTypes(request.DocumentType);

        var fingerprint = ComputeFingerprint(
            oa.Id, indicators.Select(i => i.Id), clase.NivelBloom,
            request.DocumentType, request.Difficulty, request.ItemCount,
            structure?.Id);

        var context = new EducationalDocumentGenerationContext
        {
            ClassId = classId,
            Level = plan.Nivel?.Nombre ?? "",
            Subject = plan.NivelAsignatura?.Asignatura?.Nombre ?? "",
            Unit = plan.Unidad?.Nombre ?? "",
            Objective = new CurriculumObjectiveRef
            {
                Id = oa.Id,
                Code = oa.Codigo,
                Description = oa.Descripcion
            },
            Indicators = indicators,
            Skills = skills,
            Attitudes = attitudes,
            BloomLevel = clase.NivelBloom,
            CurriculumRelease = release,
            SnapshotId = clase.CurriculumSnapshot?.Id,
            ClassStructureGenerationId = structure?.Id,
            ClassStructure = structure is null ? null : new ClassStructureSummaryForDocuments
            {
                GenerationId = structure.Id,
                Title = structure.GeneratedTitle,
                Purpose = structure.GeneratedPurpose,
                StartSummary = TruncateJson(structure.GeneratedStartJson),
                DevelopmentSummary = TruncateJson(structure.GeneratedDevelopmentJson),
                ClosureSummary = TruncateJson(structure.GeneratedClosureJson)
            },
            DocumentType = request.DocumentType,
            ItemCount = request.ItemCount,
            Difficulty = request.Difficulty,
            AllowedItemTypes = allowed,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            IncludeAnswerKey = request.IncludeAnswerKey,
            IncludeFeedback = request.IncludeFeedback,
            IncludeScoring = request.IncludeScoring,
            IncludeDifferentiation = request.IncludeDifferentiation,
            TeacherInstructions = teacher.Text,
            StudentInstructions = student.Text,
            ConfigurationFingerprint = fingerprint,
            Warnings = warnings,
            PromptVersion = PromptVersionFor(request.DocumentType)
        };

        _logger.LogInformation(
            "EducationalDocumentContextBuilt ClassId={ClassId} Type={Type} Indicators={Count}",
            classId, request.DocumentType, indicators.Count);
        return context;
    }

    public static string ComputeFingerprint(
        Guid oaId,
        IEnumerable<Guid> indicatorIds,
        string? bloom,
        EducationalDocumentType type,
        ItemDifficulty difficulty,
        int itemCount,
        Guid? structureId)
    {
        var raw = string.Join("|",
            oaId,
            string.Join(",", indicatorIds.OrderBy(x => x)),
            bloom ?? "",
            type,
            difficulty,
            itemCount,
            structureId?.ToString() ?? "");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    public static string PromptVersionFor(EducationalDocumentType type)
        => PromptCatalog.ForDocument(type).PromptVersion;

    private static List<EducationalItemType> DefaultItemTypes(EducationalDocumentType type) => type switch
    {
        EducationalDocumentType.LearningGuide =>
        [
            EducationalItemType.PracticalActivity,
            EducationalItemType.ProblemSolving,
            EducationalItemType.Reflection,
            EducationalItemType.ShortAnswer
        ],
        EducationalDocumentType.Exercises =>
        [
            EducationalItemType.MultipleChoice,
            EducationalItemType.ShortAnswer,
            EducationalItemType.ProblemSolving,
            EducationalItemType.TrueFalse
        ],
        EducationalDocumentType.Assessment =>
        [
            EducationalItemType.MultipleChoice,
            EducationalItemType.TrueFalse,
            EducationalItemType.OpenResponse,
            EducationalItemType.ShortAnswer
        ],
        _ => [EducationalItemType.ShortAnswer]
    };

    private void ValidateRequest(GenerateEducationalDocumentRequest request)
    {
        if (!Enum.IsDefined(request.DocumentType))
            throw new EducationalDocumentGenerationException("Tipo de documento inválido.", "InvalidDocumentType", 400);
        if (request.ItemCount is < 1 or > 50)
            throw new EducationalDocumentGenerationException("La cantidad de ítems debe estar entre 1 y 50.", "InvalidItemCount", 400);
        if (request.EstimatedDurationMinutes is not null
            && (request.EstimatedDurationMinutes < 10 || request.EstimatedDurationMinutes > 240))
            throw new EducationalDocumentGenerationException("La duración debe estar entre 10 y 240 minutos.", "InvalidDuration", 400);
        foreach (var t in request.AllowedItemTypes)
        {
            if (!Enum.IsDefined(t))
                throw new EducationalDocumentGenerationException($"Tipo de ítem inválido: {t}.", "InvalidItemType", 400);
        }
    }

    private async Task<string> ResolveReleaseAsync(Models.Curriculum.ObjetivoAprendizaje oa, CancellationToken ct)
    {
        if (oa.CurriculumReleaseId is Guid releaseId)
        {
            var release = await _db.CurriculumReleases.AsNoTracking().FirstOrDefaultAsync(r => r.Id == releaseId, ct);
            if (release is not null)
                return string.IsNullOrWhiteSpace(release.Version) ? release.Name : $"{release.Name} {release.Version}".Trim();
        }

        return oa.Version;
    }

    private static string? TruncateJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("objective", out var obj))
                return obj.GetString();
        }
        catch (JsonException)
        {
            /* ignore */
        }

        return json.Length <= 240 ? json : json[..240] + "…";
    }
}
