using ProfeAsistente.Api.Data;
using ProfeAsistente.Api.Models.Classroom;
using ProfeAsistente.Api.Models.Planning;
using ProfeAsistente.Api.Services.Authorization;
using ProfeAsistente.Api.Services.DateTimeServices;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;
using ProfeAsistente.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace ProfeAsistente.Api.Services.Classroom;

public interface IAssessmentService
{
    Task<LearningAssessmentDto> CreateAssessmentAsync(CreateLearningAssessmentRequest request, CancellationToken cancellationToken = default);
    Task SaveScoresAsync(Guid assessmentId, IReadOnlyList<SaveAssessmentScoreRequest> scores, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssessmentScoreDto>> GetScoresAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LearningAssessmentDto>> ListAssessmentsAsync(Guid? courseId, Guid? classId = null, CancellationToken cancellationToken = default);
    Task<AssessmentEvidenceSummaryDto> GetAssessmentEvidenceAsync(Guid assessmentId, CancellationToken cancellationToken = default);
}

public sealed class AssessmentService : IAssessmentService
{
    private readonly ProfeAsistenteDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly ClassroomAccess _access;
    private readonly ICourseRosterService _roster;
    private readonly IApplicationClock _clock;

    public AssessmentService(
        ProfeAsistenteDbContext db,
        ICurrentUserService current,
        ClassroomAccess access,
        ICourseRosterService roster,
        IApplicationClock clock)
    {
        _db = db;
        _current = current;
        _access = access;
        _roster = roster;
        _clock = clock;
    }

    public async Task<LearningAssessmentDto> CreateAssessmentAsync(CreateLearningAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomEvaluate);
        Guid institutionId = request.InstitutionId;
        Guid? courseId = request.SchoolCourseId;
        Guid? planningId = request.PlanningId;
        Guid? classId = request.ClassId;
        Guid? oaId = request.ObjectiveLearningId;

        if (classId is Guid cid)
        {
            var meta = await _db.Clases.AsNoTracking()
                .Where(c => c.Id == cid)
                .Join(_db.Planificaciones, c => c.PlanificacionId, p => p.Id, (c, p) => new { c, p })
                .FirstAsync(cancellationToken);
            planningId ??= meta.p.Id;
            courseId ??= meta.p.SchoolCourseId;
            oaId ??= meta.c.ObjetivoAprendizajeId;
            if (institutionId == Guid.Empty)
                institutionId = meta.p.InstitutionId ?? _current.ActiveInstitutionId ?? Guid.Empty;
        }

        if (institutionId == Guid.Empty)
            throw new InvalidOperationException("Indique el establecimiento o cree la evaluación desde una clase.");
        _access.EnsureInstitution(institutionId);

        var documentId = request.EducationalDocumentId;
        if (documentId is null && classId is Guid classForDoc)
        {
            documentId = await _db.EducationalDocuments.AsNoTracking()
                .Where(d => d.ClassId == classForDoc && !d.IsDeleted
                            && d.DocumentType == EducationalDocumentType.Assessment)
                .OrderByDescending(d => d.UpdatedAt)
                .Select(d => (Guid?)d.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var entity = new LearningAssessment
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            SchoolCourseId = courseId,
            ClassId = classId,
            PlanningId = planningId,
            ObjectiveLearningId = oaId,
            EducationalDocumentId = documentId,
            Purpose = request.Purpose,
            Name = request.Name.Trim(),
            Date = request.Date,
            Criteria = request.Criteria,
            CreatedByUserId = _current.UserId ?? Guid.Empty
        };
        _db.LearningAssessments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAssessmentAsync(entity, cancellationToken);
    }

    public async Task SaveScoresAsync(Guid assessmentId, IReadOnlyList<SaveAssessmentScoreRequest> scores, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomEvaluate);
        var assessment = await _db.LearningAssessments
            .FirstAsync(a => a.Id == assessmentId, cancellationToken);
        _access.EnsureInstitution(assessment.InstitutionId);

        var existing = await _db.AssessmentScores.Where(s => s.LearningAssessmentId == assessmentId).ToListAsync(cancellationToken);
        _db.AssessmentScores.RemoveRange(existing);
        foreach (var s in scores)
        {
            _db.AssessmentScores.Add(new AssessmentScore
            {
                Id = Guid.NewGuid(),
                LearningAssessmentId = assessmentId,
                StudentId = s.StudentId,
                Score = s.Score,
                AchievementLevel = s.AchievementLevel,
                Feedback = s.Feedback
            });
        }

