using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ProfeAsistente.Api.Configuration;
using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models;
using ProfeAsistente.Api.Models.AI;
using ProfeAsistente.Api.Models.Export;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ProfeAsistente.Api.Services.Export;

public sealed class WordExportService : IWordExportService
{
    public const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public const string ZipContentType = "application/zip";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly Regex UnsafeFileChars = new(@"[\\/:*?""<>|]+", RegexOptions.Compiled);

    private readonly ProfeAsistenteDbContext _db;
    private readonly ExportOptions _options;
    private readonly IWordExportValidator _validator;
    private readonly IHostEnvironment _env;
    private readonly ILogger<WordExportService> _logger;
    private readonly WordTemplateSettings _template;

    public WordExportService(
        ProfeAsistenteDbContext db,
        IOptions<ExportOptions> options,
        IWordExportValidator validator,
        IHostEnvironment env,
        ILogger<WordExportService> logger)
    {
        _db = db;
        _options = options.Value;
        _validator = validator;
        _env = env;
        _logger = logger;
        var templatePath = Path.IsPathRooted(_options.TemplateSettingsPath)
            ? _options.TemplateSettingsPath
            : Path.Combine(_env.ContentRootPath, _options.TemplateSettingsPath);
        _template = WordTemplateSettings.Load(templatePath);
        _template.Validate();
    }

    public Task<ExportResultDto> ExportPlanningAsync(Guid planningId, CreateExportRequest request, CancellationToken cancellationToken = default)
    {
        request.PlanningId = planningId;
        request.DocumentType = ExportDocumentType.Planning;
        return ExportAsync(request, cancellationToken);
    }

    public Task<ExportResultDto> ExportClassAsync(Guid classId, CreateExportRequest request, CancellationToken cancellationToken = default)
    {
        request.ClassId = classId;
        request.DocumentType = ExportDocumentType.ClassPlan;
        return ExportAsync(request, cancellationToken);
    }

    public async Task<ExportResultDto> ExportEducationalDocumentAsync(Guid documentId, CreateExportRequest request, CancellationToken cancellationToken = default)
    {
        request.EducationalDocumentId = documentId;
        if (request.DocumentType is not (ExportDocumentType.LearningGuide
            or ExportDocumentType.Exercises
            or ExportDocumentType.Assessment
            or ExportDocumentType.AnswerKey
            or ExportDocumentType.SpecificationTable))
        {
            var type = await _db.EducationalDocuments.AsNoTracking()
                .Where(d => d.Id == documentId)
                .Select(d => (EducationalDocumentType?)d.DocumentType)
                .FirstOrDefaultAsync(cancellationToken);
            request.DocumentType = type switch
            {
                EducationalDocumentType.LearningGuide => ExportDocumentType.LearningGuide,
                EducationalDocumentType.Exercises => ExportDocumentType.Exercises,
                EducationalDocumentType.Assessment => ExportDocumentType.Assessment,
                _ => ExportDocumentType.Assessment
            };
        }

        return await ExportAsync(request, cancellationToken);
    }

    public Task<ExportResultDto> ExportPlanningPackageAsync(Guid planningId, CreateExportRequest request, CancellationToken cancellationToken = default)
    {
        request.PlanningId = planningId;
        request.DocumentType = ExportDocumentType.PlanningPackage;
        return ExportAsync(request, cancellationToken);
    }

    public async Task<ExportResultDto> ExportAsync(CreateExportRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        SanitizeCustomFields(request);

        if (request.Audience == ExportAudience.Student)
        {
            request.IncludeAnswerKey = false;
            request.IncludeTeacherNotes = false;
        }

        var export = new DocumentExport
        {
            Id = Guid.NewGuid(),
            DocumentType = request.DocumentType,
            Audience = request.Audience,
            PlanningId = request.PlanningId,
            ClassId = request.ClassId,
            EducationalDocumentId = request.EducationalDocumentId,
            Status = ExportStatus.Processing,
            FileName = "pending.docx",
            ContentType = request.DocumentType == ExportDocumentType.PlanningPackage ? ZipContentType : DocxContentType,
            RequestedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow,
            OptionsJson = JsonSerializer.Serialize(request, JsonOptions),
            ExpiresAt = DateTime.UtcNow.AddDays(_options.KeepFilesForDays),
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        _db.DocumentExports.Add(export);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ExportRequested ExportId={Id} Type={Type}", export.Id, request.DocumentType);

        var warnings = new List<string>();
        string? relativePath = null;
        try
        {
            _logger.LogInformation("ExportStarted ExportId={Id}", export.Id);
            var (fileName, relative, contentType) = request.DocumentType switch
            {
                ExportDocumentType.PlanningPackage => await BuildPackageAsync(request, warnings, cancellationToken),
                ExportDocumentType.Planning => await BuildPlanningDocxAsync(request, warnings, cancellationToken),
                ExportDocumentType.ClassPlan => await BuildClassDocxAsync(request, warnings, cancellationToken),
                ExportDocumentType.AnswerKey => await BuildAnswerKeyDocxAsync(request, warnings, cancellationToken),
                ExportDocumentType.SpecificationTable => await BuildSpecTableDocxAsync(request, warnings, cancellationToken),
                _ => await BuildEducationalDocxAsync(request, warnings, cancellationToken)
            };

            relativePath = relative;
            var fullPath = ResolvePath(relative);
            var validation = contentType == ZipContentType
                ? ValidateZip(fullPath)
                : _validator.ValidateFile(fullPath);

            if (!validation.IsValid)
            {
                TryDelete(fullPath);
                export.Status = ExportStatus.Invalid;
                export.ErrorCode = "ExportValidationFailed";
                export.ErrorMessage = string.Join(" ", validation.Errors);
                export.WarningsJson = JsonSerializer.Serialize(warnings.Concat(validation.Warnings), JsonOptions);
                await _db.SaveChangesAsync(cancellationToken);
                throw new WordExportException(
                    "El archivo generado no superó la validación.", "ExportValidationFailed", 422);
            }

            if (validation.SizeBytes > _options.MaximumFileSizeMb * 1024L * 1024L)
            {
                TryDelete(fullPath);
                export.Status = ExportStatus.Failed;
                export.ErrorCode = "ExportFileTooLarge";
                export.ErrorMessage = "El archivo supera el tamaño máximo permitido.";
                await _db.SaveChangesAsync(cancellationToken);
                throw new WordExportException(export.ErrorMessage, export.ErrorCode, 413);
            }

            export.FileName = fileName;
            export.RelativeFilePath = relative;
            export.ContentType = contentType;
            export.SizeBytes = validation.SizeBytes;
            export.Sha256 = validation.Sha256 ?? (contentType == ZipContentType ? ComputeSha256(fullPath) : null);
            export.Status = ExportStatus.Completed;
            export.CompletedAt = DateTime.UtcNow;
            export.WarningsJson = JsonSerializer.Serialize(warnings.Concat(validation.Warnings).Distinct(), JsonOptions);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("ExportCompleted ExportId={Id} Size={Size}", export.Id, export.SizeBytes);
            _logger.LogInformation("ExportValidated ExportId={Id}", export.Id);
            return Map(export);
        }
        catch (WordExportException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportFailed ExportId={Id}", export.Id);
            if (relativePath is not null) TryDelete(ResolvePath(relativePath));
            export.Status = ExportStatus.Failed;
            export.ErrorCode = "ExportFailed";
            export.ErrorMessage = "No se pudo generar el documento.";
            export.WarningsJson = JsonSerializer.Serialize(warnings, JsonOptions);
            await _db.SaveChangesAsync(CancellationToken.None);
            throw new WordExportException("No se pudo generar el documento.", "ExportFailed", 500);
        }
    }

    public async Task<ExportResultDto?> GetAsync(Guid exportId, CancellationToken cancellationToken = default)
    {
        var e = await _db.DocumentExports.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == exportId && !x.IsDeleted, cancellationToken);
        return e is null ? null : Map(e);
    }

