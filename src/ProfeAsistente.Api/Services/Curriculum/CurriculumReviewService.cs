using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.Curriculum;
using ProfeAsistente.CurriculumImporter.Models.Review;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using ICurriculumValidator = ProfeAsistente.CurriculumImporter.Abstractions.ICurriculumValidator;

namespace ProfeAsistente.Api.Services.Curriculum;

public sealed class CurriculumReviewService : ICurriculumReviewService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions NormalizedJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ProfeAsistenteDbContext _db;
    private readonly ICurriculumValidator _validator;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CurriculumReviewService> _logger;

    public CurriculumReviewService(
        ProfeAsistenteDbContext db,
        ICurriculumValidator validator,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<CurriculumReviewService> logger)
    {
        _db = db;
        _validator = validator;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task<CurriculumReviewSessionDto> StartReviewAsync(
        Guid importBatchId, string? reviewer, CancellationToken cancellationToken = default)
    {
        var batch = await GetBatchAsync(importBatchId, cancellationToken);
        if (batch.Status is not (CurriculumImportStatus.PendingReview or CurriculumImportStatus.Validated
            or CurriculumImportStatus.Failed or CurriculumImportStatus.ReadyForApproval))
        {
            throw new CurriculumReviewException(
                $"El lote debe estar en PendingReview, Validated, Failed o ReadyForApproval; actual: {batch.Status}", 409);
        }

        if (batch.ActiveReviewSessionId is Guid activeId)
        {
            var existing = await _db.CurriculumReviewSessions.FirstOrDefaultAsync(s => s.Id == activeId, cancellationToken);
            if (existing is not null && existing.Estado == CurriculumReviewStatus.InProgress)
            {
                if (!string.IsNullOrWhiteSpace(reviewer))
                    existing.RevisadoPor = reviewer;
                await _db.SaveChangesAsync(cancellationToken);
                return ToSessionDto(existing, batch.Id);
            }
        }

        var extraction = ReadExtraction(batch);
        var package = BuildPackageFromExtraction(extraction, batch);
        var now = DateTime.UtcNow;
        var session = new CurriculumReviewSession
        {
            Id = Guid.NewGuid(),
            CurriculumImportBatchId = batch.Id,
            Estado = CurriculumReviewStatus.InProgress,
            FechaInicio = now,
            FechaUltimaModificacion = now,
            RevisadoPor = reviewer,
            VersionRevision = 1,
            RowVersion = Guid.NewGuid().ToByteArray(),
            ReviewPackageJson = JsonSerializer.Serialize(package, JsonOptions)
        };

        _db.CurriculumReviewSessions.Add(session);
        batch.ActiveReviewSessionId = session.Id;
        batch.Status = CurriculumImportStatus.PendingReview;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("CurriculumReviewStarted BatchId={BatchId} SessionId={SessionId}", batch.Id, session.Id);
        return ToSessionDto(session, batch.Id);
    }

    public async Task<CurriculumReviewPackageDto?> GetReviewPackageAsync(
        Guid importBatchId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryLoadActiveAsync(importBatchId, cancellationToken);
        return ctx is null ? null : ToPackageDto(ctx.Value.Batch, ctx.Value.Session, ctx.Value.Package);
    }

    public Task<CurriculumReviewPackageDto> UpdateUnitAsync(
        Guid importBatchId, string unitTemporaryId, UpdateReviewUnitRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(importBatchId, request.RowVersion, cancellationToken, (batch, session, package, user) =>
        {
            var unit = FindUnit(package, unitTemporaryId);
            if (request.Number is int n && unit.Number != n)
                Track(batch, session, "Unit", unit.TemporaryId, "Number", unit.Number.ToString(), n.ToString(), request.Reason, user);
            if (request.Name is not null && unit.Name != request.Name)
                Track(batch, session, "Unit", unit.TemporaryId, "Name", unit.Name, request.Name, request.Reason, user);
            if (request.Description is not null && unit.Description != request.Description)
                Track(batch, session, "Unit", unit.TemporaryId, "Description", unit.Description, request.Description, request.Reason, user);
            if (request.SuggestedHours is int h && unit.SuggestedHours != h)
                Track(batch, session, "Unit", unit.TemporaryId, "SuggestedHours", unit.SuggestedHours?.ToString(), h.ToString(), request.Reason, user);
            if (request.Order is int o && unit.Order != o)
                Track(batch, session, "Unit", unit.TemporaryId, "Order", unit.Order.ToString(), o.ToString(), request.Reason, user);

            var fieldsChanged = request.Number is not null || request.Name is not null || request.Description is not null
                                || request.SuggestedHours is not null || request.Order is not null;
            if (request.Number is int num) unit.Number = num;
            if (request.Name is not null) unit.Name = request.Name;
            if (request.Description is not null) unit.Description = request.Description;
            if (request.SuggestedHours is not null) unit.SuggestedHours = request.SuggestedHours;
            if (request.Order is int ord) unit.Order = ord;
            ApplyDecision(unit, fieldsChanged, request.Decision, () => unit.Decision, d => unit.Decision = d, () => unit.WasManuallyModified = true);
        });

    public Task<CurriculumReviewPackageDto> UpdateObjectiveAsync(
        Guid importBatchId, string objectiveTemporaryId, UpdateReviewObjectiveRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(importBatchId, request.RowVersion, cancellationToken, (batch, session, package, user) =>
        {
            var oa = FindObjective(package, objectiveTemporaryId);
            if (request.Code is not null && oa.Code != request.Code)
                Track(batch, session, "LearningObjective", oa.TemporaryId, "Code", oa.Code, request.Code, request.Reason, user);
            if (request.Description is not null && oa.Description != request.Description)
                Track(batch, session, "LearningObjective", oa.TemporaryId, "Description", oa.Description, request.Description, request.Reason, user);
            if (request.AxisTemporaryId is not null && oa.AxisTemporaryId != request.AxisTemporaryId)
                Track(batch, session, "LearningObjective", oa.TemporaryId, "AxisTemporaryId", oa.AxisTemporaryId, request.AxisTemporaryId, request.Reason, user);
            if (request.UnitTemporaryIds is not null)
            {
                var prev = string.Join(",", oa.UnitTemporaryIds);
                var next = string.Join(",", request.UnitTemporaryIds);
                if (!string.Equals(prev, next, StringComparison.Ordinal))
                    Track(batch, session, "LearningObjective", oa.TemporaryId, "UnitTemporaryIds", prev, next, request.Reason, user,
                        CurriculumReviewChangeType.RelationshipChange);
            }

            var fieldsChanged = request.Code is not null || request.Description is not null
                                || request.AxisTemporaryId is not null || request.UnitTemporaryIds is not null;
            if (request.Code is not null) oa.Code = request.Code;
            if (request.Description is not null) oa.Description = request.Description;
            if (request.AxisTemporaryId is not null) oa.AxisTemporaryId = request.AxisTemporaryId;
            if (request.UnitTemporaryIds is not null)
            {
                SyncUnitLinks(package, oa, request.UnitTemporaryIds);
                oa.UnitTemporaryIds = request.UnitTemporaryIds.ToList();
            }

            oa.LastModifiedAt = DateTimeOffset.UtcNow;
            oa.LastModifiedBy = user;
            ApplyDecision(oa, fieldsChanged, request.Decision, () => oa.Decision, d => oa.Decision = d, () => oa.WasManuallyModified = true);
        });

    public Task<CurriculumReviewPackageDto> UpdateIndicatorAsync(
        Guid importBatchId, string indicatorTemporaryId, UpdateReviewIndicatorRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(importBatchId, request.RowVersion, cancellationToken, (batch, session, package, user) =>
        {
            var ind = FindIndicator(package, indicatorTemporaryId);
            if (request.Code is not null && ind.Code != request.Code)
                Track(batch, session, "EvaluationIndicator", ind.TemporaryId, "Code", ind.Code, request.Code, request.Reason, user);
            if (request.Description is not null && ind.Description != request.Description)
                Track(batch, session, "EvaluationIndicator", ind.TemporaryId, "Description", ind.Description, request.Description, request.Reason, user);
            if (request.ObjectiveTemporaryId is not null && ind.ObjectiveTemporaryId != request.ObjectiveTemporaryId)
            {
                _ = FindObjective(package, request.ObjectiveTemporaryId);
                Track(batch, session, "EvaluationIndicator", ind.TemporaryId, "ObjectiveTemporaryId",
                    ind.ObjectiveTemporaryId, request.ObjectiveTemporaryId, request.Reason, user,
                    CurriculumReviewChangeType.RelationshipChange);
                ind.ObjectiveTemporaryId = request.ObjectiveTemporaryId;
            }

            if (request.Order is int o && ind.Order != o)
                Track(batch, session, "EvaluationIndicator", ind.TemporaryId, "Order", ind.Order.ToString(), o.ToString(), request.Reason, user);

            var fieldsChanged = request.Code is not null || request.Description is not null
                                || request.ObjectiveTemporaryId is not null || request.Order is not null;
            if (request.Code is not null) ind.Code = request.Code;
            if (request.Description is not null) ind.Description = request.Description;
            if (request.Order is int ord) ind.Order = ord;
            ApplyDecision(ind, fieldsChanged, request.Decision, () => ind.Decision, d => ind.Decision = d, () => ind.WasManuallyModified = true);
        });

    public Task<CurriculumReviewPackageDto> UpdateSkillAsync(
        Guid importBatchId, string skillTemporaryId, UpdateReviewSkillRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(importBatchId, request.RowVersion, cancellationToken, (batch, session, package, user) =>
        {
            var skill = package.Skills.FirstOrDefault(s => s.TemporaryId == skillTemporaryId)
                ?? throw new CurriculumReviewException($"Habilidad no encontrada: {skillTemporaryId}", 404);
            if (request.Description is not null && skill.Description != request.Description)
                Track(batch, session, "Skill", skill.TemporaryId, "Description", skill.Description, request.Description, request.Reason, user);
            var fieldsChanged = request.Description is not null;
            if (request.Description is not null) skill.Description = request.Description;
            ApplyDecision(skill, fieldsChanged, request.Decision, () => skill.Decision, d => skill.Decision = d, () => skill.WasManuallyModified = true);
        });

    public Task<CurriculumReviewPackageDto> UpdateAttitudeAsync(
        Guid importBatchId, string attitudeTemporaryId, UpdateReviewAttitudeRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(importBatchId, request.RowVersion, cancellationToken, (batch, session, package, user) =>
        {
            var attitude = package.Attitudes.FirstOrDefault(a => a.TemporaryId == attitudeTemporaryId)
                ?? throw new CurriculumReviewException($"Actitud no encontrada: {attitudeTemporaryId}", 404);
            if (request.Description is not null && attitude.Description != request.Description)
                Track(batch, session, "Attitude", attitude.TemporaryId, "Description", attitude.Description, request.Description, request.Reason, user);
            var fieldsChanged = request.Description is not null;
            if (request.Description is not null) attitude.Description = request.Description;
            ApplyDecision(attitude, fieldsChanged, request.Decision, () => attitude.Decision, d => attitude.Decision = d, () => attitude.WasManuallyModified = true);
        });

    public Task<CurriculumReviewPackageDto> AddObjectiveAsync(
        Guid importBatchId, AddReviewObjectiveRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(importBatchId, request.RowVersion, cancellationToken, (batch, session, package, user) =>
        {
            if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Description))
                throw new CurriculumReviewException("Código y descripción del OA son obligatorios.");

            var tempId = $"oa-{package.NextOaSeq:D3}";
            package.NextOaSeq++;
            var oa = new ReviewableLearningObjective
            {
                TemporaryId = tempId,
                Code = request.Code.Trim(),
                Description = request.Description.Trim(),
                ExtractedCode = request.Code.Trim(),
                ExtractedDescription = request.Description.Trim(),
                Decision = CurriculumRecordDecision.Corrected,
                WasManuallyModified = true,
                LastModifiedAt = DateTimeOffset.UtcNow,
                LastModifiedBy = user,
                UnitTemporaryIds = string.IsNullOrWhiteSpace(request.UnitTemporaryId)
                    ? []
                    : [request.UnitTemporaryId]
            };
            if (!string.IsNullOrWhiteSpace(request.UnitTemporaryId))
            {
                var unit = FindUnit(package, request.UnitTemporaryId);
                if (!unit.LearningObjectiveTemporaryIds.Contains(tempId))
                    unit.LearningObjectiveTemporaryIds.Add(tempId);
            }

            package.Objectives.Add(oa);
            Track(batch, session, "LearningObjective", tempId, "*", null, request.Code, request.Reason, user,
                CurriculumReviewChangeType.RecordAdded);
        });

    public Task<CurriculumReviewPackageDto> AddIndicatorAsync(
        Guid importBatchId, string objectiveTemporaryId, AddReviewIndicatorRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(importBatchId, request.RowVersion, cancellationToken, (batch, session, package, user) =>
        {
            var oa = FindObjective(package, objectiveTemporaryId);
            if (string.IsNullOrWhiteSpace(request.Description))
                throw new CurriculumReviewException("La descripción del indicador es obligatoria.");

            var seq = package.Indicators.Count(i => i.ObjectiveTemporaryId == oa.TemporaryId) + 1;
            var tempId = $"{oa.TemporaryId}-ind-{seq:D3}";
            while (package.Indicators.Any(i => i.TemporaryId == tempId))
            {
                seq++;
                tempId = $"{oa.TemporaryId}-ind-{seq:D3}";
            }

            package.NextIndicatorSeq = Math.Max(package.NextIndicatorSeq, seq + 1);
            var ind = new ReviewableEvaluationIndicator
            {
                TemporaryId = tempId,
                Code = request.Code,
                Description = request.Description.Trim(),
                ExtractedDescription = request.Description.Trim(),
                ObjectiveTemporaryId = oa.TemporaryId,
                Order = seq,
                Decision = CurriculumRecordDecision.Corrected,
                WasManuallyModified = true
            };
            package.Indicators.Add(ind);
            Track(batch, session, "EvaluationIndicator", tempId, "*", null, request.Description, request.Reason, user,
                CurriculumReviewChangeType.RecordAdded);
        });

    public Task<CurriculumReviewPackageDto> DeleteRecordAsync(
        Guid importBatchId, string entityType, string temporaryId, DeleteReviewRecordRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(importBatchId, request.RowVersion, cancellationToken, (batch, session, package, user) =>
        {
            SoftDelete(package, entityType, temporaryId, request.Reason, user);
            Track(batch, session, NormalizeEntityType(entityType), temporaryId, "IsDeleted", "false", "true",
                request.Reason, user, CurriculumReviewChangeType.RecordRemoved);
        });

    public Task<CurriculumReviewPackageDto> RestoreRecordAsync(
        Guid importBatchId, string entityType, string temporaryId, string? rowVersion,
        CancellationToken cancellationToken = default) =>
        MutateAsync(importBatchId, rowVersion, cancellationToken, (batch, session, package, user) =>
        {
            Restore(package, entityType, temporaryId);
            Track(batch, session, NormalizeEntityType(entityType), temporaryId, "IsDeleted", "true", "false",
                "Restauración", user);
        });

    public async Task RevertChangeAsync(
        Guid importBatchId, Guid changeId, string? rowVersion, CancellationToken cancellationToken = default)
    {
        var (batch, session, package) = await LoadActiveAsync(importBatchId, cancellationToken);
        EnsureEditable(session);
        EnsureRowVersion(session, rowVersion);
        var change = await _db.CurriculumReviewChanges
            .FirstOrDefaultAsync(c => c.Id == changeId && c.CurriculumImportBatchId == batch.Id, cancellationToken)
            ?? throw new CurriculumReviewException("Cambio no encontrado.", 404);
        if (change.IsReverted)
            throw new CurriculumReviewException("El cambio ya fue revertido.");
        if (change.CurriculumReviewSessionId is Guid sid && sid != session.Id)
            throw new CurriculumReviewException("El cambio no pertenece a la sesión activa.");

        var user = session.RevisadoPor;
        ApplyRevert(package, change);
        change.IsReverted = true;
        change.RevertedAt = DateTime.UtcNow;
        Track(batch, session, change.EntityType, change.EntityTemporaryId, change.FieldName,
            change.NewValue, change.PreviousValue, "Reversión", user, CurriculumReviewChangeType.Reverted);
        BumpConcurrency(session);
        InvalidateReady(batch, session);
        session.FechaUltimaModificacion = DateTime.UtcNow;
        session.ReviewPackageJson = JsonSerializer.Serialize(package, JsonOptions);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CurriculumValidationResultDto> RevalidateAsync(
        Guid importBatchId, CancellationToken cancellationToken = default)
    {
        var (batch, session, package) = await LoadActiveAsync(importBatchId, cancellationToken);
        EnsureEditable(session);

        var extraction = PackageToExtraction(package, includePending: true);
        var validation = _validator.Validate(extraction);
        var issues = ToIssueDtos(validation);
        AttachIssues(package, issues);

        session.IssuesJson = JsonSerializer.Serialize(issues, JsonOptions);
        session.LastValidationAt = DateTime.UtcNow;
        // No tocar FechaUltimaModificacion: la revalidación no es una edición de contenido.
        session.ReviewPackageJson = JsonSerializer.Serialize(package, JsonOptions);
        batch.CantidadErrores = validation.Errors.Count;
        batch.CantidadAdvertencias = validation.Warnings.Count;
        await _db.SaveChangesAsync(cancellationToken);

        return new CurriculumValidationResultDto
        {
            IsValid = validation.IsValid,
            CanMarkReady = CanMarkReady(package, session, issues, out _),
            Issues = issues,
            ValidatedAt = session.LastValidationAt.Value
        };
    }

    public async Task MarkReadyForApprovalAsync(
        Guid importBatchId, string? user, CancellationToken cancellationToken = default)
    {
        var (batch, session, package) = await LoadActiveAsync(importBatchId, cancellationToken);
        EnsureEditable(session);

        if (session.LastValidationAt is null || session.LastValidationAt < session.FechaUltimaModificacion)
            await RevalidateAsync(importBatchId, cancellationToken);

        (batch, session, package) = await LoadActiveAsync(importBatchId, cancellationToken);

        if (session.LastDiffAt is null || session.LastDiffAt < session.FechaUltimaModificacion)
            await GetRichDiffAsync(importBatchId, cancellationToken);

        (batch, session, package) = await LoadActiveAsync(importBatchId, cancellationToken);
        var issues = session.IssuesJson is null
            ? []
            : JsonSerializer.Deserialize<List<ValidationIssueDto>>(session.IssuesJson, JsonOptions) ?? [];

        if (await HasUnresolvedBlockingCommentsAsync(session.Id, cancellationToken))
            throw new CurriculumReviewException("No se puede marcar listo mientras existan comentarios bloqueantes sin resolver.");

        if (!CanMarkReady(package, session, issues, out var reason))
            throw new CurriculumReviewException(reason);

        var finalExtraction = PackageToExtraction(package, includePending: false);
        var normalized = JsonSerializer.Serialize(finalExtraction, NormalizedJsonOptions);
        var hash = Sha256(normalized);
        var path = await WriteArtifactAsync(batch.Id, "final-review.json", normalized, cancellationToken);

        session.ReviewContentHash = hash;
        session.ReviewContentPath = path;
        session.ReadyAt = DateTime.UtcNow;
        session.ReadyBy = user;
        session.Estado = CurriculumReviewStatus.ReadyForApproval;
        session.FechaUltimaModificacion = DateTime.UtcNow;
        session.ReviewPackageJson = JsonSerializer.Serialize(package, JsonOptions);

        batch.Status = CurriculumImportStatus.ReadyForApproval;
        batch.ReviewContentHash = hash;
        batch.FinalReviewJson = normalized;
        batch.FinalReviewJsonPath = path;
        batch.ReadyAt = session.ReadyAt;
        batch.ReadyBy = user;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("CurriculumReviewReady BatchId={BatchId} Hash={Hash}", batch.Id, hash);
    }

    public async Task ApproveFromReviewAsync(
        Guid importBatchId, string? user, CancellationToken cancellationToken = default)
    {
        var (batch, session, package) = await LoadActiveAsync(importBatchId, cancellationToken);
        if (batch.Status != CurriculumImportStatus.ReadyForApproval
            || session.Estado != CurriculumReviewStatus.ReadyForApproval)
        {
            throw new CurriculumReviewException(
                $"El lote debe estar en ReadyForApproval; actual: {batch.Status}", 409);
        }

        var finalExtraction = PackageToExtraction(package, includePending: false);
        var normalized = JsonSerializer.Serialize(finalExtraction, NormalizedJsonOptions);
        var hash = Sha256(normalized);
        if (!string.Equals(hash, session.ReviewContentHash, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(hash, batch.ReviewContentHash, StringComparison.OrdinalIgnoreCase))
        {
            InvalidateReady(batch, session);
            await _db.SaveChangesAsync(cancellationToken);
            throw new CurriculumReviewException(
                "El contenido revisado no coincide con el hash aprobado. Debe validar y marcar listo nuevamente.", 409);
        }

        batch.Status = CurriculumImportStatus.Approved;
        batch.Estado = EstadoImportBatch.Aprobado;
        batch.UsuarioRevisor = user;
        batch.FechaAprobacion = DateTime.UtcNow;
        batch.FinalReviewJson = normalized;
        session.Estado = CurriculumReviewStatus.Approved;
        session.FechaCierre = DateTime.UtcNow;
        session.RevisadoPor = user ?? session.RevisadoPor;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("CurriculumReviewApproved BatchId={BatchId}", batch.Id);
    }

    public async Task RejectFromReviewAsync(
        Guid importBatchId, string reason, string? user, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new CurriculumReviewException("Debe indicar un motivo de rechazo.");

        var batch = await GetBatchAsync(importBatchId, cancellationToken);
        var session = await GetActiveSessionAsync(batch, cancellationToken, required: false);
        batch.Status = CurriculumImportStatus.Rejected;
        batch.Estado = EstadoImportBatch.Rechazado;
        batch.UsuarioRevisor = user;
        batch.FechaTermino = DateTime.UtcNow;
        batch.Mensaje = reason.Trim();
        if (session is not null)
        {
            session.Estado = CurriculumReviewStatus.Rejected;
            session.FechaCierre = DateTime.UtcNow;
            session.ObservacionGeneral = reason.Trim();
            session.RevisadoPor = user ?? session.RevisadoPor;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task PublishAsync(
        Guid importBatchId, string? user, CancellationToken cancellationToken = default)
    {
        var batch = await GetBatchAsync(importBatchId, cancellationToken);
        if (batch.Status != CurriculumImportStatus.Imported)
            throw new CurriculumReviewException($"El lote debe estar importado; actual: {batch.Status}", 409);

        var extraction = JsonSerializer.Deserialize<CurriculumExtractionResult>(
            batch.FinalReviewJson ?? batch.CorrectedExtractionJson ?? batch.OriginalExtractionJson ?? batch.ExtractionJson
            ?? throw new CurriculumReviewException("Lote sin extracción final."), JsonOptions)
            ?? throw new CurriculumReviewException("Extracción final inválida.");

        var oaCodes = extraction.LearningObjectives.Select(o => o.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var oas = await _db.ObjetivosAprendizaje
            .Where(o => oaCodes.Contains(o.Codigo) && o.EsContenidoOficial)
            .ToListAsync(cancellationToken);
        var unitNumbers = extraction.Units.Select(u => u.Number).ToHashSet();
        var unidades = await _db.Unidades
            .Where(u => u.EsContenidoOficial && unitNumbers.Contains(u.Numero))
            .Include(u => u.NivelAsignatura)!.ThenInclude(n => n!.Nivel)
            .Include(u => u.NivelAsignatura)!.ThenInclude(n => n!.Asignatura)
            .ToListAsync(cancellationToken);

        if (extraction.Level is not null && extraction.Subject is not null)
        {
            unidades = unidades.Where(u =>
                u.NivelAsignatura?.Nivel?.Codigo == extraction.Level.Code
                && u.NivelAsignatura?.Asignatura?.Codigo == extraction.Subject.Code).ToList();
            oas = await _db.ObjetivosAprendizaje
                .Include(o => o.NivelAsignatura)!.ThenInclude(n => n!.Nivel)
                .Include(o => o.NivelAsignatura)!.ThenInclude(n => n!.Asignatura)
                .Where(o => oaCodes.Contains(o.Codigo) && o.EsContenidoOficial
                            && o.NivelAsignatura!.Nivel!.Codigo == extraction.Level.Code
                            && o.NivelAsignatura.Asignatura!.Codigo == extraction.Subject.Code)
                .ToListAsync(cancellationToken);
        }

        var content = JsonSerializer.Serialize(extraction, NormalizedJsonOptions);
        var hash = Sha256(content);
        var release = new CurriculumRelease
        {
            Id = Guid.NewGuid(),
            Name = $"{extraction.Subject?.Name ?? "Asignatura"} {extraction.Level?.Name ?? ""}".Trim(),
            Version = DateTime.UtcNow.ToString("yyyy.MM.dd"),
            PublishedAt = DateTime.UtcNow,
            PublishedBy = user,
            SourceDocumentCount = batch.CurriculumDocumentId is null ? 0 : 1,
            ImportBatchCount = 1,
            Status = CurriculumPublicationStatus.Published,
            ContentHash = hash,
            CurriculumImportBatchId = batch.Id,
            Notes = $"Publicación del lote {batch.Id:N}"
        };
        _db.CurriculumReleases.Add(release);

        foreach (var oa in oas)
        {
            oa.PublicationStatus = CurriculumPublicationStatus.Published;
            oa.EsContenidoOficial = true;
            oa.CurriculumReleaseId = release.Id;
            oa.EstadoRevision = EstadoRevision.Aprobado;
        }

        foreach (var u in unidades)
        {
            u.PublicationStatus = CurriculumPublicationStatus.Published;
            u.EsContenidoOficial = true;
            u.CurriculumReleaseId = release.Id;
            u.EstadoRevision = EstadoRevision.Aprobado;
        }

        batch.PublishedAt = release.PublishedAt;
        batch.CurriculumReleaseId = release.Id;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("CurriculumPublished BatchId={BatchId} ReleaseId={ReleaseId}", batch.Id, release.Id);
    }

    public async Task<CurriculumReviewSummaryDto> GetSummaryAsync(
        Guid importBatchId, CancellationToken cancellationToken = default)
    {
        var (batch, session, package) = await LoadActiveAsync(importBatchId, cancellationToken);
        var issues = session.IssuesJson is null
            ? []
            : JsonSerializer.Deserialize<List<ValidationIssueDto>>(session.IssuesJson, JsonOptions) ?? [];
        var changes = await _db.CurriculumReviewChanges.AsNoTracking()
            .CountAsync(c => c.CurriculumImportBatchId == batch.Id && !c.IsReverted, cancellationToken);
        var unresolved = await _db.CurriculumReviewComments.AsNoTracking()
            .CountAsync(c => c.CurriculumReviewSessionId == session.Id && !c.IsResolved, cancellationToken);
        var blockingComments = await HasUnresolvedBlockingCommentsAsync(session.Id, cancellationToken);

        return new CurriculumReviewSummaryDto
        {
            ImportBatchId = batch.Id,
            Status = session.Estado.ToString(),
            ImportStatus = batch.Status.ToString(),
            Units = CountDecisions(package.Units.Where(u => !u.IsDeleted).Select(u => u.Decision)),
            Objectives = CountDecisions(package.Objectives.Where(o => !o.IsDeleted).Select(o => o.Decision)),
            Indicators = CountDecisions(package.Indicators.Where(i => !i.IsDeleted).Select(i => i.Decision)),
            Issues = CountIssues(package, issues),
            Changes = changes,
            UnresolvedComments = unresolved,
            LastValidationAt = session.LastValidationAt,
            LastDiffAt = session.LastDiffAt,
            CanMarkReady = !blockingComments && CanMarkReady(package, session, issues, out _),
            DocumentTitle = package.DocumentTitle,
            LevelName = package.LevelName,
            SubjectName = package.SubjectName,
            ExtractionConfidence = package.ExtractionConfidence,
            Skills = package.Skills.Count(s => !s.IsDeleted),
            Attitudes = package.Attitudes.Count(a => !a.IsDeleted)
        };
    }

    public async Task<IReadOnlyList<ReviewChangeDto>> GetChangesAsync(
        Guid importBatchId, CancellationToken cancellationToken = default)
    {
        var batch = await GetBatchAsync(importBatchId, cancellationToken);
        return await _db.CurriculumReviewChanges.AsNoTracking()
            .Where(c => c.CurriculumImportBatchId == batch.Id)
            .OrderByDescending(c => c.ChangedAt)
            .Select(c => new ReviewChangeDto
            {
                Id = c.Id,
                EntityType = c.EntityType,
                EntityTemporaryId = c.EntityTemporaryId,
                FieldName = c.FieldName,
                PreviousValue = c.PreviousValue,
                NewValue = c.NewValue,
                ChangeType = c.ChangeType.ToString(),
                ChangedAt = c.ChangedAt,
                ChangedBy = c.ChangedBy ?? c.UsuarioRevisor,
                Reason = c.Reason,
                IsReverted = c.IsReverted
            }).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewCommentDto>> GetCommentsAsync(
        Guid importBatchId, CancellationToken cancellationToken = default)
    {
        var (batch, session, _) = await LoadActiveAsync(importBatchId, cancellationToken);
        _ = batch;
        return await _db.CurriculumReviewComments.AsNoTracking()
            .Where(c => c.CurriculumReviewSessionId == session.Id)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ReviewCommentDto
            {
                Id = c.Id,
                EntityType = c.EntityType,
                EntityTemporaryId = c.EntityTemporaryId,
                Message = c.Message,
                Severity = c.Severity.ToString(),
                CreatedAt = c.CreatedAt,
                CreatedBy = c.CreatedBy,
                IsResolved = c.IsResolved
            }).ToListAsync(cancellationToken);
    }

    public async Task<ReviewCommentDto> AddCommentAsync(
        Guid importBatchId, AddReviewCommentRequest request, string? user,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new CurriculumReviewException("El comentario no puede estar vacío.");

        var (_, session, _) = await LoadActiveAsync(importBatchId, cancellationToken);
        var comment = new CurriculumReviewComment
        {
            Id = Guid.NewGuid(),
            CurriculumReviewSessionId = session.Id,
            EntityType = request.EntityType,
            EntityTemporaryId = request.EntityTemporaryId,
            Message = request.Message.Trim(),
            Severity = request.Severity,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user
        };
        _db.CurriculumReviewComments.Add(comment);
        session.FechaUltimaModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return new ReviewCommentDto
        {
            Id = comment.Id,
            EntityType = comment.EntityType,
            EntityTemporaryId = comment.EntityTemporaryId,
            Message = comment.Message,
            Severity = comment.Severity.ToString(),
            CreatedAt = comment.CreatedAt,
            CreatedBy = comment.CreatedBy,
            IsResolved = false
        };
    }

    public async Task ResolveCommentAsync(
        Guid importBatchId, Guid commentId, string? user, CancellationToken cancellationToken = default)
    {
        var (_, session, _) = await LoadActiveAsync(importBatchId, cancellationToken);
        var comment = await _db.CurriculumReviewComments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.CurriculumReviewSessionId == session.Id, cancellationToken)
            ?? throw new CurriculumReviewException("Comentario no encontrado.", 404);
        comment.IsResolved = true;
        comment.ResolvedAt = DateTime.UtcNow;
        comment.ResolvedBy = user;
        session.FechaUltimaModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<CurriculumReviewPackageDto> SplitObjectiveAsync(
        Guid importBatchId, string objectiveTemporaryId, SplitObjectiveRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(importBatchId, request.RowVersion, cancellationToken, (batch, session, package, user) =>
        {
            var original = FindObjective(package, objectiveTemporaryId);
            if (original.IsDeleted)
                throw new CurriculumReviewException("No se puede dividir un OA eliminado.");
            if (string.IsNullOrWhiteSpace(request.First.Code) || string.IsNullOrWhiteSpace(request.Second.Code))
                throw new CurriculumReviewException("Ambas partes de la división requieren código y descripción.");

            var firstId = $"oa-{package.NextOaSeq:D3}";
            package.NextOaSeq++;
            var secondId = $"oa-{package.NextOaSeq:D3}";
            package.NextOaSeq++;

            var first = CloneObjective(original, firstId, request.First.Code, request.First.Description, user);
            var second = CloneObjective(original, secondId, request.Second.Code, request.Second.Description, user);
            package.Objectives.Add(first);
            package.Objectives.Add(second);

            foreach (var ind in package.Indicators.Where(i => i.ObjectiveTemporaryId == original.TemporaryId && !i.IsDeleted))
            {
                if (!request.IndicatorAssignments.TryGetValue(ind.TemporaryId, out var target))
                    throw new CurriculumReviewException($"Falta asignación para el indicador {ind.TemporaryId}.");
                ind.ObjectiveTemporaryId = target.Equals("second", StringComparison.OrdinalIgnoreCase) ? secondId : firstId;
                ind.WasManuallyModified = true;
            }

            foreach (var unitId in original.UnitTemporaryIds)
            {
                var unit = package.Units.FirstOrDefault(u => u.TemporaryId == unitId);
                if (unit is null) continue;
                unit.LearningObjectiveTemporaryIds.Remove(original.TemporaryId);
                if (!unit.LearningObjectiveTemporaryIds.Contains(firstId)) unit.LearningObjectiveTemporaryIds.Add(firstId);
                if (!unit.LearningObjectiveTemporaryIds.Contains(secondId)) unit.LearningObjectiveTemporaryIds.Add(secondId);
            }

            original.IsDeleted = true;
            original.DeletedAt = DateTimeOffset.UtcNow;
            original.DeletedBy = user;
            original.DeletionReason = request.Reason ?? "Dividido en dos OA";
            original.IsMerged = true;
            original.MergedIntoTemporaryId = $"{firstId},{secondId}";
            original.Decision = CurriculumRecordDecision.Ignored;

            Track(batch, session, "LearningObjective", original.TemporaryId, "Split",
                original.Code, $"{firstId}+{secondId}", request.Reason, user,
                CurriculumReviewChangeType.RecordRemoved);
            Track(batch, session, "LearningObjective", firstId, "*", null, first.Code, request.Reason, user,
                CurriculumReviewChangeType.RecordAdded);
            Track(batch, session, "LearningObjective", secondId, "*", null, second.Code, request.Reason, user,
                CurriculumReviewChangeType.RecordAdded);
        });

    public Task<CurriculumReviewPackageDto> MergeAsync(
        Guid importBatchId, MergeReviewRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(importBatchId, request.RowVersion, cancellationToken, (batch, session, package, user) =>
        {
            if (!string.Equals(request.EntityType, "LearningObjective", StringComparison.OrdinalIgnoreCase))
                throw new CurriculumReviewException("Solo se soporta fusión de LearningObjective.");
            if (request.TemporaryIds.Count < 2)
                throw new CurriculumReviewException("Se requieren al menos dos OA para fusionar.");
            if (string.IsNullOrWhiteSpace(request.Result.Code) || string.IsNullOrWhiteSpace(request.Result.Description))
                throw new CurriculumReviewException("El resultado de la fusión requiere código y descripción.");

            var sources = request.TemporaryIds.Select(id => FindObjective(package, id)).ToList();
            if (sources.Any(s => s.IsDeleted))
                throw new CurriculumReviewException("No se pueden fusionar OA eliminados.");

            var newId = $"oa-{package.NextOaSeq:D3}";
            package.NextOaSeq++;
            var merged = new ReviewableLearningObjective
            {
                TemporaryId = newId,
                Code = request.Result.Code.Trim(),
                Description = request.Result.Description.Trim(),
                ExtractedCode = string.Join(" | ", sources.Select(s => s.ExtractedCode).Where(s => !string.IsNullOrWhiteSpace(s))),
                ExtractedDescription = string.Join("\n", sources.Select(s => s.ExtractedDescription).Where(s => !string.IsNullOrWhiteSpace(s))),
                UnitTemporaryIds = sources.SelectMany(s => s.UnitTemporaryIds).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                AxisTemporaryId = sources.Select(s => s.AxisTemporaryId).FirstOrDefault(a => !string.IsNullOrWhiteSpace(a)),
                ExtractionConfidence = sources.Average(s => s.ExtractionConfidence),
                Decision = CurriculumRecordDecision.Corrected,
                WasManuallyModified = true,
                LastModifiedAt = DateTimeOffset.UtcNow,
                LastModifiedBy = user,
                PageStart = sources.Min(s => s.PageStart),
                PageEnd = sources.Max(s => s.PageEnd),
                SourceFragment = string.Join("\n---\n", sources.Select(s => s.SourceFragment).Where(s => !string.IsNullOrWhiteSpace(s)))
            };
            package.Objectives.Add(merged);

            var seenDescriptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var order = 1;
            foreach (var source in sources)
            {
                foreach (var ind in package.Indicators.Where(i => i.ObjectiveTemporaryId == source.TemporaryId && !i.IsDeleted))
                {
                    var key = ind.Description.Trim();
                    if (!seenDescriptions.Add(key))
                    {
                        ind.IsDeleted = true;
                        ind.DeletedAt = DateTimeOffset.UtcNow;
                        ind.DeletedBy = user;
                        ind.DeletionReason = "Duplicado en fusión";
                        continue;
                    }

                    ind.ObjectiveTemporaryId = newId;
                    ind.Order = order++;
                    ind.WasManuallyModified = true;
                }

                foreach (var unitId in source.UnitTemporaryIds)
                {
                    var unit = package.Units.FirstOrDefault(u => u.TemporaryId == unitId);
                    if (unit is null) continue;
                    unit.LearningObjectiveTemporaryIds.Remove(source.TemporaryId);
                    if (!unit.LearningObjectiveTemporaryIds.Contains(newId))
                        unit.LearningObjectiveTemporaryIds.Add(newId);
                }

                source.IsDeleted = true;
                source.IsMerged = true;
                source.MergedIntoTemporaryId = newId;
                source.DeletedAt = DateTimeOffset.UtcNow;
                source.DeletedBy = user;
                source.DeletionReason = request.Reason ?? "Fusionado";
                source.Decision = CurriculumRecordDecision.Ignored;
            }

            Track(batch, session, "LearningObjective", newId, "*",
                string.Join(",", request.TemporaryIds), request.Result.Code, request.Reason, user,
                CurriculumReviewChangeType.RecordAdded);
        });

    public Task<CurriculumReviewPackageDto> BulkDecideAsync(
        Guid importBatchId, BulkDecisionRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(importBatchId, request.RowVersion, cancellationToken, (batch, session, package, user) =>
        {
            var type = NormalizeEntityType(request.EntityType);
            foreach (var id in request.TemporaryIds.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                switch (type)
                {
                    case "Unit":
                        var unit = FindUnit(package, id);
                        if (request.OnlyWithoutIssues && unit.Issues.Count > 0) continue;
                        if (unit.IsDeleted) continue;
                        Track(batch, session, type, id, "Decision", unit.Decision.ToString(), request.Decision.ToString(), request.Reason, user);
                        unit.Decision = request.Decision;
                        break;
                    case "LearningObjective":
                        var oa = FindObjective(package, id);
                        if (request.OnlyWithoutIssues && oa.Issues.Count > 0) continue;
                        if (oa.IsDeleted) continue;
                        Track(batch, session, type, id, "Decision", oa.Decision.ToString(), request.Decision.ToString(), request.Reason, user);
                        oa.Decision = request.Decision;
                        break;
                    case "EvaluationIndicator":
                        var ind = FindIndicator(package, id);
                        if (request.OnlyWithoutIssues && ind.Issues.Count > 0) continue;
                        if (ind.IsDeleted) continue;
                        Track(batch, session, type, id, "Decision", ind.Decision.ToString(), request.Decision.ToString(), request.Reason, user);
                        ind.Decision = request.Decision;
                        break;
                    default:
                        throw new CurriculumReviewException($"Tipo de entidad no soportado para decisión masiva: {request.EntityType}");
                }
            }
        });

    public async Task<RichCurriculumDiffResultDto> GetRichDiffAsync(
        Guid importBatchId, CancellationToken cancellationToken = default)
    {
        var (batch, session, package) = await LoadActiveAsync(importBatchId, cancellationToken);
        var extraction = PackageToExtraction(package, includePending: true);
        var current = await BuildCurrentPublishedAsync(extraction, cancellationToken);

        var items = new List<RichCurriculumDiffItemDto>();
        var oldOas = (current?.LearningObjectives ?? []).ToDictionary(o => o.Code, o => o, StringComparer.OrdinalIgnoreCase);
        var newOas = extraction.LearningObjectives.ToDictionary(o => o.Code, o => o, StringComparer.OrdinalIgnoreCase);
        var tempByCode = package.Objectives.Where(o => !o.IsDeleted && !string.IsNullOrWhiteSpace(o.Code))
            .ToDictionary(o => o.Code!, o => o.TemporaryId, StringComparer.OrdinalIgnoreCase);

        foreach (var (code, oa) in newOas)
        {
            tempByCode.TryGetValue(code, out var tempId);
            if (!oldOas.TryGetValue(code, out var prev))
            {
                items.Add(new RichCurriculumDiffItemDto
                {
                    EntityType = "LearningObjective",
                    Code = code,
                    TemporaryId = tempId ?? "",
                    ChangeType = "Added",
                    Fields =
                    [
                        FieldDiffHelper.CompareField("description", null, oa.Description),
                        FieldDiffHelper.CompareField("code", null, oa.Code)
                    ]
                });
            }
            else
            {
                var fields = new List<FieldDiffDto>();
                var codeDiff = FieldDiffHelper.CompareField("code", prev.Code, oa.Code);
                var descDiff = FieldDiffHelper.CompareField("description", prev.Description, oa.Description);
                if (codeDiff.Significance != nameof(TextChangeSignificance.None)) fields.Add(codeDiff);
                if (descDiff.Significance != nameof(TextChangeSignificance.None)) fields.Add(descDiff);
                items.Add(new RichCurriculumDiffItemDto
                {
                    EntityType = "LearningObjective",
                    Code = code,
                    TemporaryId = tempId ?? "",
                    ChangeType = fields.Count == 0 ? "Unchanged" : "Modified",
                    Fields = fields
                });
            }
        }

        foreach (var (code, prev) in oldOas)
        {
            if (newOas.ContainsKey(code)) continue;
            items.Add(new RichCurriculumDiffItemDto
            {
                EntityType = "LearningObjective",
                Code = code,
                TemporaryId = "",
                ChangeType = "PossiblyRemoved",
                Fields = [FieldDiffHelper.CompareField("description", prev.Description, null)]
            });
        }

        var result = new RichCurriculumDiffResultDto
        {
            ImportBatchId = batch.Id,
            GeneratedAt = DateTime.UtcNow,
            Items = items
        };
        session.DiffJson = JsonSerializer.Serialize(result, JsonOptions);
        session.LastDiffAt = result.GeneratedAt;
        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    // --- helpers ---

    private async Task<CurriculumReviewPackageDto> MutateAsync(
        Guid importBatchId,
        string? rowVersion,
        CancellationToken cancellationToken,
        Action<CurriculumImportBatch, CurriculumReviewSession, ReviewableCurriculumPackage, string?> mutate)
    {
        var (batch, session, package) = await LoadActiveAsync(importBatchId, cancellationToken);
        EnsureEditable(session);
        EnsureRowVersion(session, rowVersion);
        var user = session.RevisadoPor;
        mutate(batch, session, package, user);
        BumpConcurrency(session);
        InvalidateReady(batch, session);
        session.FechaUltimaModificacion = DateTime.UtcNow;
        session.ReviewPackageJson = JsonSerializer.Serialize(package, JsonOptions);
        await _db.SaveChangesAsync(cancellationToken);
        return ToPackageDto(batch, session, package);
    }

    private async Task<(CurriculumImportBatch Batch, CurriculumReviewSession Session, ReviewableCurriculumPackage Package)?> TryLoadActiveAsync(
        Guid importBatchId, CancellationToken cancellationToken)
    {
        var batch = await _db.CurriculumImportBatches.FirstOrDefaultAsync(b => b.Id == importBatchId, cancellationToken);
        if (batch is null) return null;
        if (batch.ActiveReviewSessionId is null) return null;
        var session = await _db.CurriculumReviewSessions.FirstOrDefaultAsync(s => s.Id == batch.ActiveReviewSessionId, cancellationToken);
        if (session?.ReviewPackageJson is null) return null;
        var package = JsonSerializer.Deserialize<ReviewableCurriculumPackage>(session.ReviewPackageJson, JsonOptions)
                      ?? throw new CurriculumReviewException("Paquete de revisión inválido.");
        return (batch, session, package);
    }

    private async Task<(CurriculumImportBatch Batch, CurriculumReviewSession Session, ReviewableCurriculumPackage Package)> LoadActiveAsync(
        Guid importBatchId, CancellationToken cancellationToken)
    {
        var loaded = await TryLoadActiveAsync(importBatchId, cancellationToken);
        if (loaded is null)
            throw new CurriculumReviewException("No hay una sesión de revisión activa. Inicie la revisión primero.", 404);
        return loaded.Value;
    }

    private async Task<CurriculumImportBatch> GetBatchAsync(Guid id, CancellationToken ct) =>
        await _db.CurriculumImportBatches.FirstOrDefaultAsync(b => b.Id == id, ct)
        ?? throw new CurriculumReviewException("Lote no encontrado.", 404);

    private async Task<CurriculumReviewSession?> GetActiveSessionAsync(
        CurriculumImportBatch batch, CancellationToken ct, bool required)
    {
        if (batch.ActiveReviewSessionId is null)
        {
            if (required) throw new CurriculumReviewException("No hay sesión de revisión activa.", 404);
            return null;
        }

        var session = await _db.CurriculumReviewSessions.FirstOrDefaultAsync(s => s.Id == batch.ActiveReviewSessionId, ct);
        if (session is null && required)
            throw new CurriculumReviewException("Sesión de revisión no encontrada.", 404);
        return session;
    }

    private static void EnsureEditable(CurriculumReviewSession session)
    {
        if (session.Estado is CurriculumReviewStatus.Approved
            or CurriculumReviewStatus.Closed
            or CurriculumReviewStatus.ReadyForApproval
            or CurriculumReviewStatus.Rejected)
            throw new CurriculumReviewException("La revisión está bloqueada y no admite cambios.", 409);
    }

    private static void EnsureRowVersion(CurriculumReviewSession session, string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
            throw new CurriculumReviewException("RowVersion es obligatorio para editar.", 409);
        string current;
        try { current = Convert.ToBase64String(session.RowVersion); }
        catch { throw new CurriculumReviewException("RowVersion de sesión inválido.", 409); }
        if (!string.Equals(current, rowVersion.Trim(), StringComparison.Ordinal))
            throw new CurriculumReviewException("Otro usuario modificó este registro. Recargue e intente nuevamente.", 409);
    }

    private static void BumpConcurrency(CurriculumReviewSession session) =>
        session.RowVersion = Guid.NewGuid().ToByteArray();

    private static void InvalidateReady(CurriculumImportBatch batch, CurriculumReviewSession session)
    {
        if (session.Estado is CurriculumReviewStatus.ReadyForApproval or CurriculumReviewStatus.Rejected)
            session.Estado = CurriculumReviewStatus.InProgress;
        session.ReviewContentHash = null;
        session.ReviewContentPath = null;
        session.ReadyAt = null;
        session.ReadyBy = null;
        if (batch.Status == CurriculumImportStatus.ReadyForApproval)
            batch.Status = CurriculumImportStatus.PendingReview;
        batch.ReviewContentHash = null;
        batch.ReadyAt = null;
        batch.ReadyBy = null;
    }

    private void Track(
        CurriculumImportBatch batch,
        CurriculumReviewSession session,
        string entityType,
        string temporaryId,
        string field,
        string? previous,
        string? next,
        string? reason,
        string? user,
        CurriculumReviewChangeType changeType = CurriculumReviewChangeType.ManualCorrection)
    {
        _db.CurriculumReviewChanges.Add(new CurriculumReviewChange
        {
            Id = Guid.NewGuid(),
            CurriculumImportBatchId = batch.Id,
            CurriculumReviewSessionId = session.Id,
            EntityType = entityType,
            EntityKey = temporaryId,
            EntityTemporaryId = temporaryId,
            Field = field,
            FieldName = field,
            OriginalValue = previous,
            PreviousValue = previous,
            NewValue = next,
            ChangeType = changeType,
            UsuarioRevisor = user,
            ChangedBy = user,
            Reason = reason,
            FechaCambio = DateTime.UtcNow,
            ChangedAt = DateTime.UtcNow
        });
    }

    private static void ApplyDecision<T>(
        T _,
        bool fieldsChanged,
        CurriculumRecordDecision? explicitDecision,
        Func<CurriculumRecordDecision> get,
        Action<CurriculumRecordDecision> set,
        Action markModified)
    {
        if (fieldsChanged)
        {
            markModified();
            if (explicitDecision is null && get() != CurriculumRecordDecision.Rejected && get() != CurriculumRecordDecision.Ignored)
                set(CurriculumRecordDecision.Corrected);
        }

        if (explicitDecision is CurriculumRecordDecision d)
            set(d);
    }

    private static ReviewableUnit FindUnit(ReviewableCurriculumPackage p, string id) =>
        p.Units.FirstOrDefault(u => u.TemporaryId == id)
        ?? throw new CurriculumReviewException($"Unidad no encontrada: {id}", 404);

    private static ReviewableLearningObjective FindObjective(ReviewableCurriculumPackage p, string id) =>
        p.Objectives.FirstOrDefault(o => o.TemporaryId == id)
        ?? throw new CurriculumReviewException($"OA no encontrado: {id}", 404);

    private static ReviewableEvaluationIndicator FindIndicator(ReviewableCurriculumPackage p, string id) =>
        p.Indicators.FirstOrDefault(i => i.TemporaryId == id)
        ?? throw new CurriculumReviewException($"Indicador no encontrado: {id}", 404);

    private static string NormalizeEntityType(string entityType) => entityType.Trim().ToLowerInvariant() switch
    {
        "unit" or "unidad" => "Unit",
        "learningobjective" or "objective" or "oa" or "objetivo" => "LearningObjective",
        "evaluationindicator" or "indicator" or "indicador" => "EvaluationIndicator",
        "skill" or "habilidad" => "Skill",
        "attitude" or "actitud" => "Attitude",
        _ => entityType
    };

    private static void SoftDelete(ReviewableCurriculumPackage package, string entityType, string temporaryId, string? reason, string? user)
    {
        var type = NormalizeEntityType(entityType);
        var now = DateTimeOffset.UtcNow;
        switch (type)
        {
            case "Unit":
                var u = FindUnit(package, temporaryId);
                u.IsDeleted = true; u.DeletedAt = now; u.DeletedBy = user; u.DeletionReason = reason;
                break;
            case "LearningObjective":
                var o = FindObjective(package, temporaryId);
                o.IsDeleted = true; o.DeletedAt = now; o.DeletedBy = user; o.DeletionReason = reason;
                break;
            case "EvaluationIndicator":
                var i = FindIndicator(package, temporaryId);
                i.IsDeleted = true; i.DeletedAt = now; i.DeletedBy = user; i.DeletionReason = reason;
                break;
            case "Skill":
                var s = package.Skills.FirstOrDefault(x => x.TemporaryId == temporaryId)
                    ?? throw new CurriculumReviewException($"Habilidad no encontrada: {temporaryId}", 404);
                s.IsDeleted = true;
                break;
            case "Attitude":
                var a = package.Attitudes.FirstOrDefault(x => x.TemporaryId == temporaryId)
                    ?? throw new CurriculumReviewException($"Actitud no encontrada: {temporaryId}", 404);
                a.IsDeleted = true;
                break;
            default:
                throw new CurriculumReviewException($"Tipo de entidad no soportado: {entityType}");
        }
    }

    private static void Restore(ReviewableCurriculumPackage package, string entityType, string temporaryId)
    {
        var type = NormalizeEntityType(entityType);
        switch (type)
        {
            case "Unit":
                var u = FindUnit(package, temporaryId);
                u.IsDeleted = false; u.DeletedAt = null; u.DeletedBy = null; u.DeletionReason = null;
                break;
            case "LearningObjective":
                var o = FindObjective(package, temporaryId);
                o.IsDeleted = false; o.IsMerged = false; o.MergedIntoTemporaryId = null;
                o.DeletedAt = null; o.DeletedBy = null; o.DeletionReason = null;
                if (o.Decision is CurriculumRecordDecision.Ignored) o.Decision = CurriculumRecordDecision.Pending;
                break;
            case "EvaluationIndicator":
                var i = FindIndicator(package, temporaryId);
                i.IsDeleted = false; i.DeletedAt = null; i.DeletedBy = null; i.DeletionReason = null;
                break;
            case "Skill":
                var s = package.Skills.FirstOrDefault(x => x.TemporaryId == temporaryId)
                    ?? throw new CurriculumReviewException($"Habilidad no encontrada: {temporaryId}", 404);
                s.IsDeleted = false;
                break;
            case "Attitude":
                var a = package.Attitudes.FirstOrDefault(x => x.TemporaryId == temporaryId)
                    ?? throw new CurriculumReviewException($"Actitud no encontrada: {temporaryId}", 404);
                a.IsDeleted = false;
                break;
            default:
                throw new CurriculumReviewException($"Tipo de entidad no soportado: {entityType}");
        }
    }

    private static void SyncUnitLinks(ReviewableCurriculumPackage package, ReviewableLearningObjective oa, List<string> unitIds)
    {
        foreach (var unit in package.Units)
            unit.LearningObjectiveTemporaryIds.Remove(oa.TemporaryId);
        foreach (var unitId in unitIds)
        {
            var unit = FindUnit(package, unitId);
            if (!unit.LearningObjectiveTemporaryIds.Contains(oa.TemporaryId))
                unit.LearningObjectiveTemporaryIds.Add(oa.TemporaryId);
        }
    }

    private static ReviewableLearningObjective CloneObjective(
        ReviewableLearningObjective original, string newId, string code, string description, string? user) =>
        new()
        {
            TemporaryId = newId,
            Code = code.Trim(),
            Description = description.Trim(),
            ExtractedCode = original.ExtractedCode,
            ExtractedDescription = original.ExtractedDescription,
            AxisTemporaryId = original.AxisTemporaryId,
            UnitTemporaryIds = original.UnitTemporaryIds.ToList(),
            ExtractionConfidence = original.ExtractionConfidence,
            Decision = CurriculumRecordDecision.Corrected,
            WasManuallyModified = true,
            LastModifiedAt = DateTimeOffset.UtcNow,
            LastModifiedBy = user,
            PageStart = original.PageStart,
            PageEnd = original.PageEnd,
            SourceFragment = original.SourceFragment
        };

    private static void ApplyRevert(ReviewableCurriculumPackage package, CurriculumReviewChange change)
    {
        var type = NormalizeEntityType(change.EntityType);
        var field = change.FieldName;
        switch (type)
        {
            case "Unit":
                var u = FindUnit(package, change.EntityTemporaryId);
                if (field is "IsDeleted") Restore(package, type, change.EntityTemporaryId);
                else if (field is "Name") u.Name = change.PreviousValue ?? u.Name;
                else if (field is "Description") u.Description = change.PreviousValue;
                else if (field is "Number" && int.TryParse(change.PreviousValue, out var n)) u.Number = n;
                else if (field is "SuggestedHours")
                    u.SuggestedHours = int.TryParse(change.PreviousValue, out var h) ? h : null;
                else if (field is "Order" && int.TryParse(change.PreviousValue, out var o)) u.Order = o;
                else if (field is "Decision" && Enum.TryParse<CurriculumRecordDecision>(change.PreviousValue, out var ud)) u.Decision = ud;
                break;
            case "LearningObjective":
                var oa = FindObjective(package, change.EntityTemporaryId);
                if (field is "IsDeleted" or "Split") Restore(package, type, change.EntityTemporaryId);
                else if (field is "Code") oa.Code = change.PreviousValue;
                else if (field is "Description") oa.Description = change.PreviousValue ?? oa.Description;
                else if (field is "AxisTemporaryId") oa.AxisTemporaryId = change.PreviousValue;
                else if (field is "UnitTemporaryIds")
                {
                    var ids = (change.PreviousValue ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                    SyncUnitLinks(package, oa, ids);
                    oa.UnitTemporaryIds = ids;
                }
                else if (field is "Decision" && Enum.TryParse<CurriculumRecordDecision>(change.PreviousValue, out var od)) oa.Decision = od;
                break;
            case "EvaluationIndicator":
                var ind = FindIndicator(package, change.EntityTemporaryId);
                if (field is "IsDeleted") Restore(package, type, change.EntityTemporaryId);
                else if (field is "Code") ind.Code = change.PreviousValue;
                else if (field is "Description") ind.Description = change.PreviousValue ?? ind.Description;
                else if (field is "ObjectiveTemporaryId") ind.ObjectiveTemporaryId = change.PreviousValue ?? ind.ObjectiveTemporaryId;
                else if (field is "Order" && int.TryParse(change.PreviousValue, out var io)) ind.Order = io;
                else if (field is "Decision" && Enum.TryParse<CurriculumRecordDecision>(change.PreviousValue, out var id)) ind.Decision = id;
                break;
            default:
                throw new CurriculumReviewException($"No se puede revertir el tipo {change.EntityType}.");
        }
    }

    private static bool IsImportable(CurriculumRecordDecision d) =>
        d is CurriculumRecordDecision.Accepted or CurriculumRecordDecision.Corrected;

    private static CurriculumExtractionResult PackageToExtraction(ReviewableCurriculumPackage package, bool includePending)
    {
        bool Include(CurriculumRecordDecision d) =>
            includePending
                ? d is CurriculumRecordDecision.Accepted or CurriculumRecordDecision.Corrected or CurriculumRecordDecision.Pending
                : IsImportable(d);

        var units = package.Units.Where(u => !u.IsDeleted && Include(u.Decision)).ToList();
        var objectives = package.Objectives.Where(o => !o.IsDeleted && Include(o.Decision)).ToList();
        var objectiveIds = objectives.Select(o => o.TemporaryId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var indicators = package.Indicators
            .Where(i => !i.IsDeleted && Include(i.Decision) && objectiveIds.Contains(i.ObjectiveTemporaryId))
            .ToList();

        var codeByTemp = objectives.ToDictionary(o => o.TemporaryId, o => o.Code ?? o.TemporaryId, StringComparer.OrdinalIgnoreCase);
        var axisNameByTemp = package.Axes.Where(a => !a.IsDeleted)
            .ToDictionary(a => a.TemporaryId, a => a.Name, StringComparer.OrdinalIgnoreCase);

        return new CurriculumExtractionResult
        {
            SourceTitle = package.DocumentTitle ?? "",
            SourceUrl = package.DocumentUrl ?? "",
            ConfianzaExtraccion = package.ExtractionConfidence,
            Level = string.IsNullOrWhiteSpace(package.LevelCode) ? null : new LevelExtractDto
            {
                Code = package.LevelCode!,
                Name = package.LevelName ?? package.LevelCode!
            },
            Subject = string.IsNullOrWhiteSpace(package.SubjectCode) ? null : new SubjectExtractDto
            {
                Code = package.SubjectCode!,
                Name = package.SubjectName ?? package.SubjectCode!
            },
            Axes = package.Axes.Where(a => !a.IsDeleted).Select(a => new AxisExtractDto
            {
                Code = a.Code, Name = a.Name, Description = a.Description
            }).ToList(),
            Units = units.Select(u => new UnitExtractDto
            {
                Number = u.Number,
                Name = u.Name,
                Description = u.Description,
                SuggestedHours = u.SuggestedHours,
                LearningObjectiveCodes = u.LearningObjectiveTemporaryIds
                    .Where(objectiveIds.Contains)
                    .Select(id => codeByTemp.TryGetValue(id, out var c) ? c : id)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToList()!
            }).ToList(),
            LearningObjectives = objectives.Select(o => new LearningObjectiveExtractDto
            {
                Code = o.Code ?? o.TemporaryId,
                Description = o.Description,
                AxisName = o.AxisTemporaryId is not null && axisNameByTemp.TryGetValue(o.AxisTemporaryId, out var n) ? n : null
            }).ToList(),
            EvaluationIndicators = indicators.Select(i => new EvaluationIndicatorExtractDto
            {
                LearningObjectiveCode = codeByTemp.TryGetValue(i.ObjectiveTemporaryId, out var c) ? c : i.ObjectiveTemporaryId,
                Code = i.Code,
                Description = i.Description,
                Orden = i.Order,
                EsSugerido = i.IsSuggested
            }).ToList(),
            Skills = package.Skills.Where(s => !s.IsDeleted && Include(s.Decision))
                .Select(s => new SkillExtractDto { Code = s.Code, Description = s.Description }).ToList(),
            Attitudes = package.Attitudes.Where(a => !a.IsDeleted && Include(a.Decision))
                .Select(a => new AttitudeExtractDto { Code = a.Code, Description = a.Description }).ToList()
        };
    }

    private static ReviewableCurriculumPackage BuildPackageFromExtraction(
        CurriculumExtractionResult extraction, CurriculumImportBatch batch)
    {
        var package = new ReviewableCurriculumPackage
        {
            SourceId = batch.SourceExternalId ?? batch.Id.ToString("N"),
            LevelCode = extraction.Level?.Code,
            LevelName = extraction.Level?.Name,
            SubjectCode = extraction.Subject?.Code,
            SubjectName = extraction.Subject?.Name,
            DocumentTitle = string.IsNullOrWhiteSpace(extraction.SourceTitle) ? null : extraction.SourceTitle,
            DocumentUrl = string.IsNullOrWhiteSpace(extraction.SourceUrl) ? null : extraction.SourceUrl,
            ExtractionConfidence = extraction.ConfianzaExtraccion
        };

        var axisSeq = 1;
        var axisByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var axis in extraction.Axes)
        {
            var id = $"axis-{axisSeq:D3}";
            axisSeq++;
            package.Axes.Add(new ReviewableAxis
            {
                TemporaryId = id,
                Code = axis.Code,
                Name = axis.Name,
                Description = axis.Description,
                Decision = CurriculumRecordDecision.Pending
            });
            if (!string.IsNullOrWhiteSpace(axis.Name))
                axisByName[axis.Name] = id;
        }

        var unitSeq = 1;
        var unitByNumber = new Dictionary<int, string>();
        foreach (var unit in extraction.Units.OrderBy(u => u.Number))
        {
            var id = $"unit-{unitSeq:D3}";
            unitSeq++;
            unitByNumber[unit.Number] = id;
            package.Units.Add(new ReviewableUnit
            {
                TemporaryId = id,
                Number = unit.Number,
                Name = unit.Name,
                Description = unit.Description,
                SuggestedHours = unit.SuggestedHours,
                Order = unit.Number,
                ExtractedName = unit.Name,
                ExtractedDescription = unit.Description,
                Decision = CurriculumRecordDecision.Pending
            });
        }

        var oaSeq = 1;
        var oaTempByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var oa in extraction.LearningObjectives)
        {
            var id = $"oa-{oaSeq:D3}";
            oaSeq++;
            if (!string.IsNullOrWhiteSpace(oa.Code))
                oaTempByCode[oa.Code] = id;
            string? axisId = null;
            if (!string.IsNullOrWhiteSpace(oa.AxisName) && axisByName.TryGetValue(oa.AxisName, out var aid))
                axisId = aid;

            var unitIds = new List<string>();
            foreach (var u in extraction.Units)
            {
                if (u.LearningObjectiveCodes.Any(c => string.Equals(c, oa.Code, StringComparison.OrdinalIgnoreCase))
                    && unitByNumber.TryGetValue(u.Number, out var uid))
                    unitIds.Add(uid);
            }

            package.Objectives.Add(new ReviewableLearningObjective
            {
                TemporaryId = id,
                Code = oa.Code,
                Description = oa.Description,
                ExtractedCode = oa.Code,
                ExtractedDescription = oa.Description,
                AxisTemporaryId = axisId,
                UnitTemporaryIds = unitIds,
                ExtractionConfidence = (decimal)extraction.ConfianzaExtraccion,
                Decision = CurriculumRecordDecision.Pending
            });

            foreach (var uid in unitIds)
            {
                var unit = package.Units.First(x => x.TemporaryId == uid);
                unit.LearningObjectiveTemporaryIds.Add(id);
            }
        }

        foreach (var group in extraction.EvaluationIndicators.GroupBy(i => i.LearningObjectiveCode, StringComparer.OrdinalIgnoreCase))
        {
            if (!oaTempByCode.TryGetValue(group.Key, out var oaTemp)) continue;
            var indSeq = 1;
            foreach (var ind in group.OrderBy(i => i.Orden))
            {
                var id = $"{oaTemp}-ind-{indSeq:D3}";
                indSeq++;
                package.Indicators.Add(new ReviewableEvaluationIndicator
                {
                    TemporaryId = id,
                    Code = ind.Code,
                    Description = ind.Description,
                    ExtractedDescription = ind.Description,
                    ObjectiveTemporaryId = oaTemp,
                    Order = ind.Orden > 0 ? ind.Orden : indSeq - 1,
                    IsSuggested = ind.EsSugerido,
                    Decision = CurriculumRecordDecision.Pending
                });
            }
        }

        var skillSeq = 1;
        foreach (var sk in extraction.Skills)
        {
            package.Skills.Add(new ReviewableSkill
            {
                TemporaryId = $"skill-{skillSeq:D3}",
                Code = sk.Code,
                Description = sk.Description,
                ExtractedDescription = sk.Description,
                Decision = CurriculumRecordDecision.Pending
            });
            skillSeq++;
        }

        var attitudeSeq = 1;
        foreach (var at in extraction.Attitudes)
        {
            package.Attitudes.Add(new ReviewableAttitude
            {
                TemporaryId = $"attitude-{attitudeSeq:D3}",
                Code = at.Code,
                Description = at.Description,
                ExtractedDescription = at.Description,
                Decision = CurriculumRecordDecision.Pending
            });
            attitudeSeq++;
        }

        package.NextUnitSeq = unitSeq;
        package.NextOaSeq = oaSeq;
        package.NextIndicatorSeq = package.Indicators.Count + 1;
        package.NextSkillSeq = skillSeq;
        package.NextAttitudeSeq = attitudeSeq;
        package.NextAxisSeq = axisSeq;
        return package;
    }

    private CurriculumExtractionResult ReadExtraction(CurriculumImportBatch batch)
    {
        var json = batch.CorrectedExtractionJson ?? batch.OriginalExtractionJson ?? batch.ExtractionJson
                   ?? throw new CurriculumReviewException("Lote sin extracción.");
        return JsonSerializer.Deserialize<CurriculumExtractionResult>(json, JsonOptions)
               ?? throw new CurriculumReviewException("Extracción inválida.");
    }

    private async Task<CurriculumExtractionResult?> BuildCurrentPublishedAsync(
        CurriculumExtractionResult extraction, CancellationToken ct)
    {
        if (extraction.Level is null || extraction.Subject is null) return null;
        var objectives = await _db.ObjetivosAprendizaje.AsNoTracking()
            .Include(x => x.NivelAsignatura)!.ThenInclude(x => x!.Nivel)
            .Include(x => x.NivelAsignatura)!.ThenInclude(x => x!.Asignatura)
            .Where(x => x.Vigente && x.EsContenidoOficial
                        && x.PublicationStatus == CurriculumPublicationStatus.Published
                        && x.NivelAsignatura!.Nivel!.Codigo == extraction.Level.Code
                        && x.NivelAsignatura.Asignatura!.Codigo == extraction.Subject.Code)
            .Select(x => new LearningObjectiveExtractDto { Code = x.Codigo, Description = x.Descripcion })
            .ToListAsync(ct);
        return new CurriculumExtractionResult
        {
            Level = extraction.Level,
            Subject = extraction.Subject,
            LearningObjectives = objectives
        };
    }

    private static bool CanMarkReady(
        ReviewableCurriculumPackage package,
        CurriculumReviewSession session,
        IReadOnlyList<ValidationIssueDto> issues,
        out string reason)
    {
        if (issues.Any(i => i.Blocking || string.Equals(i.Severity, "Error", StringComparison.OrdinalIgnoreCase)))
        {
            reason = "No se puede marcar listo mientras existan problemas bloqueantes o errores.";
            return false;
        }

        var oas = package.Objectives.Where(o => !o.IsDeleted).ToList();
        var inds = package.Indicators.Where(i => !i.IsDeleted).ToList();
        var units = package.Units.Where(u => !u.IsDeleted).ToList();

        if (oas.Any(o => o.Decision == CurriculumRecordDecision.Pending)
            || inds.Any(i => i.Decision == CurriculumRecordDecision.Pending))
        {
            reason = "No se puede aprobar mientras existan registros pendientes.";
            return false;
        }

        if (!oas.Any(o => IsImportable(o.Decision)) || !units.Any(u => IsImportable(u.Decision)))
        {
            reason = "Debe existir al menos una unidad y un OA aceptados o corregidos.";
            return false;
        }

        var oaIds = oas.Where(o => IsImportable(o.Decision)).Select(o => o.TemporaryId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (inds.Any(i => IsImportable(i.Decision) && !oaIds.Contains(i.ObjectiveTemporaryId)))
        {
            reason = "El indicador debe estar asociado a un objetivo.";
            return false;
        }

        var codes = oas.Where(o => IsImportable(o.Decision)).Select(o => o.Code?.Trim()).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        if (codes.Count != codes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            reason = "Existen códigos OA duplicados en este nivel y asignatura.";
            return false;
        }

        if (units.Any(u => IsImportable(u.Decision) && (u.Number <= 0 || string.IsNullOrWhiteSpace(u.Name))))
        {
            reason = "Cada unidad debe tener número y nombre.";
            return false;
        }

        if (session.LastValidationAt is null || session.LastValidationAt < session.FechaUltimaModificacion)
        {
            reason = "El lote cambió desde la última validación.";
            return false;
        }

        if (session.LastDiffAt is null || session.LastDiffAt < session.FechaUltimaModificacion)
        {
            reason = "Debe regenerar el diff después del último cambio.";
            return false;
        }

        reason = "";
        return true;
    }

    private async Task<bool> HasUnresolvedBlockingCommentsAsync(Guid sessionId, CancellationToken ct) =>
        await _db.CurriculumReviewComments.AsNoTracking()
            .AnyAsync(c => c.CurriculumReviewSessionId == sessionId && !c.IsResolved
                           && c.Severity == CurriculumCommentSeverity.Blocking, ct);

    private static EntityDecisionCountsDto CountDecisions(IEnumerable<CurriculumRecordDecision> decisions)
    {
        var list = decisions.ToList();
        return new EntityDecisionCountsDto
        {
            Total = list.Count,
            Accepted = list.Count(d => d == CurriculumRecordDecision.Accepted),
            Corrected = list.Count(d => d == CurriculumRecordDecision.Corrected),
            Pending = list.Count(d => d == CurriculumRecordDecision.Pending),
            Rejected = list.Count(d => d == CurriculumRecordDecision.Rejected),
            Ignored = list.Count(d => d == CurriculumRecordDecision.Ignored)
        };
    }

    private static IssueCountsDto CountIssues(ReviewableCurriculumPackage package, IReadOnlyList<ValidationIssueDto> issues)
    {
        var fieldIssues = package.Units.SelectMany(u => u.Issues)
            .Concat(package.Objectives.SelectMany(o => o.Issues))
            .Concat(package.Indicators.SelectMany(i => i.Issues))
            .ToList();
        return new IssueCountsDto
        {
            Blocking = issues.Count(i => i.Blocking) + fieldIssues.Count(i => i.Severity is "Blocking" or "Error"),
            Errors = issues.Count(i => string.Equals(i.Severity, "Error", StringComparison.OrdinalIgnoreCase))
                     + fieldIssues.Count(i => i.Severity == "Error"),
            Warnings = issues.Count(i => string.Equals(i.Severity, "Warning", StringComparison.OrdinalIgnoreCase))
                       + fieldIssues.Count(i => i.Severity == "Warning"),
            Info = issues.Count(i => string.Equals(i.Severity, "Info", StringComparison.OrdinalIgnoreCase))
                   + fieldIssues.Count(i => i.Severity == "Info")
        };
    }

    private static List<ValidationIssueDto> ToIssueDtos(CurriculumValidationResult validation) =>
        validation.Errors.Select(x => new ValidationIssueDto { Severity = "Error", Blocking = true, Message = x })
            .Concat(validation.Warnings.Select(x => new ValidationIssueDto { Severity = "Warning", Message = x }))
            .ToList();

    private static void AttachIssues(ReviewableCurriculumPackage package, IReadOnlyList<ValidationIssueDto> issues)
    {
        foreach (var u in package.Units) u.Issues.Clear();
        foreach (var o in package.Objectives) o.Issues.Clear();
        foreach (var i in package.Indicators) i.Issues.Clear();

        foreach (var issue in issues)
        {
            var msg = issue.Message;
            var attached = false;
            foreach (var oa in package.Objectives.Where(o => !o.IsDeleted && !string.IsNullOrWhiteSpace(o.Code)))
            {
                if (!msg.Contains(oa.Code!, StringComparison.OrdinalIgnoreCase)) continue;
                oa.Issues.Add(new ReviewFieldIssue
                {
                    Code = issue.Blocking ? "BLOCK" : issue.Severity.ToUpperInvariant(),
                    Severity = issue.Blocking ? "Error" : issue.Severity,
                    Message = msg,
                    FieldName = msg.Contains("código", StringComparison.OrdinalIgnoreCase) ? "Code" : "Description"
                });
                attached = true;
            }

            if (attached) continue;
            if (msg.Contains("Unidad", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var unit in package.Units.Where(u => !u.IsDeleted && msg.Contains(u.Number.ToString(), StringComparison.Ordinal)))
                {
                    unit.Issues.Add(new ReviewFieldIssue
                    {
                        Code = "UNIT",
                        Severity = issue.Blocking ? "Error" : issue.Severity,
                        Message = msg
                    });
                }
            }
        }
    }

    private static CurriculumReviewSessionDto ToSessionDto(CurriculumReviewSession session, Guid batchId) => new()
    {
        Id = session.Id,
        ImportBatchId = batchId,
        Status = session.Estado.ToString(),
        Reviewer = session.RevisadoPor,
        VersionRevision = session.VersionRevision,
        RowVersion = Convert.ToBase64String(session.RowVersion),
        StartedAt = session.FechaInicio,
        LastModifiedAt = session.FechaUltimaModificacion,
        ReviewContentHash = session.ReviewContentHash,
        ReadyAt = session.ReadyAt,
        LastValidationAt = session.LastValidationAt,
        LastDiffAt = session.LastDiffAt
    };

    private static CurriculumReviewPackageDto ToPackageDto(
        CurriculumImportBatch batch, CurriculumReviewSession session, ReviewableCurriculumPackage package) =>
        new()
        {
            ImportBatchId = batch.Id,
            ReviewSessionId = session.Id,
            ReviewStatus = session.Estado.ToString(),
            ImportStatus = batch.Status.ToString(),
            RowVersion = Convert.ToBase64String(session.RowVersion),
            DocumentTitle = package.DocumentTitle,
            LevelName = package.LevelName,
            SubjectName = package.SubjectName,
            ExtractionConfidence = package.ExtractionConfidence,
            Units = package.Units.Select(u => new ReviewUnitDto
            {
                TemporaryId = u.TemporaryId,
                Number = u.Number,
                Name = u.Name,
                Description = u.Description,
                SuggestedHours = u.SuggestedHours,
                Decision = u.Decision.ToString(),
                WasManuallyModified = u.WasManuallyModified,
                IsDeleted = u.IsDeleted,
                ObjectiveCount = u.LearningObjectiveTemporaryIds.Count,
                IssueCount = u.Issues.Count,
                PageStart = u.PageStart
            }).ToList(),
            Objectives = package.Objectives.Select(o => new ReviewObjectiveDto
            {
                TemporaryId = o.TemporaryId,
                Code = o.Code,
                Description = o.Description,
                ExtractedCode = o.ExtractedCode,
                ExtractedDescription = o.ExtractedDescription,
                UnitTemporaryIds = o.UnitTemporaryIds,
                AxisTemporaryId = o.AxisTemporaryId,
                Decision = o.Decision.ToString(),
                WasManuallyModified = o.WasManuallyModified,
                IsDeleted = o.IsDeleted,
                ExtractionConfidence = o.ExtractionConfidence,
                IndicatorCount = package.Indicators.Count(i => i.ObjectiveTemporaryId == o.TemporaryId && !i.IsDeleted),
                IssueCount = o.Issues.Count,
                PageStart = o.PageStart,
                PageEnd = o.PageEnd,
                SourceFragment = o.SourceFragment,
                Issues = o.Issues.Select(i => new ReviewFieldIssueDto
                {
                    Code = i.Code, Severity = i.Severity, Message = i.Message, FieldName = i.FieldName
                }).ToList()
            }).ToList(),
            Indicators = package.Indicators.Select(i => new ReviewIndicatorDto
            {
                TemporaryId = i.TemporaryId,
                Code = i.Code,
                Description = i.Description,
                ExtractedDescription = i.ExtractedDescription,
                ObjectiveTemporaryId = i.ObjectiveTemporaryId,
                Decision = i.Decision.ToString(),
                WasManuallyModified = i.WasManuallyModified,
                IsDeleted = i.IsDeleted,
                Order = i.Order,
                Issues = i.Issues.Select(x => new ReviewFieldIssueDto
                {
                    Code = x.Code, Severity = x.Severity, Message = x.Message, FieldName = x.FieldName
                }).ToList()
            }).ToList(),
            Skills = package.Skills.Select(s => new ReviewSkillDto
            {
                TemporaryId = s.TemporaryId,
                Description = s.Description,
                Decision = s.Decision.ToString(),
                WasManuallyModified = s.WasManuallyModified,
                IsDeleted = s.IsDeleted
            }).ToList(),
            Attitudes = package.Attitudes.Select(a => new ReviewAttitudeDto
            {
                TemporaryId = a.TemporaryId,
                Description = a.Description,
                Decision = a.Decision.ToString(),
                WasManuallyModified = a.WasManuallyModified,
                IsDeleted = a.IsDeleted
            }).ToList()
        };

    private async Task<string> WriteArtifactAsync(Guid id, string name, string content, CancellationToken ct)
    {
        var root = _configuration["Curriculum:StorageRoot"] ?? "App_Data/Curriculum";
        var dir = Path.Combine(Path.IsPathRooted(root) ? root : Path.Combine(_environment.ContentRootPath, root), "Imports", id.ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);
        await File.WriteAllTextAsync(path, content, ct);
        return path;
    }

    private static string Sha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