        await RecordAssessmentEvidenceAsync(assessment, scores, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AssessmentScoreDto>> GetScoresAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomView, AppPermissions.ClassroomEvaluate);
        var assessment = await _db.LearningAssessments.AsNoTracking()
            .FirstAsync(a => a.Id == assessmentId, cancellationToken);
        _access.EnsureInstitution(assessment.InstitutionId);

        var saved = await _db.AssessmentScores.AsNoTracking()
            .Where(s => s.LearningAssessmentId == assessmentId)
            .ToListAsync(cancellationToken);
        var byStudent = saved.ToDictionary(s => s.StudentId);

        IReadOnlyList<RosterStudentDto> students = [];
        if (assessment.ClassId is Guid classId)
            students = (await _roster.GetRosterForClassAsync(classId, cancellationToken)).Students;
        else if (assessment.SchoolCourseId is Guid courseId)
            students = (await _roster.GetRosterAsync(courseId, cancellationToken)).Students;

        if (students.Count > 0)
        {
            return students.Select(s =>
            {
                byStudent.TryGetValue(s.StudentId, out var row);
                return new AssessmentScoreDto
                {
                    StudentId = s.StudentId,
                    StudentName = s.DisplayName,
                    Score = row?.Score,
                    AchievementLevel = row?.AchievementLevel,
                    Feedback = row?.Feedback
                };
            }).ToList();
        }

        var names = await _db.Students.AsNoTracking()
            .Where(s => saved.Select(x => x.StudentId).Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.DisplayName, cancellationToken);
        return saved.Select(s => new AssessmentScoreDto
        {
            StudentId = s.StudentId,
            StudentName = names.GetValueOrDefault(s.StudentId, "Estudiante"),
            Score = s.Score,
            AchievementLevel = s.AchievementLevel,
            Feedback = s.Feedback
        }).ToList();
    }