    public async Task<IReadOnlyList<ExportSummaryDto>> ListAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        return await _db.DocumentExports.AsNoTracking()
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.RequestedAt)
            .Take(Math.Clamp(take, 1, 200))
            .Select(e => new ExportSummaryDto
            {
                Id = e.Id,
                DocumentType = e.DocumentType.ToString(),
                Audience = e.Audience.ToString(),
                FileName = e.FileName,
                Status = e.Status.ToString(),
                SizeBytes = e.SizeBytes,
                RequestedAt = e.RequestedAt,
                ExpiresAt = e.ExpiresAt,
                PlanningId = e.PlanningId,
                ClassId = e.ClassId,
                EducationalDocumentId = e.EducationalDocumentId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(Stream Stream, string FileName, string ContentType)> OpenDownloadAsync(
        Guid exportId, CancellationToken cancellationToken = default)
    {
        var e = await _db.DocumentExports
            .FirstOrDefaultAsync(x => x.Id == exportId, cancellationToken)
            ?? throw new WordExportException("Exportación no encontrada.", "ExportNotFound", 404);

        if (e.IsDeleted || e.Status == ExportStatus.Expired)
            throw new WordExportException("La exportación expiró y debe generarse nuevamente.", "ExportExpired", 410);
        if (e.Status == ExportStatus.Processing || e.Status == ExportStatus.Pending)
            throw new WordExportException("La exportación aún está en proceso.", "ExportProcessing", 409);
        if (e.Status != ExportStatus.Completed)
            throw new WordExportException("La exportación no está disponible para descarga.", "ExportNotReady", 409);
        if (string.IsNullOrWhiteSpace(e.RelativeFilePath))
            throw new WordExportException("La exportación expiró y debe generarse nuevamente.", "ExportExpired", 410);

        var full = ResolvePath(e.RelativeFilePath);
        if (!File.Exists(full))
            throw new WordExportException("La exportación expiró y debe generarse nuevamente.", "ExportExpired", 410);

        if (e.ExpiresAt is not null && e.ExpiresAt < DateTime.UtcNow)
        {
            e.Status = ExportStatus.Expired;
            await _db.SaveChangesAsync(cancellationToken);
            throw new WordExportException("La exportación expiró y debe generarse nuevamente.", "ExportExpired", 410);
        }

        _logger.LogInformation("ExportDownloaded ExportId={Id}", exportId);
        var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, e.FileName, e.ContentType);
    }

    public async Task SoftDeleteAsync(Guid exportId, CancellationToken cancellationToken = default)
    {
        var e = await _db.DocumentExports.FirstOrDefaultAsync(x => x.Id == exportId, cancellationToken)
                ?? throw new WordExportException("Exportación no encontrada.", "ExportNotFound", 404);
        if (!string.IsNullOrWhiteSpace(e.RelativeFilePath))
            TryDelete(ResolvePath(e.RelativeFilePath));
        e.IsDeleted = true;
        e.Status = ExportStatus.Expired;
        e.RelativeFilePath = null;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ExportDeleted ExportId={Id}", exportId);
    }

    private async Task<(string FileName, string Relative, string ContentType)> BuildPlanningDocxAsync(
        CreateExportRequest request, List<string> warnings, CancellationToken ct)
    {
        var plan = await LoadPlanningAsync(request.PlanningId!.Value, ct);
        if (plan.Clases.Count == 0)
            warnings.Add("La planificación no tiene clases.");

        var fileName = SafeName($"Planificacion_{plan.NivelAsignatura?.Asignatura?.Nombre}_{plan.Nivel?.Nombre}_{plan.Unidad?.Nombre}") + ".docx";
        var relative = Path.Combine(NormalizeRoot(), "Planning", $"{Guid.NewGuid():N}_{fileName}");
        var full = ResolvePath(relative);

        using var builder = new WordDocumentBuilder(_template);
        builder.AddTitle(request.CustomTitle ?? plan.Nombre);
        builder.AddSubtitle($"{plan.Nivel?.Nombre} · {plan.NivelAsignatura?.Asignatura?.Nombre} · {plan.Unidad?.Nombre}");
        if (!string.IsNullOrWhiteSpace(request.SchoolName)) builder.AddParagraph($"Establecimiento: {request.SchoolName}");
        if (!string.IsNullOrWhiteSpace(request.TeacherName)) builder.AddParagraph($"Docente: {request.TeacherName}");
        builder.AddParagraph($"Periodo: {plan.FechaInicio:dd/MM/yyyy} – {plan.FechaFin:dd/MM/yyyy}");
        builder.AddParagraph($"Cantidad de clases: {plan.Clases.Count}");
        AddStatusBanner(builder, ExportAudience.Administrative, null);

        builder.AddHeading("Tabla de clases", 1);
        var headers = new[] { "N°", "Fecha", "OA", "Bloom", "Materiales" };
        var rows = plan.Clases.OrderBy(c => c.Numero).Select(c => (IReadOnlyList<string>)new[]
        {
            c.Numero.ToString(),
            c.Fecha.ToString("dd/MM"),
            c.ObjetivoAprendizaje?.Codigo ?? "",
            c.NivelBloom,
            MaterialFlags(c)
        }).ToList();
        builder.AddTable(headers, rows);

        foreach (var clase in plan.Clases.OrderBy(c => c.Numero))
        {
            if (request.PageBreakPerClass) builder.AddPageBreak();
            else builder.AddHeading($"Clase {clase.Numero}", 1);
            await AppendClassContentAsync(builder, clase, request, warnings, teacherMode: true, ct);
        }

        if (request.IncludeCurriculumReferences)
            AppendCurriculumRefs(builder, plan, student: false);

        ApplyHeaderFooter(builder, request, $"{plan.NivelAsignatura?.Asignatura?.Nombre} | {plan.Nivel?.Nombre}", "Planificación");
        await builder.SaveAsync(full, ct);
        return (fileName, relative, DocxContentType);
    }

    private async Task<(string FileName, string Relative, string ContentType)> BuildClassDocxAsync(
        CreateExportRequest request, List<string> warnings, CancellationToken ct)
    {
        var clase = await LoadClassAsync(request.ClassId!.Value, ct);
        var fileName = SafeName($"Clase_{clase.Numero:00}_{clase.ObjetivoAprendizaje?.Codigo}") + ".docx";
        var relative = Path.Combine(NormalizeRoot(), "Classes", $"{Guid.NewGuid():N}_{fileName}");
        var full = ResolvePath(relative);

        using var builder = new WordDocumentBuilder(_template);
        builder.AddTitle(request.CustomTitle ?? $"Clase {clase.Numero}");
        builder.AddSubtitle(clase.Planificacion?.Nombre ?? "Planificación");
        AddStatusBanner(builder, request.Audience, null);
        await AppendClassContentAsync(builder, clase, request, warnings, teacherMode: request.Audience != ExportAudience.Student, ct);
        if (request.IncludeCurriculumReferences)
            AppendCurriculumRefs(builder, clase.Planificacion, student: request.Audience == ExportAudience.Student);
        ApplyHeaderFooter(builder, request,
            $"{clase.Planificacion?.NivelAsignatura?.Asignatura?.Nombre}",
            $"Clase {clase.Numero}");
        await builder.SaveAsync(full, ct);
        return (fileName, relative, DocxContentType);
    }

    private async Task<(string FileName, string Relative, string ContentType)> BuildEducationalDocxAsync(
        CreateExportRequest request, List<string> warnings, CancellationToken ct)
    {
        var doc = await LoadEducationalDocumentAsync(request.EducationalDocumentId!.Value, ct);
        EnsureEducationalExportable(doc, request);

        var audienceLabel = request.Audience == ExportAudience.Student ? "Estudiante" : "Docente";
        var typeLabel = doc.DocumentType switch
        {
            EducationalDocumentType.LearningGuide => "Guia",
            EducationalDocumentType.Exercises => "Ejercicios",
            EducationalDocumentType.Assessment => "Prueba",
            _ => "Material"
        };
        var folder = doc.DocumentType switch
        {
            EducationalDocumentType.LearningGuide => "Guides",
            EducationalDocumentType.Exercises => "Exercises",
            _ => "Assessments"
        };

        var fileName = SafeName($"{typeLabel}_{doc.Title}_{audienceLabel}") + ".docx";
        var relative = Path.Combine(NormalizeRoot(), folder, $"{Guid.NewGuid():N}_{fileName}");
        var full = ResolvePath(relative);

        using var builder = new WordDocumentBuilder(_template);
        builder.AddTitle(request.CustomTitle ?? doc.Title);
        if (request.Audience == ExportAudience.Teacher)
            builder.AddWarning("VERSIÓN DOCENTE — CONTIENE RESPUESTAS");
        AddStatusBanner(builder, request.Audience, doc.Status);
        if (doc.IsOutdated)
            builder.AddWarning("Advertencia: este material fue generado con una configuración anterior de la clase.");

        AppendStudentHeaderFields(builder, request, doc);
        if (!string.IsNullOrWhiteSpace(doc.Purpose)) builder.AddParagraph($"Propósito: {doc.Purpose}");
        builder.AddInstruction(doc.Instructions);
        if (!string.IsNullOrWhiteSpace(request.AdditionalInstructions))
            builder.AddInstruction(request.AdditionalInstructions);

        var teacherMode = request.Audience != ExportAudience.Student;
        foreach (var item in doc.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order))
            AppendItem(builder, item, teacherMode, request);

        if (teacherMode && request.IncludeSpecificationTable && doc.DocumentType == EducationalDocumentType.Assessment)
            AppendSpecification(builder, doc);

        if (request.IncludeCurriculumReferences)
        {
            builder.AddHeading("Referencias curriculares", 2);
            builder.AddCurriculumReference($"OA {doc.ObjectiveCode} · {doc.CurriculumRelease} · Bloom {doc.BloomLevel}");
        }

        ApplyHeaderFooter(builder, request, request.CourseName ?? doc.ObjectiveCode, $"{typeLabel} ({audienceLabel})");
        await builder.SaveAsync(full, ct);

        // Security text scan for student docs
        if (!teacherMode)
            AssertNoSecretLeak(full, doc);

        return (fileName, relative, DocxContentType);
    }

    private async Task<(string FileName, string Relative, string ContentType)> BuildAnswerKeyDocxAsync(
        CreateExportRequest request, List<string> warnings, CancellationToken ct)
    {
        if (request.Audience == ExportAudience.Student)
            throw new WordExportException("La versión estudiante no puede incluir respuestas.", "StudentCannotIncludeAnswers", 400);

        var doc = await LoadEducationalDocumentAsync(request.EducationalDocumentId!.Value, ct);
        EnsureEducationalExportable(doc, request);
        if (!doc.Items.Any(i => !i.IsDeleted && (!string.IsNullOrWhiteSpace(i.ExpectedAnswer) || i.Options.Any(o => o.IsCorrect))))
            throw new WordExportException("El documento no contiene respuestas para exportar.", "NoAnswersAvailable", 400);

        var fileName = SafeName($"Clave_{doc.Title}") + ".docx";
        var relative = Path.Combine(NormalizeRoot(), "AnswerKeys", $"{Guid.NewGuid():N}_{fileName}");
        var full = ResolvePath(relative);

        using var builder = new WordDocumentBuilder(_template);
        builder.AddTitle($"Clave de respuestas — {doc.Title}");
        builder.AddWarning("DOCUMENTO DOCENTE — CLAVE DE RESPUESTAS");
        var headers = new[] { "N°", "Tipo", "Respuesta", "Pts", "Bloom" };
        var rows = doc.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Order).Select(i =>
        {
            var answer = i.Options.Where(o => o.IsCorrect).Select(o => o.Text).DefaultIfEmpty(i.ExpectedAnswer ?? "").First();
            return (IReadOnlyList<string>)new[]
            {
                i.Order.ToString(),
                i.ItemType.ToString(),
                answer,
                i.Points.ToString("0.##"),
                i.BloomLevel
            };
        }).ToList();
        builder.AddTable(headers, rows);
        ApplyHeaderFooter(builder, request, doc.ObjectiveCode, "Clave");
        await builder.SaveAsync(full, ct);
        return (fileName, relative, DocxContentType);
    }

    private async Task<(string FileName, string Relative, string ContentType)> BuildSpecTableDocxAsync(
        CreateExportRequest request, List<string> warnings, CancellationToken ct)
    {
        var doc = await LoadEducationalDocumentAsync(request.EducationalDocumentId!.Value, ct);
        if (doc.DocumentType != EducationalDocumentType.Assessment)
            throw new WordExportException("La tabla de especificaciones solo aplica a pruebas.", "NotAnAssessment", 400);
        EnsureEducationalExportable(doc, request);
        if (doc.Specifications.Count == 0)
            throw new WordExportException("La prueba no tiene tabla de especificaciones.", "SpecificationMissing", 400);

        var fileName = SafeName($"Tabla_Especificaciones_{doc.Title}") + ".docx";
        var relative = Path.Combine(NormalizeRoot(), "SpecificationTables", $"{Guid.NewGuid():N}_{fileName}");
        var full = ResolvePath(relative);

        using var builder = new WordDocumentBuilder(_template);
        builder.AddTitle($"Tabla de especificaciones — {doc.Title}");
        builder.AddParagraph($"OA {doc.ObjectiveCode} · {doc.CurriculumRelease}");
        AppendSpecification(builder, doc);
        ApplyHeaderFooter(builder, request, doc.ObjectiveCode, "Especificaciones");
        await builder.SaveAsync(full, ct);
        return (fileName, relative, DocxContentType);
    }

    private async Task<(string FileName, string Relative, string ContentType)> BuildPackageAsync(
        CreateExportRequest request, List<string> warnings, CancellationToken ct)
    {
        var plan = await LoadPlanningAsync(request.PlanningId!.Value, ct);
        var packageName = SafeName($"Paquete_{plan.Nombre}") + ".zip";
        var relative = Path.Combine(NormalizeRoot(), "Packages", $"{Guid.NewGuid():N}_{packageName}");
        var full = ResolvePath(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        var manifestFiles = new List<object>();
        using (var zip = ZipFile.Open(full, ZipArchiveMode.Create))
        {
            async Task AddExport(ExportDocumentType type, ExportAudience audience, Guid? classId, Guid? docId, string entryPath)
            {
                var sub = new CreateExportRequest
                {
                    DocumentType = type,
                    Audience = audience,
                    PlanningId = plan.Id,
                    ClassId = classId,
                    EducationalDocumentId = docId,
                    IncludeCurriculumReferences = request.IncludeCurriculumReferences,
                    IncludeIndicators = request.IncludeIndicators,
                    IncludeAnswerKey = audience != ExportAudience.Student && request.IncludeAnswerKey,
                    IncludeSpecificationTable = audience != ExportAudience.Student,
                    IncludeTeacherNotes = audience != ExportAudience.Student,
                    SchoolName = request.SchoolName,
                    TeacherName = request.TeacherName,
                    CourseName = request.CourseName,
                    ConfirmOutdatedExport = true,
                    PageBreakPerClass = request.PageBreakPerClass
                };
                var result = await ExportAsync(sub, ct);
                var stored = await _db.DocumentExports.AsNoTracking().FirstAsync(e => e.Id == result.ExportId, ct);
                if (stored.RelativeFilePath is null) return;
                var src = ResolvePath(stored.RelativeFilePath);
                zip.CreateEntryFromFile(src, entryPath);
                manifestFiles.Add(new
                {
                    name = entryPath,
                    type = type.ToString(),
                    audience = audience.ToString(),
                    sha256 = stored.Sha256
                });
            }

            await AddExport(ExportDocumentType.Planning, ExportAudience.Administrative, null, null, "Planificacion.docx");

            foreach (var clase in plan.Clases.OrderBy(c => c.Numero))
                await AddExport(ExportDocumentType.ClassPlan, ExportAudience.Teacher, clase.Id, null, $"Clases/Clase_{clase.Numero:00}.docx");

            var classIds = plan.Clases.Select(c => c.Id).ToList();
            var eduDocs = await _db.EducationalDocuments.AsNoTracking()
                .Where(d => classIds.Contains(d.ClassId) && !d.IsDeleted
                            && d.Status != EducationalDocumentStatus.Archived)
                .ToListAsync(ct);

            foreach (var d in eduDocs)
            {
                var folder = d.DocumentType switch
                {
                    EducationalDocumentType.LearningGuide => "Materiales/Guias",
                    EducationalDocumentType.Exercises => "Materiales/Ejercicios",
                    _ => "Materiales/Pruebas"
                };
                var baseName = SafeName(d.Title);
                await AddExport(MapType(d.DocumentType), ExportAudience.Student, null, d.Id, $"{folder}/{baseName}_Estudiante.docx");
                await AddExport(MapType(d.DocumentType), ExportAudience.Teacher, null, d.Id, $"Respuestas/Versiones_Docente/{baseName}_Docente.docx");
                if (d.DocumentType == EducationalDocumentType.Assessment)
                {
                    await AddExport(ExportDocumentType.AnswerKey, ExportAudience.Teacher, null, d.Id, $"Respuestas/Claves/Clave_{baseName}.docx");
                    await AddExport(ExportDocumentType.SpecificationTable, ExportAudience.Administrative, null, d.Id, $"Especificaciones/Tabla_{baseName}.docx");
                }
            }

            var manifest = new
            {
                planningId = plan.Id,
                planningName = plan.Nombre,
                generatedAt = DateTime.UtcNow,
                curriculumRelease = plan.Unidad?.FuenteTipo ?? "",
                files = manifestFiles
            };
            var entry = zip.CreateEntry("manifest.json");
            await using var es = entry.Open();
            await JsonSerializer.SerializeAsync(es, manifest, new JsonSerializerOptions { WriteIndented = true }, ct);
        }

        _logger.LogInformation("PlanningPackageCreated PlanningId={Id} File={File}", plan.Id, packageName);
        return (packageName, relative, ZipContentType);
    }

    private async Task AppendClassContentAsync(
        WordDocumentBuilder builder,
        Clase clase,
        CreateExportRequest request,
        List<string> warnings,
        bool teacherMode,
        CancellationToken ct)
    {
        builder.AddHeading($"Clase {clase.Numero} — {clase.Fecha:dd/MM/yyyy}", 1);
        builder.AddParagraph($"OA: {clase.ObjetivoAprendizaje?.Codigo} — {clase.ObjetivoAprendizaje?.Descripcion}");
        builder.AddParagraph($"Bloom: {clase.NivelBloom}");
        if (request.IncludeIndicators)
        {
            var inds = clase.Indicadores.Select(i => i.IndicadorEvaluacion?.Descripcion).Where(s => !string.IsNullOrWhiteSpace(s))!;
            builder.AddHeading("Indicadores", 3);
            builder.AddBulletList(inds!);
        }

        var structure = await _db.ClassStructureGenerations.AsNoTracking()
            .Where(g => g.ClassId == clase.Id && g.IsCurrentVersion && !g.IsDeleted && g.Status == AiGenerationStatus.Completed)
            .OrderByDescending(g => g.GenerationNumber)
            .FirstOrDefaultAsync(ct);

        if (structure is not null)
        {
            if (!string.IsNullOrWhiteSpace(structure.GeneratedTitle))
                builder.AddParagraph($"Título: {structure.GeneratedTitle}", bold: true);
            if (!string.IsNullOrWhiteSpace(structure.GeneratedPurpose))
                builder.AddParagraph($"Propósito: {structure.GeneratedPurpose}");
            AppendPhaseJson(builder, "Inicio", structure.GeneratedStartJson);
            AppendPhaseJson(builder, "Desarrollo", structure.GeneratedDevelopmentJson);
            AppendPhaseJson(builder, "Cierre", structure.GeneratedClosureJson);
            if (teacherMode)
            {
                AppendPhaseJson(builder, "Evaluación formativa", structure.FormativeAssessmentJson);
                AppendPhaseJson(builder, "Diferenciación", structure.DifferentiationJson);
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(clase.DescripcionInicio)
                && string.IsNullOrWhiteSpace(clase.DescripcionDesarrollo)
                && string.IsNullOrWhiteSpace(clase.DescripcionCierre))
                warnings.Add($"Clase {clase.Numero}: sin Inicio/Desarrollo/Cierre.");

            if (!string.IsNullOrWhiteSpace(clase.DescripcionInicio))
            {
                builder.AddHeading("Inicio", 2);
                builder.AddParagraph(clase.DescripcionInicio);
            }
            if (!string.IsNullOrWhiteSpace(clase.DescripcionDesarrollo))
            {
                builder.AddHeading("Desarrollo", 2);
                builder.AddParagraph(clase.DescripcionDesarrollo);
            }
            if (!string.IsNullOrWhiteSpace(clase.DescripcionCierre))
            {
                builder.AddHeading("Cierre", 2);
                builder.AddParagraph(clase.DescripcionCierre);
            }
        }
    }

    private static void AppendPhaseJson(WordDocumentBuilder builder, string title, string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        builder.AddHeading(title, 2);
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("objective", out var obj))
                builder.AddParagraph(obj.GetString() ?? "");
            if (doc.RootElement.TryGetProperty("durationMinutes", out var dur))
                builder.AddParagraph($"Duración: {dur.GetInt32()} min");
            if (doc.RootElement.TryGetProperty("teacherActions", out var ta) && ta.ValueKind == JsonValueKind.Array)
                builder.AddBulletList(ta.EnumerateArray().Select(x => x.GetString() ?? ""));
            if (doc.RootElement.TryGetProperty("studentActions", out var sa) && sa.ValueKind == JsonValueKind.Array)
                builder.AddBulletList(sa.EnumerateArray().Select(x => "Estudiante: " + (x.GetString() ?? "")));
            if (doc.RootElement.TryGetProperty("strategy", out var strategy))
                builder.AddParagraph(strategy.GetString() ?? "");
            if (doc.RootElement.TryGetProperty("supportActions", out var support) && support.ValueKind == JsonValueKind.Array)
                builder.AddBulletList(support.EnumerateArray().Select(x => x.GetString() ?? ""));
        }
        catch (JsonException)
        {
            builder.AddParagraph(json.Length > 500 ? json[..500] + "…" : json);
        }
    }

    private static void AppendItem(WordDocumentBuilder builder, EducationalItem item, bool teacherMode, CreateExportRequest request)
    {
        builder.AddHeading($"{item.Order}. {item.Statement}", 2);
        if (!string.IsNullOrWhiteSpace(item.Instructions))
            builder.AddInstruction(item.Instructions);

        if (teacherMode)
        {
            builder.AddParagraph($"Tipo: {item.ItemType} · Dificultad: {item.Difficulty} · Bloom: {item.BloomLevel} · {item.Points} pts", italic: true);
        }
        else if (request.IncludeScoringLike())
        {
            builder.AddParagraph($"({item.Points} pts)", italic: true);
        }

        switch (item.ItemType)
        {
            case EducationalItemType.MultipleChoice:
                foreach (var opt in item.Options.OrderBy(o => o.Order))
                {
                    var letter = (char)('A' + Math.Max(0, opt.Order - 1));
                    var prefix = teacherMode && opt.IsCorrect ? $"{letter}) ★ " : $"{letter}) ";
                    builder.AddParagraph(prefix + opt.Text, bold: teacherMode && opt.IsCorrect);
                }
                if (teacherMode)
                {
                    var correct = item.Options.FirstOrDefault(o => o.IsCorrect);
                    if (correct is not null)
                        builder.AddAnswer($"Respuesta correcta: {(char)('A' + Math.Max(0, correct.Order - 1))}");
                }
                break;
            case EducationalItemType.TrueFalse:
                builder.AddCheckbox("Verdadero");
                builder.AddCheckbox("Falso");
                if (teacherMode && !string.IsNullOrWhiteSpace(item.ExpectedAnswer))
                    builder.AddAnswer("Respuesta correcta: " + item.ExpectedAnswer);
                break;
            case EducationalItemType.Matching:
                var pairs = item.Options.OrderBy(o => o.Order).Select(o => (IReadOnlyList<string>)new[] { o.Text, teacherMode && o.IsCorrect ? "→ correcta" : "" }).ToList();
                if (pairs.Count > 0)
                    builder.AddTable(new[] { "Elemento", "Correspondencia" }, pairs);
                break;
            default:
                builder.AddAnswerSpace(WordDocumentBuilder.AnswerLinesForPoints(item.Points));
                if (teacherMode && !string.IsNullOrWhiteSpace(item.ExpectedAnswer))
                    builder.AddAnswer("Respuesta esperada / pauta: " + item.ExpectedAnswer);
                break;
        }

        if (teacherMode)
        {
            if (!string.IsNullOrWhiteSpace(item.Explanation))
                builder.AddParagraph("Explicación: " + item.Explanation);
            if (request.IncludeTeacherNotes && !string.IsNullOrWhiteSpace(item.TeacherNotes))
                builder.AddTeacherNote(item.TeacherNotes);
        }
    }

    private static void AppendSpecification(WordDocumentBuilder builder, EducationalDocument doc)
    {
        builder.AddHeading("Tabla de especificaciones", 1);
        var headers = new[] { "Indicador", "Bloom", "Ítems", "Puntos", "%" };
        var rows = doc.Specifications.Select(s => (IReadOnlyList<string>)new[]
        {
            s.EvaluationIndicatorId.ToString()[..8],
            s.BloomLevel,
            s.ItemCount.ToString(),
            s.TotalPoints.ToString("0.##"),
            s.WeightPercentage.ToString("0.##")
        }).ToList();
        builder.AddTable(headers, rows);
        builder.AddParagraph($"Puntaje total: {doc.TotalPoints?.ToString("0.##") ?? doc.Items.Where(i => !i.IsDeleted).Sum(i => i.Points).ToString("0.##")}");
        builder.AddParagraph($"Cantidad de preguntas: {doc.Items.Count(i => !i.IsDeleted)}");
    }

    private static void AppendStudentHeaderFields(WordDocumentBuilder builder, CreateExportRequest request, EducationalDocument doc)
    {
        if (!string.IsNullOrWhiteSpace(request.SchoolName)) builder.AddParagraph($"Establecimiento: {request.SchoolName}");
        if (!string.IsNullOrWhiteSpace(request.CourseName)) builder.AddParagraph($"Curso: {request.CourseName}");
        if (!string.IsNullOrWhiteSpace(request.TeacherName)) builder.AddParagraph($"Docente: {request.TeacherName}");
        builder.AddParagraph("Nombre del estudiante: ________________________________");
        builder.AddParagraph($"Fecha: ____________    Duración: {doc.EstimatedDurationMinutes?.ToString() ?? "—"} min");
        if (doc.DocumentType == EducationalDocumentType.Assessment)
        {
            builder.AddParagraph($"Puntaje ideal: {doc.TotalPoints?.ToString("0.##") ?? "—"}    Puntaje obtenido: ______    Nota: ______");
        }
    }

    private static void AppendCurriculumRefs(WordDocumentBuilder builder, Planificacion? plan, bool student)
    {
        if (plan is null) return;
        builder.AddHeading("Referencias curriculares", 2);
        builder.AddCurriculumReference($"{plan.Nivel?.Nombre} · {plan.NivelAsignatura?.Asignatura?.Nombre} · {plan.Unidad?.Nombre}");
        if (!student)
            builder.AddCurriculumReference($"Fuente unidad: {plan.Unidad?.FuenteTipo}");
    }

    private static void ApplyHeaderFooter(WordDocumentBuilder builder, CreateExportRequest request, string? left, string docLabel)
    {
        if (request.IncludeHeader)
            builder.AddHeader($"ProfeAsistente | {left}");
        if (request.IncludeFooter)
            builder.AddFooter($"{docLabel} · {request.Audience}", request.IncludePageNumbers);
    }

    private static void AddStatusBanner(WordDocumentBuilder builder, ExportAudience audience, EducationalDocumentStatus? status)
    {
        if (status == EducationalDocumentStatus.Draft)
            builder.AddWarning("BORRADOR");
        else if (status == EducationalDocumentStatus.UnderReview)
            builder.AddWarning("EN REVISIÓN");
        else if (status == EducationalDocumentStatus.Final)
            builder.AddParagraph("Versión final", italic: true);
        _ = audience;
    }

    private void EnsureEducationalExportable(EducationalDocument doc, CreateExportRequest request)
    {
        if (doc.IsDeleted || doc.Status == EducationalDocumentStatus.Archived)
            throw new WordExportException("No se puede exportar un documento archivado o eliminado.", "DocumentNotExportable", 400);
        if (doc.Items.Count(i => !i.IsDeleted) == 0)
            throw new WordExportException(
                doc.DocumentType == EducationalDocumentType.Assessment
                    ? "La prueba no contiene preguntas."
                    : "La guía/ejercicios no contienen actividades.",
                "DocumentEmpty", 400);
        if (doc.IsOutdated && !_options.AllowOutdatedDocuments && !request.ConfirmOutdatedExport)
            throw new WordExportException(
                "El material está desactualizado. Confirme la exportación (confirmOutdatedExport=true).",
                "OutdatedConfirmationRequired", 409);
        if (request.Audience == ExportAudience.Student && (request.IncludeAnswerKey || request.IncludeTeacherNotes))
            throw new WordExportException("La versión estudiante no puede incluir respuestas.", "StudentCannotIncludeAnswers", 400);
    }

    private void AssertNoSecretLeak(string filePath, EducationalDocument doc)
    {
        var text = ExtractPlainText(filePath);
        foreach (var marker in new[] { "Respuesta correcta", "ExpectedAnswer", "TeacherNotes", "IsCorrect", "Clave de respuestas", "Pauta docente", "Nota docente:" })
        {
            if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
                throw new WordExportException(
                    "La versión estudiante contiene contenido docente prohibido.",
                    "StudentContentLeak", 500);
        }

        foreach (var secret in doc.Items.Where(i => !i.IsDeleted)
                     .SelectMany(i => new[] { i.ExpectedAnswer, i.Explanation, i.TeacherNotes })
                     .Where(s => !string.IsNullOrWhiteSpace(s) && s!.Length >= 12))
        {
            if (text.Contains(secret!, StringComparison.Ordinal))
                throw new WordExportException(
                    "La versión estudiante contiene una respuesta o pauta.",
                    "StudentContentLeak", 500);
        }
    }

    private static string ExtractPlainText(string filePath)
    {
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false);
        var sb = new StringBuilder();
        foreach (var t in doc.MainDocumentPart!.Document.Body!.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
            sb.AppendLine(t.Text);
        return sb.ToString();
    }

    private async Task<Planificacion> LoadPlanningAsync(Guid id, CancellationToken ct)
    {
        return await _db.Planificaciones
                   .Include(p => p.Nivel)
                   .Include(p => p.NivelAsignatura)!.ThenInclude(n => n!.Asignatura)
                   .Include(p => p.Unidad)
                   .Include(p => p.Clases.OrderBy(c => c.Numero)).ThenInclude(c => c.ObjetivoAprendizaje)
                   .Include(p => p.Clases).ThenInclude(c => c.Indicadores).ThenInclude(i => i.IndicadorEvaluacion)
                   .Include(p => p.Clases).ThenInclude(c => c.Documentos)
                   .FirstOrDefaultAsync(p => p.Id == id, ct)
               ?? throw new WordExportException("Planificación no encontrada.", "PlanningNotFound", 404);
    }

    private async Task<Clase> LoadClassAsync(Guid id, CancellationToken ct)
    {
        return await _db.Clases
                   .Include(c => c.ObjetivoAprendizaje)
                   .Include(c => c.Indicadores).ThenInclude(i => i.IndicadorEvaluacion)
                   .Include(c => c.Documentos)
                   .Include(c => c.Planificacion)!.ThenInclude(p => p!.Nivel)
                   .Include(c => c.Planificacion)!.ThenInclude(p => p!.NivelAsignatura)!.ThenInclude(n => n!.Asignatura)
                   .Include(c => c.Planificacion)!.ThenInclude(p => p!.Unidad)
                   .FirstOrDefaultAsync(c => c.Id == id, ct)
               ?? throw new WordExportException("Clase no encontrada.", "ClassNotFound", 404);
    }

    private async Task<EducationalDocument> LoadEducationalDocumentAsync(Guid id, CancellationToken ct)
    {
        return await _db.EducationalDocuments
                   .Include(d => d.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Options)
                   .Include(d => d.Items).ThenInclude(i => i.Indicators)
                   .Include(d => d.Specifications)
                   .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, ct)
               ?? throw new WordExportException("Documento educativo no encontrado.", "DocumentNotFound", 404);
    }

    private void ValidateRequest(CreateExportRequest request)
    {
        switch (request.DocumentType)
        {
            case ExportDocumentType.Planning:
            case ExportDocumentType.PlanningPackage:
                if (request.PlanningId is null || request.PlanningId == Guid.Empty)
                    throw new WordExportException("PlanningId es obligatorio.", "PlanningIdRequired", 400);
                break;
            case ExportDocumentType.ClassPlan:
                if (request.ClassId is null || request.ClassId == Guid.Empty)
                    throw new WordExportException("ClassId es obligatorio.", "ClassIdRequired", 400);
                break;
            case ExportDocumentType.LearningGuide:
            case ExportDocumentType.Exercises:
            case ExportDocumentType.Assessment:
            case ExportDocumentType.AnswerKey:
            case ExportDocumentType.SpecificationTable:
                if (request.EducationalDocumentId is null || request.EducationalDocumentId == Guid.Empty)
                    throw new WordExportException("EducationalDocumentId es obligatorio.", "DocumentIdRequired", 400);
                break;
            default:
                throw new WordExportException("Tipo de exportación inválido.", "InvalidExportType", 400);
        }
    }

    private static void SanitizeCustomFields(CreateExportRequest request)
    {
        static string? Clip(string? s) => string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length > 200 ? s.Trim()[..200] : s.Trim());
        request.SchoolName = Clip(request.SchoolName);
        request.TeacherName = Clip(request.TeacherName);
        request.CourseName = Clip(request.CourseName);
        request.CustomTitle = Clip(request.CustomTitle);
        request.AdditionalInstructions = string.IsNullOrWhiteSpace(request.AdditionalInstructions)
            ? null
            : request.AdditionalInstructions.Trim()[..Math.Min(2000, request.AdditionalInstructions.Trim().Length)];
    }

    private string NormalizeRoot() => _options.RootPath.Replace('\\', '/').Trim('/');

    private string ResolvePath(string relative)
    {
        if (Path.IsPathRooted(relative)) return relative;
        return Path.Combine(_env.ContentRootPath, relative);
    }

    private static string SafeName(string? raw)
    {
        var s = UnsafeFileChars.Replace(raw ?? "documento", "_");
        s = Regex.Replace(s, @"\s+", "_").Trim('_');
        if (s.Length > 80) s = s[..80];
        return string.IsNullOrWhiteSpace(s) ? "documento" : s;
    }

    private static string MaterialFlags(Clase c)
    {
        var docs = c.Documentos ?? [];
        var parts = new List<string>();
        if (docs.Any(d => d.Tipo == TipoDocumento.Guia)) parts.Add("Guía");
        if (docs.Any(d => d.Tipo == TipoDocumento.Ejercicios)) parts.Add("Ejerc.");
        if (docs.Any(d => d.Tipo == TipoDocumento.Prueba)) parts.Add("Prueba");
        return parts.Count == 0 ? "—" : string.Join(", ", parts);
    }

    private static ExportDocumentType MapType(EducationalDocumentType t) => t switch
    {
        EducationalDocumentType.LearningGuide => ExportDocumentType.LearningGuide,
        EducationalDocumentType.Exercises => ExportDocumentType.Exercises,
        _ => ExportDocumentType.Assessment
    };

    private WordExportValidationResult ValidateZip(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length <= 0)
            return new WordExportValidationResult { IsValid = false, Errors = ["ZIP vacío."] };
        try
        {
            using var zip = ZipFile.OpenRead(path);
            if (zip.Entries.Count == 0)
                return new WordExportValidationResult { IsValid = false, Errors = ["ZIP sin entradas."] };
            return new WordExportValidationResult
            {
                IsValid = true,
                SizeBytes = info.Length,
                Sha256 = ComputeSha256(path)
            };
        }
        catch (Exception ex)
        {
            return new WordExportValidationResult { IsValid = false, Errors = [ex.Message] };
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }

    private static ExportResultDto Map(DocumentExport e) => new()
    {
        ExportId = e.Id,
        Status = e.Status.ToString(),
        DocumentType = e.DocumentType.ToString(),
        Audience = e.Audience.ToString(),
        FileName = e.FileName,
        ContentType = e.ContentType,
        SizeBytes = e.SizeBytes,
        Sha256 = e.Sha256,
        RequestedAt = e.RequestedAt,
        CompletedAt = e.CompletedAt,
        ExpiresAt = e.ExpiresAt,
        Warnings = DeserializeWarnings(e.WarningsJson),
        ErrorCode = e.ErrorCode,
        ErrorMessage = e.ErrorMessage
    };

    private static List<string> DeserializeWarnings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? []; }
        catch { return []; }
    }
}

internal static class ExportRequestExtensions
{
    public static bool IncludeScoringLike(this CreateExportRequest _) => true;
}
