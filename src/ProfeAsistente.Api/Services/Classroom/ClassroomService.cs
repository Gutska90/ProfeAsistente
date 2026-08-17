using ProfeAsistente.Shared.Dtos;

namespace ProfeAsistente.Api.Services.Classroom;

public interface IClassroomService
{
    Task<TeacherDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<StudentDto> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentDto>> ListInstitutionStudentsAsync(Guid institutionId, CancellationToken cancellationToken = default);
    Task<CourseRosterDto> GetRosterAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<CourseRosterDto> GetRosterForClassAsync(Guid classId, CancellationToken cancellationToken = default);
    Task EnrollAsync(Guid courseId, EnrollStudentRequest request, CancellationToken cancellationToken = default);
    Task<SupportPlanDto> AddSupportPlanAsync(Guid studentId, CreateSupportPlanRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportPlanDto>> ListSupportPlansAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<ClassDuaStrategyDto> AddDuaStrategyAsync(Guid classId, AddClassDuaStrategyRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassDuaStrategyDto>> ListDuaAsync(Guid classId, CancellationToken cancellationToken = default);
    Task SaveAttendanceAsync(Guid classId, SaveAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(Guid classId, CancellationToken cancellationToken = default);
    Task<LearningAssessmentDto> CreateAssessmentAsync(CreateLearningAssessmentRequest request, CancellationToken cancellationToken = default);
    Task SaveScoresAsync(Guid assessmentId, IReadOnlyList<SaveAssessmentScoreRequest> scores, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssessmentScoreDto>> GetScoresAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LearningAssessmentDto>> ListAssessmentsAsync(Guid? courseId, Guid? classId = null, CancellationToken cancellationToken = default);
    Task<AssessmentEvidenceSummaryDto> GetAssessmentEvidenceAsync(Guid assessmentId, CancellationToken cancellationToken = default);
}

public sealed class ClassroomService : IClassroomService
{
    private readonly ITeacherDashboardService _dashboard;
    private readonly ICourseRosterService _roster;
    private readonly IStudentSupportService _support;
    private readonly IClassDuaService _dua;
    private readonly IAttendanceService _attendance;
    private readonly IAssessmentService _assessments;

    public ClassroomService(
        ITeacherDashboardService dashboard,
        ICourseRosterService roster,
        IStudentSupportService support,
        IClassDuaService dua,
        IAttendanceService attendance,
        IAssessmentService assessments)
    {
        _dashboard = dashboard;
        _roster = roster;
        _support = support;
        _dua = dua;
        _attendance = attendance;
        _assessments = assessments;
    }

    public Task<TeacherDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        => _dashboard.GetDashboardAsync(cancellationToken);

    public Task<StudentDto> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken = default)
        => _roster.CreateStudentAsync(request, cancellationToken);

    public Task<IReadOnlyList<StudentDto>> ListInstitutionStudentsAsync(Guid institutionId, CancellationToken cancellationToken = default)
        => _roster.ListInstitutionStudentsAsync(institutionId, cancellationToken);

    public Task<CourseRosterDto> GetRosterAsync(Guid courseId, CancellationToken cancellationToken = default)
        => _roster.GetRosterAsync(courseId, cancellationToken);

    public Task<CourseRosterDto> GetRosterForClassAsync(Guid classId, CancellationToken cancellationToken = default)
        => _roster.GetRosterForClassAsync(classId, cancellationToken);

    public Task EnrollAsync(Guid courseId, EnrollStudentRequest request, CancellationToken cancellationToken = default)
        => _roster.EnrollAsync(courseId, request, cancellationToken);

    public Task<SupportPlanDto> AddSupportPlanAsync(Guid studentId, CreateSupportPlanRequest request, CancellationToken cancellationToken = default)
        => _support.AddSupportPlanAsync(studentId, request, cancellationToken);

    public Task<IReadOnlyList<SupportPlanDto>> ListSupportPlansAsync(Guid studentId, CancellationToken cancellationToken = default)
        => _support.ListSupportPlansAsync(studentId, cancellationToken);

    public Task<ClassDuaStrategyDto> AddDuaStrategyAsync(Guid classId, AddClassDuaStrategyRequest request, CancellationToken cancellationToken = default)
        => _dua.AddDuaStrategyAsync(classId, request, cancellationToken);

    public Task<IReadOnlyList<ClassDuaStrategyDto>> ListDuaAsync(Guid classId, CancellationToken cancellationToken = default)
        => _dua.ListDuaAsync(classId, cancellationToken);

    public Task SaveAttendanceAsync(Guid classId, SaveAttendanceRequest request, CancellationToken cancellationToken = default)
        => _attendance.SaveAttendanceAsync(classId, request, cancellationToken);

    public Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(Guid classId, CancellationToken cancellationToken = default)
        => _attendance.GetAttendanceAsync(classId, cancellationToken);

    public Task<LearningAssessmentDto> CreateAssessmentAsync(CreateLearningAssessmentRequest request, CancellationToken cancellationToken = default)
        => _assessments.CreateAssessmentAsync(request, cancellationToken);

    public Task SaveScoresAsync(Guid assessmentId, IReadOnlyList<SaveAssessmentScoreRequest> scores, CancellationToken cancellationToken = default)
        => _assessments.SaveScoresAsync(assessmentId, scores, cancellationToken);

    public Task<IReadOnlyList<AssessmentScoreDto>> GetScoresAsync(Guid assessmentId, CancellationToken cancellationToken = default)
        => _assessments.GetScoresAsync(assessmentId, cancellationToken);

    public Task<IReadOnlyList<LearningAssessmentDto>> ListAssessmentsAsync(Guid? courseId, Guid? classId = null, CancellationToken cancellationToken = default)
        => _assessments.ListAssessmentsAsync(courseId, classId, cancellationToken);

    public Task<AssessmentEvidenceSummaryDto> GetAssessmentEvidenceAsync(Guid assessmentId, CancellationToken cancellationToken = default)
        => _assessments.GetAssessmentEvidenceAsync(assessmentId, cancellationToken);
}