    public async Task<IReadOnlyList<LearningAssessmentDto>> ListAssessmentsAsync(Guid? courseId, Guid? classId = null, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomView);
        var q = _db.LearningAssessments.AsNoTracking().AsQueryable();
        if (classId is Guid clid)
            q = q.Where(a => a.ClassId == clid);
        else if (courseId is Guid cid)
            q = q.Where(a => a.SchoolCourseId == cid);
        else if (_current.ActiveInstitutionId is Guid iid)
            q = q.Where(a => a.InstitutionId == iid);
        var list = await q.OrderByDescending(a => a.Date).Take(100).ToListAsync(cancellationToken);
        var result = new List<LearningAssessmentDto>();
        foreach (var a in list)
            result.Add(await MapAssessmentAsync(a, cancellationToken));
        return result;
    }

    public async Task<AssessmentEvidenceSummaryDto> GetAssessmentEvidenceAsync(
        Guid assessmentId, CancellationToken cancellationToken = default)
    {
        _access.Ensure(AppPermissions.ClassroomView, AppPermissions.ClassroomEvaluate);
        var assessment = await _db.LearningAssessments.AsNoTracking()
            .FirstAsync(a => a.Id == assessmentId, cancellationToken);
        _access.EnsureInstitution(assessment.InstitutionId);
        var scores = await GetScoresAsync(assessmentId, cancellationToken);
        return await BuildEvidenceSummaryAsync(assessment, scores, cancellationToken);
    }

    private async Task RecordAssessmentEvidenceAsync(
        LearningAssessment assessment,
        IReadOnlyList<SaveAssessmentScoreRequest> scores,
        CancellationToken cancellationToken)
    {
        if (assessment.ClassId is not Guid classId) return;

        var tag = $"assessment:{assessment.Id}";
        var previous = await _db.ClassLearningEvidences
            .Where(e => e.ClassId == classId && e.Notes == tag)
            .ToListAsync(cancellationToken);
        _db.ClassLearningEvidences.RemoveRange(previous);

        var scoreDtos = scores.Select(s => new AssessmentScoreDto
        {
            StudentId = s.StudentId,
            StudentName = string.Empty,
            Score = s.Score,
            AchievementLevel = s.AchievementLevel,
            Feedback = s.Feedback
        }).ToList();
        var summary = await BuildEvidenceSummaryAsync(assessment, scoreDtos, cancellationToken);

        var evidenceType = assessment.Purpose switch
        {
            EvaluationPurpose.Summative => LearningEvidenceType.SummativeAssessment,
            EvaluationPurpose.Diagnostic => LearningEvidenceType.FormativeAssessment,
            _ => LearningEvidenceType.FormativeAssessment
        };

        Guid? indicatorId = null;
        if (assessment.ClassId is Guid cid)
        {
            indicatorId = await _db.ClaseIndicadores.AsNoTracking()
                .Where(i => i.ClaseId == cid)
                .Select(i => (Guid?)i.IndicadorEvaluacionId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        _db.ClassLearningEvidences.Add(new ClassLearningEvidence
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            EvaluationIndicatorId = indicatorId,
            EvidenceType = evidenceType,
            Description = summary.ReadingSummary,
            Source = "Assessment",
            Notes = tag,
            RecordedAt = _clock.UtcNow
        });
    }

    private async Task<AssessmentEvidenceSummaryDto> BuildEvidenceSummaryAsync(
        LearningAssessment assessment,
        IReadOnlyList<AssessmentScoreDto> scores,
        CancellationToken cancellationToken)
    {
        var withLevel = scores.Where(s => !string.IsNullOrWhiteSpace(s.AchievementLevel)).ToList();
        static bool IsPorLograr(string? l) =>
            (l ?? "").Contains("por lograr", StringComparison.OrdinalIgnoreCase)
            || (l ?? "").Equals("PL", StringComparison.OrdinalIgnoreCase);
        static bool IsLogrado(string? l) =>
            (l ?? "").Equals("logrado", StringComparison.OrdinalIgnoreCase)
            || ((l ?? "").Contains("logrado", StringComparison.OrdinalIgnoreCase)
                && !(l ?? "").Contains("medianamente", StringComparison.OrdinalIgnoreCase)
                && !(l ?? "").Contains("por lograr", StringComparison.OrdinalIgnoreCase));
        static bool IsMedianamente(string? l) =>
            (l ?? "").Contains("medianamente", StringComparison.OrdinalIgnoreCase);

        var porLograr = withLevel.Count(s => IsPorLograr(s.AchievementLevel));
        var logrado = withLevel.Count(s => IsLogrado(s.AchievementLevel));
        var medianamente = withLevel.Count(s => IsMedianamente(s.AchievementLevel));
        // If labels don't match buckets, count remainder as medianamente for display stability.
        var classified = porLograr + logrado + medianamente;
        if (classified < withLevel.Count)
            medianamente += withLevel.Count - classified;

        var numeric = scores.Where(s => s.Score is not null).Select(s => s.Score!.Value).ToList();
        var avg = numeric.Count == 0 ? (decimal?)null : Math.Round(numeric.Average(), 1);

        string oaCode = string.Empty;
        string oaDesc = string.Empty;
        Guid? oaId = assessment.ObjectiveLearningId;
        if (oaId is Guid oid)
        {
            var oa = await _db.ObjetivosAprendizaje.AsNoTracking().FirstOrDefaultAsync(o => o.Id == oid, cancellationToken);
            if (oa is not null)
            {
                oaCode = oa.Codigo;
                oaDesc = oa.Descripcion;
            }
        }

        var indicators = new List<string>();
        if (assessment.ClassId is Guid classId)
        {
            var indIds = await _db.ClaseIndicadores.AsNoTracking()
                .Where(i => i.ClaseId == classId)
                .Select(i => i.IndicadorEvaluacionId)
                .ToListAsync(cancellationToken);
            indicators = await _db.IndicadoresEvaluacion.AsNoTracking()
                .Where(i => indIds.Contains(i.Id))
                .Select(i => i.Descripcion)
                .ToListAsync(cancellationToken);
        }

        IReadOnlyList<AssessmentSpecificationRowDto> specs = [];
        if (assessment.EducationalDocumentId is Guid docId)
        {
            var rawSpecs = await _db.AssessmentSpecifications.AsNoTracking()
                .Where(s => s.EducationalDocumentId == docId)
                .ToListAsync(cancellationToken);
            var indIds = rawSpecs.Select(s => s.EvaluationIndicatorId).Distinct().ToList();
            var indNames = await _db.IndicadoresEvaluacion.AsNoTracking()
                .Where(i => indIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Descripcion, cancellationToken);
            specs = rawSpecs.Select(s => new AssessmentSpecificationRowDto
            {
                Id = s.Id,
                EvaluationIndicatorId = s.EvaluationIndicatorId,
                IndicatorDescription = indNames.GetValueOrDefault(s.EvaluationIndicatorId, "Indicador"),
                BloomLevel = s.BloomLevel,
                ItemCount = s.ItemCount,
                TotalPoints = s.TotalPoints,
                WeightPercentage = s.WeightPercentage
            }).ToList();
        }

        var weakIds = scores.Where(s => IsPorLograr(s.AchievementLevel)).Select(s => s.StudentId).ToHashSet();
        var needsSupport = scores
            .Where(s => weakIds.Contains(s.StudentId) && !string.IsNullOrWhiteSpace(s.StudentName))
            .Select(s => s.StudentName)
            .Distinct()
            .Take(12)
            .ToList();
        if (needsSupport.Count == 0 && weakIds.Count > 0 && assessment.ClassId is Guid cid2)
        {
            var roster = await _roster.GetRosterForClassAsync(cid2, cancellationToken);
            needsSupport = roster.Students
                .Where(s => weakIds.Contains(s.StudentId))
                .Select(s => s.DisplayName)
                .Take(12)
                .ToList();
        }

        var needsReinforcement = withLevel.Count > 0
            && (porLograr * 2 >= withLevel.Count || (porLograr + medianamente) * 3 >= withLevel.Count * 2);

        var purposeLabel = assessment.Purpose switch
        {
            EvaluationPurpose.Diagnostic => "Diagnóstica",
            EvaluationPurpose.Summative => "Sumativa",
            _ => "Formativa"
        };

        var reading = withLevel.Count == 0
            ? $"OA {oaCode}: aún no hay niveles de logro registrados."
            : $"OA {oaCode}: {logrado} logrado(s), {medianamente} medianamente logrado(s), {porLograr} por lograr"
              + (avg is not null ? $". Promedio {avg}." : ".")
              + (needsReinforcement
                  ? " Se recomienda crear un refuerzo alineado a este OA."
                  : " El grupo avanza; mantenga seguimiento formativo.");

        return new AssessmentEvidenceSummaryDto
        {
            AssessmentId = assessment.Id,
            AssessmentName = assessment.Name,
            ClassId = assessment.ClassId,
            ObjectiveId = oaId,
            ObjectiveCode = oaCode,
            ObjectiveDescription = oaDesc,
            PurposeLabel = purposeLabel,
            StudentsTotal = scores.Count,
            StudentsWithLevel = withLevel.Count,
            CountPorLograr = porLograr,
            CountMedianamente = medianamente,
            CountLogrado = logrado,
            AverageScore = avg,
            NeedsReinforcement = needsReinforcement,
            ReadingSummary = reading,
            Indicators = indicators,
            SpecificationTable = specs,
            EducationalDocumentId = assessment.EducationalDocumentId,
            StudentsNeedingSupport = needsSupport
        };
    }

    private async Task<LearningAssessmentDto> MapAssessmentAsync(LearningAssessment a, CancellationToken ct)
    {
        string? code = null;
        string? desc = null;
        if (a.ObjectiveLearningId is Guid oid)
        {
            var oa = await _db.ObjetivosAprendizaje.AsNoTracking().FirstOrDefaultAsync(o => o.Id == oid, ct);
            code = oa?.Codigo;
            desc = oa?.Descripcion;
        }

        return new LearningAssessmentDto
        {
            Id = a.Id,
            Name = a.Name,
            Purpose = a.Purpose,
            Date = a.Date,
            ClassId = a.ClassId,
            SchoolCourseId = a.SchoolCourseId,
            ObjectiveLearningId = a.ObjectiveLearningId,
            EducationalDocumentId = a.EducationalDocumentId,
            ObjectiveCode = code,
            ObjectiveDescription = desc,
            Criteria = a.Criteria
        };
    }
}
