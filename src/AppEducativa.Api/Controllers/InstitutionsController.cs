using AppEducativa.Api.Services.Institutions;
using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppEducativa.Api.Controllers;

[ApiController]
[Authorize]
public class InstitutionsController : ControllerBase
{
    private readonly IInstitutionService _institutions;

    public InstitutionsController(IInstitutionService institutions) => _institutions = institutions;

    [HttpGet("api/institutions")]
    public async Task<ActionResult<IReadOnlyList<InstitutionDto>>> List(CancellationToken ct)
        => Ok(await _institutions.ListAsync(ct));

    [HttpGet("api/institutions/{id:guid}")]
    public async Task<ActionResult<InstitutionDto>> Get(Guid id, CancellationToken ct)
    {
        var i = await _institutions.GetAsync(id, ct);
        return i is null ? NotFound() : Ok(i);
    }

    [Authorize(Policy = AppPolicies.CanManageUsers)]
    [HttpPost("api/institutions")]
    public async Task<ActionResult<InstitutionDto>> Create([FromBody] CreateInstitutionRequest request, CancellationToken ct)
        => Ok(await _institutions.CreateAsync(request, ct));

    [HttpPut("api/institutions/{id:guid}")]
    public async Task<ActionResult<InstitutionDto>> Update(Guid id, [FromBody] UpdateInstitutionRequest request, CancellationToken ct)
        => Ok(await _institutions.UpdateAsync(id, request, ct));

    [HttpPost("api/institutions/{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await _institutions.SetActiveAsync(id, true, ct);
        return NoContent();
    }

    [HttpPost("api/institutions/{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _institutions.SetActiveAsync(id, false, ct);
        return NoContent();
    }

    [HttpGet("api/institutions/{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<InstitutionMembershipDto>>> Members(Guid id, CancellationToken ct)
        => Ok(await _institutions.ListMembersAsync(id, ct));

    [HttpPost("api/institutions/{id:guid}/members")]
    public async Task<ActionResult<InstitutionMembershipDto>> AddMember(Guid id, [FromBody] AddMembershipRequest request, CancellationToken ct)
        => Ok(await _institutions.AddMemberAsync(id, request, ct));

    [HttpDelete("api/institutions/{id:guid}/members/{membershipId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid membershipId, CancellationToken ct)
    {
        await _institutions.RemoveMemberAsync(id, membershipId, ct);
        return NoContent();
    }

    [HttpGet("api/institutions/{institutionId:guid}/academic-periods")]
    public async Task<ActionResult<IReadOnlyList<AcademicPeriodDto>>> Periods(Guid institutionId, CancellationToken ct)
        => Ok(await _institutions.ListPeriodsAsync(institutionId, ct));

    [HttpPost("api/institutions/{institutionId:guid}/academic-periods")]
    public async Task<ActionResult<AcademicPeriodDto>> CreatePeriod(Guid institutionId, [FromBody] CreateAcademicPeriodRequest request, CancellationToken ct)
        => Ok(await _institutions.CreatePeriodAsync(institutionId, request, ct));

    [HttpPost("api/academic-periods/{id:guid}/activate")]
    public async Task<IActionResult> ActivatePeriod(Guid id, CancellationToken ct)
    {
        await _institutions.ActivatePeriodAsync(id, ct);
        return NoContent();
    }

    [HttpPost("api/academic-periods/{id:guid}/close")]
    public async Task<IActionResult> ClosePeriod(Guid id, CancellationToken ct)
    {
        await _institutions.ClosePeriodAsync(id, ct);
        return NoContent();
    }

    [HttpGet("api/institutions/{institutionId:guid}/courses")]
    public async Task<ActionResult<IReadOnlyList<SchoolCourseDto>>> Courses(Guid institutionId, CancellationToken ct)
        => Ok(await _institutions.ListCoursesAsync(institutionId, ct));

    [HttpPost("api/institutions/{institutionId:guid}/courses")]
    public async Task<ActionResult<SchoolCourseDto>> CreateCourse(Guid institutionId, [FromBody] CreateSchoolCourseRequest request, CancellationToken ct)
        => Ok(await _institutions.CreateCourseAsync(institutionId, request, ct));

    [HttpGet("api/courses/{id:guid}")]
    public async Task<ActionResult<SchoolCourseDto>> GetCourse(Guid id, CancellationToken ct)
    {
        var c = await _institutions.GetCourseAsync(id, ct);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpGet("api/courses/{id:guid}/subjects")]
    public async Task<ActionResult<IReadOnlyList<CourseSubjectDto>>> Subjects(Guid id, CancellationToken ct)
        => Ok(await _institutions.ListSubjectsAsync(id, ct));

    [HttpPost("api/courses/{id:guid}/subjects")]
    public async Task<ActionResult<CourseSubjectDto>> AddSubject(Guid id, [FromBody] CreateCourseSubjectRequest request, CancellationToken ct)
        => Ok(await _institutions.AddSubjectAsync(id, request, ct));

    [HttpGet("api/course-subjects/{id:guid}/teachers")]
    public async Task<ActionResult<IReadOnlyList<CourseTeacherAssignmentDto>>> Teachers(Guid id, CancellationToken ct)
        => Ok(await _institutions.ListTeachersAsync(id, ct));

    [HttpPost("api/course-subjects/{id:guid}/teachers")]
    public async Task<ActionResult<CourseTeacherAssignmentDto>> AssignTeacher(Guid id, [FromBody] AssignTeacherRequest request, CancellationToken ct)
        => Ok(await _institutions.AssignTeacherAsync(id, request, ct));

    [HttpDelete("api/course-teacher-assignments/{id:guid}")]
    public async Task<IActionResult> RemoveTeacher(Guid id, CancellationToken ct)
    {
        await _institutions.RemoveTeacherAssignmentAsync(id, ct);
        return NoContent();
    }
}
