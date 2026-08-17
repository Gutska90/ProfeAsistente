using AppEducativa.Api.Models;
using AppEducativa.Api.Models.AI;
using AppEducativa.Api.Models.Classroom;
using AppEducativa.Api.Models.Curriculum;
using AppEducativa.Api.Models.Export;
using AppEducativa.Api.Models.Identity;
using AppEducativa.Api.Models.Institutions;
using AppEducativa.Api.Models.Planning;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppEducativa.Api.Data;

public class AppEducativaDbContext : IdentityDbContext<ApplicationUser, ApplicationRoleEntity, Guid>
{
    public AppEducativaDbContext(DbContextOptions<AppEducativaDbContext> options) : base(options) { }

    public DbSet<Nivel> Niveles => Set<Nivel>();
    public DbSet<Asignatura> Asignaturas => Set<Asignatura>();
    public DbSet<NivelAsignatura> NivelesAsignaturas => Set<NivelAsignatura>();
    public DbSet<EjeCurricular> EjesCurriculares => Set<EjeCurricular>();
    public DbSet<Unidad> Unidades => Set<Unidad>();
    public DbSet<ObjetivoAprendizaje> ObjetivosAprendizaje => Set<ObjetivoAprendizaje>();
    public DbSet<UnidadObjetivoAprendizaje> UnidadObjetivos => Set<UnidadObjetivoAprendizaje>();
    public DbSet<ObjetivoAprendizajeTransversal> Oats => Set<ObjetivoAprendizajeTransversal>();
    public DbSet<Habilidad> Habilidades => Set<Habilidad>();
    public DbSet<Actitud> Actitudes => Set<Actitud>();
    public DbSet<IndicadorEvaluacion> IndicadoresEvaluacion => Set<IndicadorEvaluacion>();

    public DbSet<CurriculumSource> CurriculumSources => Set<CurriculumSource>();
    public DbSet<CurriculumDocument> CurriculumDocuments => Set<CurriculumDocument>();
    public DbSet<CurriculumImportBatch> CurriculumImportBatches => Set<CurriculumImportBatch>();
    public DbSet<CurriculumReviewChange> CurriculumReviewChanges => Set<CurriculumReviewChange>();
    public DbSet<CurriculumReviewSession> CurriculumReviewSessions => Set<CurriculumReviewSession>();
    public DbSet<CurriculumReviewComment> CurriculumReviewComments => Set<CurriculumReviewComment>();
    public DbSet<CurriculumReviewDecision> CurriculumReviewDecisions => Set<CurriculumReviewDecision>();
    public DbSet<CurriculumRelease> CurriculumReleases => Set<CurriculumRelease>();
    public DbSet<CurriculumRecordSource> CurriculumRecordSources => Set<CurriculumRecordSource>();
    public DbSet<ClaseCurriculumSnapshot> ClaseCurriculumSnapshots => Set<ClaseCurriculumSnapshot>();

    public DbSet<ClassStructureGeneration> ClassStructureGenerations => Set<ClassStructureGeneration>();
    public DbSet<ClassStructureRevision> ClassStructureRevisions => Set<ClassStructureRevision>();
    public DbSet<AiUsageRecord> AiUsageRecords => Set<AiUsageRecord>();

    public DbSet<EducationalDocument> EducationalDocuments => Set<EducationalDocument>();
    public DbSet<EducationalDocumentGeneration> EducationalDocumentGenerations => Set<EducationalDocumentGeneration>();
    public DbSet<EducationalItem> EducationalItems => Set<EducationalItem>();
    public DbSet<EducationalItemOption> EducationalItemOptions => Set<EducationalItemOption>();
    public DbSet<EducationalItemIndicator> EducationalItemIndicators => Set<EducationalItemIndicator>();
    public DbSet<AssessmentSpecification> AssessmentSpecifications => Set<AssessmentSpecification>();
    public DbSet<EducationalDocumentRevision> EducationalDocumentRevisions => Set<EducationalDocumentRevision>();
    public DbSet<DocumentExport> DocumentExports => Set<DocumentExport>();

    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<DocumentoObjetivoAprendizaje> DocumentoObjetivos => Set<DocumentoObjetivoAprendizaje>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<SesionPlanificada> SesionesPlanificadas => Set<SesionPlanificada>();
    public DbSet<Planificacion> Planificaciones => Set<Planificacion>();
    public DbSet<Clase> Clases => Set<Clase>();
    public DbSet<ClaseIndicadorEvaluacion> ClaseIndicadores => Set<ClaseIndicadorEvaluacion>();

