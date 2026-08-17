using System.Text.Json;
using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.Curriculum;
using ProfeAsistente.CurriculumImporter.Models.Sources;
using ProfeAsistente.CurriculumImporter.Services.Extraction;
using ProfeAsistente.CurriculumImporter.Services.Parsing;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using LegacyDownloader = ProfeAsistente.CurriculumImporter.Abstractions.ISourceDownloader;
using NewDownloaded = ProfeAsistente.CurriculumImporter.Models.Download.DownloadedSource;
using NewDownloader = ProfeAsistente.CurriculumImporter.Services.Download.ISourceDownloader;
using SharedDownloaded = ProfeAsistente.Shared.Dtos.DownloadedSource;
using SharedExtraction = ProfeAsistente.Shared.Dtos.CurriculumExtractionResult;
using ICurriculumDiffService = ProfeAsistente.CurriculumImporter.Abstractions.ICurriculumDiffService;
using ICurriculumImportService = ProfeAsistente.CurriculumImporter.Abstractions.ICurriculumImportService;
using ICurriculumValidator = ProfeAsistente.CurriculumImporter.Abstractions.ICurriculumValidator;

namespace ProfeAsistente.Api.Services.Curriculum;

/// <summary>Flujo oficial: download → extract/parse → validate → review → approve → import.</summary>
public sealed class OfficialCurriculumImportOrchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private readonly ProfeAsistenteDbContext _db;
    private readonly LegacyDownloader _legacyDownloader;
    private readonly NewDownloader _newDownloader;
    private readonly ProfeAsistente.CurriculumImporter.Services.Extraction.ICurriculumExtractor _pdfExtractor;
    private readonly IProgramStudyParser _parser;
    private readonly ICurriculumValidator _validator;
    private readonly ICurriculumDiffService _diff;
    private readonly ICurriculumImportService _importer;
    private readonly SourceConfigurationLoader _sourceLoader;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<OfficialCurriculumImportOrchestrator> _logger;

    public OfficialCurriculumImportOrchestrator(
        ProfeAsistenteDbContext db,
        LegacyDownloader legacyDownloader,
        NewDownloader newDownloader,
        ProfeAsistente.CurriculumImporter.Services.Extraction.ICurriculumExtractor pdfExtractor,
        IProgramStudyParser parser,
        ICurriculumValidator validator,
        ICurriculumDiffService diff,
        ICurriculumImportService importer,
        SourceConfigurationLoader sourceLoader,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<OfficialCurriculumImportOrchestrator> logger)
    {
        _db = db;
        _legacyDownloader = legacyDownloader;
        _newDownloader = newDownloader;
        _pdfExtractor = pdfExtractor;
        _parser = parser;
        _validator = validator;
        _diff = diff;
        _importer = importer;
        _sourceLoader = sourceLoader;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task<CurriculumImportBatch> CreateImportAsync(Guid sourceId, CancellationToken ct)
    {
        var source = await _db.CurriculumSources.FindAsync([sourceId], ct)
            ?? throw new KeyNotFoundException("Fuente no encontrada.");
        return await CreateBatchForSourceAsync(source, ct);
    }

    public async Task<CurriculumImportBatch> CreateImportByExternalIdAsync(string externalId, CancellationToken ct)
    {
        await ReloadSourcesAsync(ct);
        var source = await _db.CurriculumSources.FirstOrDefaultAsync(s => s.ExternalId == externalId && s.Activo, ct)
            ?? throw new KeyNotFoundException($"Fuente externa no encontrada: {externalId}");
        return await CreateBatchForSourceAsync(source, ct);
    }

    private async Task<CurriculumImportBatch> CreateBatchForSourceAsync(CurriculumSource source, CancellationToken ct)
    {
        var batch = new CurriculumImportBatch
        {
            Id = Guid.NewGuid(),
            CurriculumSourceId = source.Id,
            SourceExternalId = source.ExternalId,
            Estado = EstadoImportBatch.EnCurso,
            Status = CurriculumImportStatus.Created,
            FechaInicio = DateTime.UtcNow
        };
        _db.CurriculumImportBatches.Add(batch);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("CurriculumImportStarted BatchId={BatchId} Source={Source}", batch.Id, source.ExternalId);
        return batch;
    }

    public async Task<CurriculumImportBatch> DownloadAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await GetBatchForUpdateAsync(batchId, ct);
        EnsureStatus(batch, CurriculumImportStatus.Created);
        var source = await _db.CurriculumSources.FindAsync([batch.CurriculumSourceId!.Value], ct)
            ?? throw new InvalidOperationException("La fuente del lote ya no existe.");

        _logger.LogInformation("CurriculumDownloadStarted BatchId={BatchId}", batchId);
        NewDownloaded downloaded;
        try
        {
            var definition = ToDefinition(source);
            downloaded = await _newDownloader.DownloadAsync(definition, ct);
        }
        catch
        {
            // Fallback legacy (file:// JSON local)
            var legacy = await _legacyDownloader.DownloadAsync(ToConfig(source), ct);
            downloaded = new NewDownloaded
            {
                SourceId = source.ExternalId ?? source.Id.ToString(),
                OriginalUrl = legacy.UrlOriginal,
                LocalFilePath = legacy.RutaArchivoLocal,
                FileName = Path.GetFileName(legacy.RutaArchivoLocal),
                ContentType = legacy.ContentType,
                SizeBytes = legacy.Content?.LongLength ?? 0,
                Sha256 = legacy.HashSha256,
                ETag = legacy.ETag,
                LastModified = DateTimeOffset.TryParse(legacy.LastModified, out var lm) ? lm : null,
                DownloadedAt = DateTimeOffset.UtcNow,
                WasNotModified = legacy.FromCache,
                Content = legacy.Content
            };
        }

        var document = await _db.CurriculumDocuments.FirstOrDefaultAsync(
            d => d.HashSha256 == downloaded.Sha256 && downloaded.Sha256 != "", ct);
        if (document is null)
        {
            document = new CurriculumDocument
            {
                Id = Guid.NewGuid(),
                CurriculumSourceId = source.Id,
                Titulo = source.Nombre,
                UrlOriginal = source.Url,
                TipoDocumento = source.TipoFuente.ToString(),
                FechaDescarga = DateTime.UtcNow,
                ETag = downloaded.ETag,
                LastModified = downloaded.LastModified?.ToString("R"),
                HashSha256 = downloaded.Sha256,
                RutaArchivoLocal = downloaded.LocalFilePath,
                ContentType = downloaded.ContentType,
                SizeBytes = downloaded.SizeBytes,
                EstadoProcesamiento = EstadoProcesamientoDocumento.Descargado
            };
            _db.CurriculumDocuments.Add(document);
        }

        batch.CurriculumDocumentId = document.Id;
        batch.Status = CurriculumImportStatus.Downloaded;
        source.FechaUltimaRevision = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("CurriculumDownloadCompleted BatchId={BatchId} Hash={Hash} NotModified={NotModified}",
            batchId, downloaded.Sha256, downloaded.WasNotModified);
        return batch;
    }

    public async Task<CurriculumImportBatch> ExtractAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await GetBatchForUpdateAsync(batchId, ct);
        EnsureStatus(batch, CurriculumImportStatus.Downloaded);
        var document = await _db.CurriculumDocuments.Include(d => d.Source)
            .FirstAsync(d => d.Id == batch.CurriculumDocumentId, ct);
        var source = document.Source ?? throw new InvalidOperationException("Documento sin fuente.");
        var definition = ToDefinition(source);
        var downloaded = new NewDownloaded
        {
            SourceId = definition.Id,
            OriginalUrl = document.UrlOriginal,
            LocalFilePath = document.RutaArchivoLocal,
            FileName = Path.GetFileName(document.RutaArchivoLocal),
            ContentType = document.ContentType ?? "application/pdf",
            SizeBytes = document.SizeBytes,
            Sha256 = document.HashSha256,
            DownloadedAt = DateTimeOffset.UtcNow
        };

        _logger.LogInformation("CurriculumExtractionStarted BatchId={BatchId}", batchId);
        SharedExtraction shared;
        if (_pdfExtractor.CanHandle(definition, downloaded))
        {
            var pages = await _pdfExtractor.ExtractAsync(definition, downloaded, ct);
            var package = await _parser.ParseAsync(definition, pages, ct);
            shared = package.ToSharedDto(definition);
            shared.ExtractedText = string.Join("\n", pages.Pages.Select(p => p.NormalizedText));
            shared.Advertencias.AddRange(pages.Warnings.Select(w => w.Message));
            document.TextoExtraidoPath = pages.ExtractedTextPath;
            document.TextoExtraido = shared.ExtractedText;
            if (pages.RequiresManualReview || package.RequiresManualReview || shared.LearningObjectives.Count == 0)
            {
                shared.Advertencias.Add("Requiere revisión manual (extracción incompleta o PDF con poco texto).");
                shared.ConfianzaExtraccion = Math.Min(shared.ConfianzaExtraccion, 0.4);
            }
            if (shared.Level is not null && shared.Level.Code == "4B")
                shared.Level.Name = "4° básico";
            if (shared.Subject is not null && shared.Subject.Code == "MAT")
                shared.Subject.Name = "Matemática";
        }
        else
        {
            var bytes = await File.ReadAllBytesAsync(document.RutaArchivoLocal, ct);
            var legacyDl = new SharedDownloaded
            {
                UrlOriginal = document.UrlOriginal,
                RutaArchivoLocal = document.RutaArchivoLocal,
                HashSha256 = document.HashSha256,
                Content = bytes,
                ContentType = document.ContentType ?? "application/octet-stream"
            };
            // Manual JSON path via existing extractors is not wired here; fail clearly.
            throw new InvalidOperationException("Extractor PDF oficial no aplicable a esta fuente.");
        }

        var json = JsonSerializer.Serialize(shared, JsonOptions);
        batch.ExtractionJson = json;
        batch.OriginalExtractionJson = json;
        batch.ExtractionJsonPath = await WriteArtifactAsync(batch.Id, "extraction.json", json, ct);
        batch.Status = CurriculumImportStatus.Extracted;
        batch.Estado = EstadoImportBatch.Extraido;
        UpdateCounts(batch, shared);
        document.FechaProcesamiento = DateTime.UtcNow;
        document.EstadoProcesamiento = shared.Errores.Count == 0
            ? EstadoProcesamientoDocumento.Extraido
            : EstadoProcesamientoDocumento.Error;
        document.ErrorProcesamiento = shared.Errores.Count == 0 ? null : string.Join("; ", shared.Errores);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("CurriculumExtractionCompleted BatchId={BatchId} OA={OA} Units={Units}",
            batchId, shared.LearningObjectives.Count, shared.Units.Count);
        return batch;
    }

    public async Task<CurriculumImportBatch> ValidateAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await GetBatchForUpdateAsync(batchId, ct);
        EnsureStatus(batch, CurriculumImportStatus.Extracted);
        var extraction = ReadExtraction(batch);
        var validation = _validator.Validate(extraction);
        batch.CantidadAdvertencias = validation.Warnings.Count;
        batch.CantidadErrores = validation.Errors.Count;
        batch.Mensaje = string.Join("; ", validation.Errors.Concat(validation.Warnings));
        // Blocking issues require human review; keep the batch available for preview edits.
        batch.Status = CurriculumImportStatus.PendingReview;
        batch.Estado = validation.IsValid ? EstadoImportBatch.Validado : EstadoImportBatch.Error;
        if (batch.CurriculumDocumentId is Guid documentId)
        {
            var doc = await _db.CurriculumDocuments.FindAsync([documentId], ct);
            if (doc is not null)
                doc.EstadoProcesamiento = validation.IsValid
                    ? EstadoProcesamientoDocumento.Validado
                    : EstadoProcesamientoDocumento.Error;
        }

        if (validation.IsValid)
        {
            var current = await BuildCurrentExtractionAsync(extraction, ct);
            var diff = _diff.Compare(extraction, current);
            batch.DiffJson = JsonSerializer.Serialize(diff, JsonOptions);
            batch.Estado = EstadoImportBatch.DiffListo;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("CurriculumValidationCompleted BatchId={BatchId} Valid={Valid}", batchId, validation.IsValid);
        return batch;
    }

    public async Task<CurriculumImportBatch> ProcessAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await GetBatchForUpdateAsync(batchId, ct);
        if (batch.Status == CurriculumImportStatus.Created)
            await DownloadAsync(batchId, ct);
        batch = await GetBatchForUpdateAsync(batchId, ct);
        if (batch.Status == CurriculumImportStatus.Downloaded)
            await ExtractAsync(batchId, ct);
        batch = await GetBatchForUpdateAsync(batchId, ct);
        if (batch.Status == CurriculumImportStatus.Extracted)
            return await ValidateAsync(batchId, ct);
        return batch;
    }

    public async Task<CurriculumImportPreviewDto> GetPreviewAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await _db.CurriculumImportBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == batchId, ct)
            ?? throw new KeyNotFoundException("Lote no encontrado.");
        return ToPreview(batch, ReadExtraction(batch));
    }

    public async Task<CurriculumImportPreviewDto> UpdatePreviewAsync(
        Guid batchId, CurriculumImportPreviewDto preview, string? user, CancellationToken ct)
    {
        var batch = await GetBatchForUpdateAsync(batchId, ct);
        if (batch.Status is not (CurriculumImportStatus.PendingReview or CurriculumImportStatus.Validated or CurriculumImportStatus.Failed))
            throw new InvalidOperationException("El lote no está disponible para corrección.");

        var before = batch.CorrectedExtractionJson ?? batch.OriginalExtractionJson ?? batch.ExtractionJson ?? "";
        var extraction = ReadExtraction(batch);
        extraction.Units = preview.Units.Select(x => new UnitExtractDto
        {
            Number = x.Number, Name = x.Name, Description = x.Description, LearningObjectiveCodes = x.LearningObjectiveCodes
        }).ToList();
        extraction.LearningObjectives = preview.Objectives.Select(x => new LearningObjectiveExtractDto
        {
            Code = x.Code, Description = x.Description, AxisName = x.AxisName
        }).ToList();
        extraction.EvaluationIndicators = preview.Indicators.Select(x => new EvaluationIndicatorExtractDto
        {
            LearningObjectiveCode = x.LearningObjectiveCode, Code = x.Code, Description = x.Description
        }).ToList();
        extraction.Skills = preview.Skills.Select(x => new SkillExtractDto { Description = x }).ToList();
        extraction.Attitudes = preview.Attitudes.Select(x => new AttitudeExtractDto { Description = x }).ToList();

        var after = JsonSerializer.Serialize(extraction, JsonOptions);
        batch.CorrectedExtractionJson = after;
        batch.CorrectedJsonPath = await WriteArtifactAsync(batch.Id, "corrected.json", after, ct);
        batch.UsuarioRevisor = user;
        batch.Status = CurriculumImportStatus.PendingReview;
        batch.ReviewChanges.Add(new CurriculumReviewChange
        {
            Id = Guid.NewGuid(),
            EntityType = "CurriculumExtraction",
            EntityKey = batch.Id.ToString(),
            Field = "preview",
            OriginalValue = Truncate(before, 4000),
            NewValue = Truncate(after, 4000),
            UsuarioRevisor = user
        });
        UpdateCounts(batch, extraction);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("CurriculumReviewUpdated BatchId={BatchId}", batchId);
        return ToPreview(batch, extraction);
    }

    public async Task<IReadOnlyList<ValidationIssueDto>> GetIssuesAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await _db.CurriculumImportBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == batchId, ct)
            ?? throw new KeyNotFoundException("Lote no encontrado.");
        var result = _validator.Validate(ReadExtraction(batch));
        return result.Errors.Select(x => new ValidationIssueDto { Severity = "Error", Blocking = true, Message = x })
            .Concat(result.Warnings.Select(x => new ValidationIssueDto { Severity = "Warning", Message = x }))
            .ToList();
    }

    public async Task ApproveAsync(Guid batchId, string? user, CancellationToken ct)
    {
        var batch = await GetBatchForUpdateAsync(batchId, ct);
        if (batch.Status != CurriculumImportStatus.ReadyForApproval)
            throw new InvalidOperationException($"El lote debe estar en ReadyForApproval; actual: {batch.Status}");
        if ((await GetIssuesAsync(batchId, ct)).Any(i => i.Blocking))
            throw new InvalidOperationException("Hay errores bloqueantes.");

        if (!string.IsNullOrWhiteSpace(batch.ReviewContentHash) && batch.ActiveReviewSessionId is Guid sessionId)
        {
            var session = await _db.CurriculumReviewSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
            if (session is not null
                && !string.Equals(session.ReviewContentHash, batch.ReviewContentHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("El contenido revisado no coincide con el hash aprobado.");
            }
        }

        batch.Status = CurriculumImportStatus.Approved;
        batch.Estado = EstadoImportBatch.Aprobado;
        batch.UsuarioRevisor = user;
        batch.FechaAprobacion = DateTime.UtcNow;
        if (batch.ActiveReviewSessionId is Guid activeSessionId)
        {
            var session = await _db.CurriculumReviewSessions.FirstOrDefaultAsync(s => s.Id == activeSessionId, ct);
            if (session is not null)
            {
                session.Estado = CurriculumReviewStatus.Approved;
                session.FechaCierre = DateTime.UtcNow;
                session.RevisadoPor = user ?? session.RevisadoPor;
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("CurriculumImportApproved BatchId={BatchId}", batchId);
    }

    public async Task RejectAsync(Guid batchId, string? user, CancellationToken ct)
    {
        var batch = await GetBatchForUpdateAsync(batchId, ct);
        batch.Status = CurriculumImportStatus.Rejected;
        batch.Estado = EstadoImportBatch.Rechazado;
        batch.UsuarioRevisor = user;
        batch.FechaTermino = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CurriculumImportResult> ImportAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await GetBatchForUpdateAsync(batchId, ct);
        EnsureStatus(batch, CurriculumImportStatus.Approved);
        if (batch.Status == CurriculumImportStatus.Imported)
            throw new InvalidOperationException("El lote ya fue importado.");

        batch.ExtractionJson = batch.FinalReviewJson
                               ?? batch.CorrectedExtractionJson
                               ?? batch.OriginalExtractionJson
                               ?? batch.ExtractionJson;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("CurriculumImportStarted Persist BatchId={BatchId}", batchId);
        var result = await _importer.ApproveBatchAsync(batchId, ct);
        if (result.Success)
        {
            // Marcar OA/unidades importados como oficiales en borrador hasta publicación.
            var extraction = ReadExtraction(batch);
            var codes = extraction.LearningObjectives.Select(o => o.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var oas = await _db.ObjetivosAprendizaje
                .Where(o => codes.Contains(o.Codigo) && o.Vigente)
                .ToListAsync(ct);
            foreach (var oa in oas)
            {
                oa.EsContenidoOficial = true;
                oa.FuenteTipo = "ProgramaEstudioOficial";
                oa.EstadoRevision = EstadoRevision.Aprobado;
                oa.PublicationStatus = CurriculumPublicationStatus.Draft;
            }

            var unitNumbers = extraction.Units.Select(u => u.Number).ToHashSet();
            var unidadesQuery = _db.Unidades.Where(u => u.Vigente && unitNumbers.Contains(u.Numero) && u.EsContenidoOficial);
            if (extraction.Level is not null && extraction.Subject is not null)
            {
                unidadesQuery = _db.Unidades
                    .Include(u => u.NivelAsignatura)!.ThenInclude(n => n!.Nivel)
                    .Include(u => u.NivelAsignatura)!.ThenInclude(n => n!.Asignatura)
                    .Where(u => u.Vigente && unitNumbers.Contains(u.Numero)
                                && u.NivelAsignatura!.Nivel!.Codigo == extraction.Level.Code
                                && u.NivelAsignatura.Asignatura!.Codigo == extraction.Subject.Code);
            }

            var unidades = await unidadesQuery.ToListAsync(ct);
            foreach (var unidad in unidades)
            {
                unidad.EsContenidoOficial = true;
                unidad.FuenteTipo = "ProgramaEstudioOficial";
                unidad.EstadoRevision = EstadoRevision.Aprobado;
                unidad.PublicationStatus = CurriculumPublicationStatus.Draft;
            }

            if (batch.CurriculumDocumentId is Guid docId)
            {
                foreach (var oa in oas)
                {
                    if (!await _db.CurriculumRecordSources.AnyAsync(r =>
                            r.CurriculumDocumentId == docId && r.EntidadId == oa.Id, ct))
                    {
                        _db.CurriculumRecordSources.Add(new CurriculumRecordSource
                        {
                            Id = Guid.NewGuid(),
                            CurriculumDocumentId = docId,
                            TipoEntidad = nameof(ObjetivoAprendizaje),
                            EntidadId = oa.Id,
                            FechaVigenciaDesde = DateTime.UtcNow
                        });
                    }
                }
            }

            batch.Status = CurriculumImportStatus.Imported;
            batch.FechaTermino = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("CurriculumImportCompleted BatchId={BatchId}", batchId);
        }
        else
        {
            batch.Status = CurriculumImportStatus.Failed;
            batch.Mensaje = string.Join("; ", result.Errores);
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("CurriculumImportFailed BatchId={BatchId}", batchId);
        }

        return result;
    }

    public async Task ReloadSourcesAsync(CancellationToken ct)
    {
        var path = ResolveSourcesPath();
        if (!File.Exists(path))
        {
            _logger.LogWarning("No se encontró curriculum-sources.json en {Path}", path);
            return;
        }

        var sources = await _sourceLoader.LoadAsync(path, ct);
        foreach (var src in sources.Where(s => s.Activo))
        {
            var entity = await _db.CurriculumSources.FirstOrDefaultAsync(x => x.ExternalId == src.Id, ct);
            if (entity is null)
            {
                entity = new CurriculumSource
                {
                    Id = Guid.NewGuid(),
                    ExternalId = src.Id,
                    Nombre = src.Nombre,
                    Url = src.Url,
                    Dominio = src.DominioPermitido,
                    TipoFuente = ParseTipo(src.TipoFuente),
                    Formato = ParseFormato(src.Formato),
                    NivelEsperado = src.NivelCodigo,
                    AsignaturaEsperada = src.AsignaturaCodigo,
                    Activo = src.Activo,
                    FechaRegistro = DateTime.UtcNow,
                    FechaUltimaRevision = DateTime.UtcNow
                };
                _db.CurriculumSources.Add(entity);
            }
            else
            {
                entity.Nombre = src.Nombre;
                entity.Url = src.Url;
                entity.Dominio = src.DominioPermitido;
                entity.TipoFuente = ParseTipo(src.TipoFuente);
                entity.Formato = ParseFormato(src.Formato);
                entity.NivelEsperado = src.NivelCodigo;
                entity.AsignaturaEsperada = src.AsignaturaCodigo;
                entity.Activo = src.Activo;
                entity.FechaUltimaRevision = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("CurriculumSourceLoaded Count={Count}", sources.Count);
    }

    private string ResolveSourcesPath()
    {
        var configured = _configuration["Curriculum:SourcesConfigPath"] ?? "Configuration/curriculum-sources.json";
        var candidates = new[]
        {
            Path.IsPathRooted(configured) ? configured : Path.Combine(_environment.ContentRootPath, configured),
            Path.Combine(_environment.ContentRootPath, "Configuration", "curriculum-sources.json"),
            Path.Combine(AppContext.BaseDirectory, "Configuration", "curriculum-sources.json"),
            Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "ProfeAsistente.CurriculumImporter", "Configuration", "curriculum-sources.json"))
        };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private async Task<CurriculumImportBatch> GetBatchForUpdateAsync(Guid id, CancellationToken ct) =>
        await _db.CurriculumImportBatches.Include(b => b.ReviewChanges).FirstOrDefaultAsync(b => b.Id == id, ct)
        ?? throw new KeyNotFoundException("Lote no encontrado.");

    private static void EnsureStatus(CurriculumImportBatch batch, CurriculumImportStatus status)
    {
        if (batch.Status != status)
            throw new InvalidOperationException($"El lote debe estar en estado {status}; estado actual: {batch.Status}.");
    }

    private Shared.Dtos.CurriculumExtractionResult ReadExtraction(CurriculumImportBatch batch) =>
        JsonSerializer.Deserialize<Shared.Dtos.CurriculumExtractionResult>(
            batch.FinalReviewJson ?? batch.CorrectedExtractionJson ?? batch.OriginalExtractionJson ?? batch.ExtractionJson
            ?? throw new InvalidOperationException("Lote sin extracción."), JsonOptions)
        ?? throw new InvalidOperationException("Extracción inválida.");

    private static void UpdateCounts(CurriculumImportBatch b, Shared.Dtos.CurriculumExtractionResult e)
    {
        b.CantidadUnidades = e.Units.Count;
        b.CantidadOA = e.LearningObjectives.Count;
        b.CantidadIndicadores = e.EvaluationIndicators.Count;
        b.CantidadHabilidades = e.Skills.Count;
        b.CantidadActitudes = e.Attitudes.Count;
        b.ConfianzaPromedio = e.ConfianzaExtraccion;
    }

    private async Task<string> WriteArtifactAsync(Guid id, string name, string content, CancellationToken ct)
    {
        var root = _configuration["Curriculum:StorageRoot"] ?? "App_Data/Curriculum";
        var dir = Path.Combine(Path.IsPathRooted(root) ? root : Path.Combine(_environment.ContentRootPath, root), "Imports", id.ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);
        await File.WriteAllTextAsync(path, content, ct);
        return path;
    }

    private static CurriculumSourceConfig ToConfig(CurriculumSource s) => new()
    {
        Nombre = s.Nombre,
        Url = s.Url,
        Tipo = s.TipoFuente.ToString(),
        Formato = s.Formato.ToString(),
        Nivel = s.NivelEsperado,
        Asignatura = s.AsignaturaEsperada,
        Activo = s.Activo
    };

    private static CurriculumSourceDefinition ToDefinition(CurriculumSource s) => new()
    {
        Id = s.ExternalId ?? s.Id.ToString("N"),
        Nombre = s.Nombre,
        Url = s.Url,
        DominioPermitido = string.IsNullOrWhiteSpace(s.Dominio) ? "www.curriculumnacional.cl" : s.Dominio,
        TipoFuente = s.TipoFuente.ToString(),
        Formato = s.Formato.ToString(),
        NivelCodigo = s.NivelEsperado,
        AsignaturaCodigo = s.AsignaturaEsperada,
        Activo = s.Activo,
        IntervaloSolicitudesMs = 1500
    };

    private static TipoFuenteCurricular ParseTipo(string t) =>
        Enum.TryParse<TipoFuenteCurricular>(t, true, out var x) ? x : TipoFuenteCurricular.ProgramaEstudio;

    private static FormatoFuenteCurricular ParseFormato(string f) => f.ToLowerInvariant() switch
    {
        "pdf" => FormatoFuenteCurricular.Pdf,
        "html" => FormatoFuenteCurricular.Html,
        "json" or "jsonmanual" => FormatoFuenteCurricular.Json,
        _ => FormatoFuenteCurricular.Pdf
    };

    private async Task<Shared.Dtos.CurriculumExtractionResult?> BuildCurrentExtractionAsync(
        Shared.Dtos.CurriculumExtractionResult extraction, CancellationToken ct)
    {
        if (extraction.Level is null || extraction.Subject is null) return null;
        var objectives = await _db.ObjetivosAprendizaje.AsNoTracking()
            .Include(x => x.NivelAsignatura)!.ThenInclude(x => x!.Nivel)
            .Include(x => x.NivelAsignatura)!.ThenInclude(x => x!.Asignatura)
            .Where(x => x.Vigente && x.EsContenidoOficial && x.EstadoRevision == EstadoRevision.Aprobado
                        && x.NivelAsignatura!.Nivel!.Codigo == extraction.Level.Code
                        && x.NivelAsignatura.Asignatura!.Codigo == extraction.Subject.Code)
            .Select(x => new LearningObjectiveExtractDto { Code = x.Codigo, Description = x.Descripcion })
            .ToListAsync(ct);
        return new Shared.Dtos.CurriculumExtractionResult
        {
            Level = extraction.Level,
            Subject = extraction.Subject,
            LearningObjectives = objectives
        };
    }

    private static CurriculumImportPreviewDto ToPreview(CurriculumImportBatch b, Shared.Dtos.CurriculumExtractionResult e) => new()
    {
        BatchId = b.Id,
        SourceExternalId = b.SourceExternalId,
        Status = b.Status.ToString(),
        ConfianzaPromedio = e.ConfianzaExtraccion,
        Units = e.Units.Select(x => new CurriculumUnitPreviewDto
        {
            Number = x.Number, Name = x.Name, Description = x.Description, LearningObjectiveCodes = x.LearningObjectiveCodes
        }).ToList(),
        Objectives = e.LearningObjectives.Select(x => new CurriculumObjectivePreviewDto
        {
            Code = x.Code, Description = x.Description, AxisName = x.AxisName
        }).ToList(),
        Indicators = e.EvaluationIndicators.Select(x => new CurriculumIndicatorPreviewDto
        {
            LearningObjectiveCode = x.LearningObjectiveCode, Code = x.Code, Description = x.Description
        }).ToList(),
        Skills = e.Skills.Select(x => x.Description).ToList(),
        Attitudes = e.Attitudes.Select(x => x.Description).ToList()
    };

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
