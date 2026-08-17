using AppEducativa.Api.Services.Classroom;
using AppEducativa.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppEducativa.Api.Controllers;

[ApiController]
[Authorize]
public class ClassroomController : ControllerBase
{
    private readonly IClassroomService _classroom;

    public ClassroomController(IClassroomService classroom) => _classroom = classroom;

    [HttpGet("api/teacher/dashboard")]
    public async Task<ActionResult<TeacherDashboardDto>> Dashboard(CancellationToken ct)
        => Ok(await _classroom.GetDashboardAsync(ct));

    [HttpGet("api/institutions/{institutionId:guid}/students")]
    public async Task<ActionResult<IReadOnlyList<StudentDto>>> Students(Guid institutionId, CancellationToken ct)
        => Ok(await _classroom.ListInstitutionStudentsAsync(institutionId, ct));

    [HttpPost("api/institutions/{institutionId:guid}/students")]
    public async Task<ActionResult<StudentDto>> CreateStudent(Guid institutionId, [FromBody] CreateStudentRequest request, CancellationToken ct)
        => Ok(await _classroom.CreateStudentAsync(new CreateStudentRequest
        {
            InstitutionId = institutionId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            BirthDate = request.BirthDate,
            Notes = request.Notes
        }, ct));

    [HttpGet("api/courses/{courseId:guid}/roster")]
    public async Task<ActionResult<CourseRosterDto>> Roster(Guid courseId, CancellationToken ct)
        => Ok(await _classroom.GetRosterAsync(courseId, ct));

    [HttpPost("api/courses/{courseId:guid}/roster")]
    public async Task<IActionResult> Enroll(Guid courseId, [FromBody] EnrollStudentRequest request, CancellationToken ct)
    {
        await _classroom.EnrollAsync(courseId, request, ct);
        return NoContent();
    }

    [HttpGet("api/students/{studentId:guid}/support-plans")]
    public async Task<ActionResult<IReadOnlyList<SupportPlanDto>>> Plans(Guid studentId, CancellationToken ct)
        => Ok(await _classroom.ListSupportPlansAsync(studentId, ct));

    [HttpPost("api/students/{studentId:guid}/support-plans")]
    public async Task<ActionResult<SupportPlanDto>> AddPlan(Guid studentId, [FromBody] CreateSupportPlanRequest request, CancellationToken ct)
        => Ok(await _classroom.AddSupportPlanAsync(studentId, request, ct));

    [HttpGet("api/clases/{classId:guid}/roster")]
    public async Task<ActionResult<CourseRosterDto>> ClassRoster(Guid classId, CancellationToken ct)
        => Ok(await _classroom.GetRosterForClassAsync(classId, ct));

    [HttpGet("api/clases/{classId:guid}/dua")]
    public async Task<ActionResult<IReadOnlyList<ClassDuaStrategyDto>>> Dua(Guid classId, CancellationToken ct)
        => Ok(await _classroom.ListDuaAsync(classId, ct));

    [HttpPost("api/clases/{classId:guid}/dua")]
    public async Task<ActionResult<ClassDuaStrategyDto>> AddDua(Guid classId, [FromBody] AddClassDuaStrategyRequest request, CancellationToken ct)
        => Ok(await _classroom.AddDuaStrategyAsync(classId, request, ct));

    [HttpGet("api/clases/{classId:guid}/asistencia")]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordDto>>> Attendance(Guid classId, CancellationToken ct)
        => Ok(await _classroom.GetAttendanceAsync(classId, ct));

    [HttpPut("api/clases/{classId:guid}/asistencia")]
    public async Task<IActionResult> SaveAttendance(Guid classId, [FromBody] SaveAttendanceRequest request, CancellationToken ct)
    {
        await _classroom.SaveAttendanceAsync(classId, request, ct);
        return NoContent();
    }

    [HttpGet("api/evaluaciones")]
    public async Task<ActionResult<IReadOnlyList<LearningAssessmentDto>>> Assessments(
        [FromQuery] Guid? courseId, [FromQuery] Guid? classId, CancellationToken ct)
        => Ok(await _classroom.ListAssessmentsAsync(courseId, classId, ct));

    [HttpPost("api/evaluaciones")]
    public async Task<ActionResult<LearningAssessmentDto>> CreateAssessment([FromBody] CreateLearningAssessmentRequest request, CancellationToken ct)
        => Ok(await _classroom.CreateAssessmentAsync(request, ct));

    [HttpGet("api/evaluaciones/{id:guid}/puntajes")]
    public async Task<ActionResult<IReadOnlyList<AssessmentScoreDto>>> GetScores(Guid id, CancellationToken ct)
        => Ok(await _classroom.GetScoresAsync(id, ct));

    [HttpPut("api/evaluaciones/{id:guid}/puntajes")]
    public async Task<IActionResult> Scores(Guid id, [FromBody] IReadOnlyList<SaveAssessmentScoreRequest> scores, CancellationToken ct)
    {
        await _classroom.SaveScoresAsync(id, scores, ct);
        return NoContent();
    }
}