    public DbSet<PlanningScheduleConfiguration> PlanningScheduleConfigurations => Set<PlanningScheduleConfiguration>();
    public DbSet<WeeklyClassSchedule> WeeklyClassSchedules => Set<WeeklyClassSchedule>();
    public DbSet<PlanningExcludedDate> PlanningExcludedDates => Set<PlanningExcludedDate>();
    public DbSet<PlanningCalendarSession> PlanningCalendarSessions => Set<PlanningCalendarSession>();
    public DbSet<PlanningSessionHistory> PlanningSessionHistories => Set<PlanningSessionHistory>();
    public DbSet<PlanningSequenceProposal> PlanningSequenceProposals => Set<PlanningSequenceProposal>();
    public DbSet<PlanningSequenceProposalItem> PlanningSequenceProposalItems => Set<PlanningSequenceProposalItem>();
    public DbSet<PlanningSequenceItemIndicator> PlanningSequenceItemIndicators => Set<PlanningSequenceItemIndicator>();
    public DbSet<PlanningAlert> PlanningAlerts => Set<PlanningAlert>();
    public DbSet<LearningObjectiveDependency> LearningObjectiveDependencies => Set<LearningObjectiveDependency>();
    public DbSet<ClassLearningEvidence> ClassLearningEvidences => Set<ClassLearningEvidence>();
    public DbSet<PlanningSuggestionState> PlanningSuggestionStates => Set<PlanningSuggestionState>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<EducationalInstitution> EducationalInstitutions => Set<EducationalInstitution>();
    public DbSet<InstitutionMembership> InstitutionMemberships => Set<InstitutionMembership>();
    public DbSet<AcademicPeriod> AcademicPeriods => Set<AcademicPeriod>();
    public DbSet<SchoolCourse> SchoolCourses => Set<SchoolCourse>();
    public DbSet<CourseSubject> CourseSubjects => Set<CourseSubject>();
    public DbSet<CourseTeacherAssignment> CourseTeacherAssignments => Set<CourseTeacherAssignment>();
    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<StudentSupportPlan> StudentSupportPlans => Set<StudentSupportPlan>();
    public DbSet<ClassDuaStrategy> ClassDuaStrategies => Set<ClassDuaStrategy>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LearningAssessment> LearningAssessments => Set<LearningAssessment>();
    public DbSet<AssessmentScore> AssessmentScores => Set<AssessmentScore>();
    public DbSet<ClassFeedbackNote> ClassFeedbackNotes => Set<ClassFeedbackNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(x => x.PreferredTimeZone).HasMaxLength(80);
            e.Property(x => x.PreferredLanguage).HasMaxLength(20);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => x.NormalizedUserName);
            e.HasIndex(x => x.NormalizedEmail);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.UserId, x.ExpiresAt });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EducationalInstitution>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ShortName).HasMaxLength(80);
            e.Property(x => x.Rbd).HasMaxLength(40);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<InstitutionMembership>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.InstitutionId, x.UserId });
            e.HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AcademicPeriod>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.HasIndex(x => new { x.InstitutionId, x.Year });
            e.HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SchoolCourse>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.InstitutionId, x.AcademicPeriodId });
            e.HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.AcademicPeriod).WithMany().HasForeignKey(x => x.AcademicPeriodId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Level).WithMany().HasForeignKey(x => x.LevelId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CourseSubject>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SchoolCourseId, x.SubjectId }).IsUnique();
            e.HasOne(x => x.SchoolCourse).WithMany(c => c.Subjects).HasForeignKey(x => x.SchoolCourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CourseTeacherAssignment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CourseSubjectId, x.UserId });
            e.HasOne(x => x.CourseSubject).WithMany(s => s.Teachers).HasForeignKey(x => x.CourseSubjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeacherProfile>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserPreference>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(80).IsRequired();
            e.HasIndex(x => new { x.UserId, x.Key }).IsUnique();
        });

        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(120).IsRequired();
            e.HasIndex(x => new { x.InstitutionId, x.Timestamp });
            e.HasIndex(x => new { x.UserId, x.Timestamp });
        });

        modelBuilder.Entity<Nivel>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Codigo).IsUnique();
            e.HasIndex(x => x.Orden);
        });

        modelBuilder.Entity<Asignatura>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Codigo).IsUnique();
        });

        modelBuilder.Entity<NivelAsignatura>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Nivel).WithMany(n => n.NivelAsignaturas).HasForeignKey(x => x.NivelId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Asignatura).WithMany(a => a.NivelAsignaturas).HasForeignKey(x => x.AsignaturaId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.NivelId, x.AsignaturaId }).IsUnique();
        });

        modelBuilder.Entity<EjeCurricular>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.NivelAsignatura).WithMany(n => n.Ejes).HasForeignKey(x => x.NivelAsignaturaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Unidad>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.NivelAsignatura).WithMany(n => n.Unidades).HasForeignKey(x => x.NivelAsignaturaId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.NivelAsignaturaId, x.Numero });
        });

        modelBuilder.Entity<ObjetivoAprendizaje>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
            e.Property(x => x.Descripcion).IsRequired();
            e.Property(x => x.Version).HasMaxLength(40).IsRequired();
            e.HasOne(x => x.NivelAsignatura).WithMany(n => n.Objetivos).HasForeignKey(x => x.NivelAsignaturaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.EjeCurricular).WithMany(ej => ej.Objetivos).HasForeignKey(x => x.EjeCurricularId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.NivelAsignaturaId, x.Codigo, x.Version }).IsUnique();
        });

        modelBuilder.Entity<UnidadObjetivoAprendizaje>(e =>
        {
            e.HasKey(x => new { x.UnidadId, x.ObjetivoAprendizajeId });
            e.HasOne(x => x.Unidad).WithMany(u => u.Objetivos).HasForeignKey(x => x.UnidadId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ObjetivoAprendizaje).WithMany(o => o.Unidades).HasForeignKey(x => x.ObjetivoAprendizajeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IndicadorEvaluacion>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.ObjetivoAprendizaje).WithMany(o => o.Indicadores).HasForeignKey(x => x.ObjetivoAprendizajeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habilidad>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.NivelAsignatura).WithMany(n => n.Habilidades).HasForeignKey(x => x.NivelAsignaturaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Actitud>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Nivel).WithMany(n => n.Actitudes).HasForeignKey(x => x.NivelId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ObjetivoAprendizajeTransversal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
            e.HasOne(x => x.Nivel).WithMany(n => n.Oats).HasForeignKey(x => x.NivelId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.Codigo, x.NivelId, x.Version });
        });

        modelBuilder.Entity<CurriculumSource>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Url).IsRequired();
            e.HasIndex(x => x.Url);
            e.HasIndex(x => x.ExternalId).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");
        });

        modelBuilder.Entity<CurriculumDocument>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.HashSha256).HasMaxLength(64).IsRequired();
            // Empty hashes are retained for legacy/manual documents; non-empty hashes deduplicate downloads.
            e.HasIndex(x => x.HashSha256).IsUnique().HasFilter("\"HashSha256\" <> ''");
            e.HasOne(x => x.Source).WithMany(s => s.Documents).HasForeignKey(x => x.CurriculumSourceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CurriculumImportBatch>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.CurriculumSource).WithMany().HasForeignKey(x => x.CurriculumSourceId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CurriculumDocument).WithMany().HasForeignKey(x => x.CurriculumDocumentId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CurriculumSourceId, x.Status });
        });
        modelBuilder.Entity<CurriculumReviewChange>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.ImportBatch).WithMany(b => b.ReviewChanges).HasForeignKey(x => x.CurriculumImportBatchId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ReviewSession).WithMany(s => s.Changes).HasForeignKey(x => x.CurriculumReviewSessionId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.CurriculumImportBatchId);
            e.HasIndex(x => x.CurriculumReviewSessionId);
        });
        modelBuilder.Entity<CurriculumReviewSession>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasOne(x => x.ImportBatch).WithMany(b => b.ReviewSessions).HasForeignKey(x => x.CurriculumImportBatchId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.CurriculumImportBatchId);
            e.HasIndex(x => x.Estado);
        });
        modelBuilder.Entity<CurriculumReviewComment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Session).WithMany(s => s.Comments).HasForeignKey(x => x.CurriculumReviewSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.CurriculumReviewSessionId);
        });
        modelBuilder.Entity<CurriculumReviewDecision>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Session).WithMany(s => s.Decisions).HasForeignKey(x => x.CurriculumReviewSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CurriculumReviewSessionId, x.EntityTemporaryId });
        });
        modelBuilder.Entity<CurriculumRelease>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(300).IsRequired();
            e.Property(x => x.Version).HasMaxLength(40).IsRequired();
            e.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.PublishedAt);
            e.HasIndex(x => x.CurriculumImportBatchId);
        });
        modelBuilder.Entity<CurriculumRecordSource>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.CurriculumDocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.TipoEntidad, x.EntidadId });
        });

        modelBuilder.Entity<ClaseCurriculumSnapshot>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ClaseId).IsUnique();
        });

        modelBuilder.Entity<ClassStructureGeneration>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Provider).HasMaxLength(40).IsRequired();
            e.Property(x => x.Model).HasMaxLength(80).IsRequired();
            e.Property(x => x.PromptVersion).HasMaxLength(80).IsRequired();
            e.Property(x => x.ConfigurationFingerprint).HasMaxLength(128);
            e.Property(x => x.ErrorCode).HasMaxLength(80);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.ClassId, x.GenerationNumber }).IsUnique();
            e.HasIndex(x => new { x.ClassId, x.IsCurrentVersion });
            e.HasIndex(x => new { x.ClassId, x.Status });
            e.HasMany(x => x.Revisions).WithOne(r => r.Generation).HasForeignKey(r => r.GenerationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClassStructureRevision>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.HasIndex(x => new { x.GenerationId, x.RevisionNumber }).IsUnique();
            e.HasIndex(x => new { x.GenerationId, x.IsCurrent });
        });

        modelBuilder.Entity<AiUsageRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.OperationType).HasMaxLength(80).IsRequired();
            e.Property(x => x.Provider).HasMaxLength(40).IsRequired();
            e.Property(x => x.Model).HasMaxLength(80).IsRequired();
            e.Property(x => x.ErrorCode).HasMaxLength(80);
            e.Property(x => x.DocumentType).HasMaxLength(40);
            e.Property(x => x.GenerationType).HasMaxLength(40);
            e.HasIndex(x => new { x.ClassId, x.StartedAt });
            e.HasIndex(x => x.OperationType);
            e.HasIndex(x => new { x.DocumentId, x.StartedAt });
        });

        modelBuilder.Entity<EducationalDocument>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.BloomLevel).HasMaxLength(40).IsRequired();
            e.Property(x => x.Provider).HasMaxLength(40).IsRequired();
            e.Property(x => x.Model).HasMaxLength(80).IsRequired();
            e.Property(x => x.PromptVersion).HasMaxLength(80).IsRequired();
            e.Property(x => x.ObjectiveCode).HasMaxLength(80).IsRequired();
            e.Property(x => x.CurriculumRelease).HasMaxLength(120).IsRequired();
            e.Property(x => x.ConfigurationFingerprint).HasMaxLength(128);
            e.Property(x => x.TotalPoints).HasPrecision(10, 2);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.ClassId, x.DocumentType, x.IsCurrentVersion });
            e.HasIndex(x => new { x.ClassId, x.Status });
            e.HasMany(x => x.Items).WithOne(i => i.Document).HasForeignKey(i => i.EducationalDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Generations).WithOne(g => g.Document).HasForeignKey(g => g.EducationalDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Specifications).WithOne(s => s.Document).HasForeignKey(s => s.EducationalDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Revisions).WithOne(r => r.Document).HasForeignKey(r => r.EducationalDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EducationalDocumentGeneration>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ErrorCode).HasMaxLength(80);
            e.HasIndex(x => new { x.EducationalDocumentId, x.GenerationNumber }).IsUnique();
        });

        modelBuilder.Entity<EducationalItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.BloomLevel).HasMaxLength(40).IsRequired();
            e.Property(x => x.Points).HasPrecision(10, 2);
            e.HasIndex(x => new { x.EducationalDocumentId, x.Order });
            e.HasMany(x => x.Options).WithOne(o => o.Item).HasForeignKey(o => o.EducationalItemId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Indicators).WithOne(i => i.Item).HasForeignKey(i => i.EducationalItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EducationalItemOption>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EducationalItemId, x.Order });
        });

        modelBuilder.Entity<EducationalItemIndicator>(e =>
        {
            e.HasKey(x => new { x.EducationalItemId, x.EvaluationIndicatorId });
        });

        modelBuilder.Entity<AssessmentSpecification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.BloomLevel).HasMaxLength(40).IsRequired();
            e.Property(x => x.TotalPoints).HasPrecision(10, 2);
            e.Property(x => x.WeightPercentage).HasPrecision(6, 2);
            e.HasIndex(x => x.EducationalDocumentId);
        });

        modelBuilder.Entity<EducationalDocumentRevision>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EducationalDocumentId, x.RevisionNumber }).IsUnique();
        });

        modelBuilder.Entity<DocumentExport>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            e.Property(x => x.Sha256).HasMaxLength(64);
            e.Property(x => x.ErrorCode).HasMaxLength(80);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => x.RequestedAt);
            e.HasIndex(x => new { x.Status, x.ExpiresAt });
            e.HasIndex(x => x.PlanningId);
            e.HasIndex(x => x.ClassId);
            e.HasIndex(x => x.EducationalDocumentId);
        });

        modelBuilder.Entity<DocumentoObjetivoAprendizaje>(e =>
        {
            e.HasKey(x => new { x.DocumentoId, x.ObjetivoAprendizajeId });
            e.HasOne(x => x.Documento).WithMany(d => d.ObjetivosSeleccionados)
                .HasForeignKey(x => x.DocumentoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ObjetivoAprendizaje).WithMany()
                .HasForeignKey(x => x.ObjetivoAprendizajeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Planificacion>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(300).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasOne(x => x.Nivel).WithMany().HasForeignKey(x => x.NivelId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NivelAsignatura).WithMany().HasForeignKey(x => x.NivelAsignaturaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Unidad).WithMany().HasForeignKey(x => x.UnidadId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Clases).WithOne(c => c.Planificacion).HasForeignKey(c => c.PlanificacionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.InstitutionId, x.OwnerUserId });
            e.HasIndex(x => new { x.SchoolCourseId, x.AcademicPeriodId });
        });

        modelBuilder.Entity<Clase>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.NivelBloom).HasMaxLength(40).IsRequired();
            e.Property(x => x.Titulo).HasMaxLength(300);
            e.Property(x => x.Proposito).HasMaxLength(1000);
            e.HasOne(x => x.ObjetivoAprendizaje).WithMany().HasForeignKey(x => x.ObjetivoAprendizajeId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Documentos).WithOne(d => d.Clase).HasForeignKey(d => d.ClaseId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.PlanificacionId, x.Numero }).IsUnique();
        });

        modelBuilder.Entity<PlanningScheduleConfiguration>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TimeZoneId).HasMaxLength(80).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => x.PlanningId).IsUnique();
            e.HasOne(x => x.Planning).WithMany().HasForeignKey(x => x.PlanningId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.WeeklySchedules).WithOne(w => w.Configuration).HasForeignKey(w => w.PlanningScheduleConfigurationId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.ExcludedDates).WithOne(d => d.Configuration).HasForeignKey(d => d.PlanningScheduleConfigurationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WeeklyClassSchedule>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.PlanningScheduleConfigurationId, x.DayOfWeek, x.StartTime });
        });

        modelBuilder.Entity<PlanningExcludedDate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).HasMaxLength(200);
            e.HasIndex(x => new { x.PlanningScheduleConfigurationId, x.Date }).IsUnique();
        });

        modelBuilder.Entity<PlanningCalendarSession>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.Property(x => x.LockReason).HasMaxLength(200);
            e.Property(x => x.CancelReason).HasMaxLength(300);
            e.HasIndex(x => new { x.PlanningId, x.ScheduledDate });
            e.HasIndex(x => new { x.PlanningId, x.SessionNumber });
            e.HasIndex(x => x.ClassId);
            e.HasOne(x => x.Planning).WithMany().HasForeignKey(x => x.PlanningId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Class).WithMany().HasForeignKey(x => x.ClassId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.History).WithOne(h => h.Session).HasForeignKey(h => h.PlanningCalendarSessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlanningSessionHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).HasMaxLength(300);
        });

        modelBuilder.Entity<PlanningSequenceProposal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.Property(x => x.PlanningVersionHash).HasMaxLength(64);
            e.HasIndex(x => new { x.PlanningId, x.IsCurrent });
            e.HasOne(x => x.Planning).WithMany().HasForeignKey(x => x.PlanningId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Items).WithOne(i => i.Proposal).HasForeignKey(i => i.PlanningSequenceProposalId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlanningSequenceProposalItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.BloomLevel).HasMaxLength(40);
            e.Property(x => x.SuggestedTitle).HasMaxLength(300);
            e.HasIndex(x => new { x.PlanningSequenceProposalId, x.Order });
            e.HasOne(x => x.CalendarSession).WithMany().HasForeignKey(x => x.CalendarSessionId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Indicators).WithOne(i => i.Item).HasForeignKey(i => i.PlanningSequenceProposalItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlanningSequenceItemIndicator>(e =>
        {
            e.HasKey(x => new { x.PlanningSequenceProposalItemId, x.EvaluationIndicatorId });
        });

        modelBuilder.Entity<PlanningAlert>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AlertCode).HasMaxLength(80).IsRequired();
            e.Property(x => x.Message).HasMaxLength(500).IsRequired();
            e.HasIndex(x => new { x.PlanningId, x.IsResolved, x.Severity });
        });

        modelBuilder.Entity<LearningObjectiveDependency>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.PlanningId, x.PrerequisiteObjectiveId, x.DependentObjectiveId });
        });

        modelBuilder.Entity<ClassLearningEvidence>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Source).HasMaxLength(80);
            e.HasIndex(x => new { x.ClassId, x.EvaluationIndicatorId });
            e.HasOne(x => x.Class).WithMany().HasForeignKey(x => x.ClassId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlanningSuggestionState>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SuggestionCode).HasMaxLength(120).IsRequired();
            e.HasIndex(x => new { x.PlanningId, x.SuggestionCode });
        });

        modelBuilder.Entity<ClaseIndicadorEvaluacion>(e =>
        {
            e.HasKey(x => new { x.ClaseId, x.IndicadorEvaluacionId });
            e.HasOne(x => x.Clase).WithMany(c => c.Indicadores).HasForeignKey(x => x.ClaseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.IndicadorEvaluacion).WithMany().HasForeignKey(x => x.IndicadorEvaluacionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Documento>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nivel).HasMaxLength(80).IsRequired();
            e.Property(x => x.Asignatura).HasMaxLength(120).IsRequired();
            e.Property(x => x.Tema).HasMaxLength(300).IsRequired();
            e.Property(x => x.ContenidoGenerado).IsRequired();
            e.HasOne(x => x.NivelNav).WithMany().HasForeignKey(x => x.NivelId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AsignaturaNav).WithMany().HasForeignKey(x => x.AsignaturaId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne(i => i.Documento).HasForeignKey(i => i.DocumentoId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Sesiones).WithOne(s => s.Documento).HasForeignKey(s => s.DocumentoId).OnDelete(DeleteBehavior.Cascade);
        });

        // Documento.AsignaturaId → Asignatura global; Unidad es snapshot string (sin FK)
        modelBuilder.Entity<Documento>().Property(x => x.UnidadId).IsRequired(false);

        modelBuilder.Entity<Item>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Enunciado).IsRequired();
            e.Property(x => x.AlternativasJson).IsRequired();
            e.HasOne(x => x.IndicadorEvaluacion).WithMany().HasForeignKey(x => x.IndicadorEvaluacionId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SesionPlanificada>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.ObjetivoAprendizaje).WithMany().HasForeignKey(x => x.ObjetivoAprendizajeId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.DocumentoId, x.Numero }).IsUnique();
        });

        modelBuilder.Entity<Student>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.InstitutionId, x.LastName, x.FirstName });
        });

        modelBuilder.Entity<CourseEnrollment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SchoolCourseId, x.StudentId }).IsUnique();
        });

        modelBuilder.Entity<StudentSupportPlan>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.StudentId, x.IsActive });
        });

        modelBuilder.Entity<ClassDuaStrategy>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ClassId);
        });

        modelBuilder.Entity<AttendanceRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ClassId, x.StudentId }).IsUnique();
        });

        modelBuilder.Entity<LearningAssessment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.InstitutionId, x.Date });
            e.HasIndex(x => x.SchoolCourseId);
        });

        modelBuilder.Entity<AssessmentScore>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.LearningAssessmentId, x.StudentId }).IsUnique();
        });

        modelBuilder.Entity<ClassFeedbackNote>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ClassId);
        });
    }
}
