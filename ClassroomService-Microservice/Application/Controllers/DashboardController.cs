using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Dashboard.DTOs;
using ClassroomService.Application.Features.Dashboard.Queries;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassroomService.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    // ============= STUDENT ENDPOINTS =============

    /// <summary>
    /// Get student's overall grades overview across all courses
    /// </summary>
    [HttpGet("student/grades/overview")]
    [Authorize(Roles = RoleConstants.Student)]
    public async Task<ActionResult<DashboardResponse<StudentGradesOverviewDto>>> GetStudentGradesOverview(
        [FromQuery] Guid? termId = null)
    {
        var query = new GetStudentGradesOverviewQuery { TermId = termId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get detailed grades for a specific course
    /// </summary>
    [HttpGet("student/grades/course/{courseId}")]
    [Authorize(Roles = RoleConstants.Student)]
    public async Task<ActionResult<DashboardResponse<CourseGradesDetailDto>>> GetStudentCourseGrades(Guid courseId)
    {
        var query = new GetStudentCourseGradesQuery { CourseId = courseId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get all pending assignments for student
    /// </summary>
    [HttpGet("student/assignments/pending")]
    [Authorize(Roles = RoleConstants.Student)]
    public async Task<ActionResult<DashboardResponse<PendingAssignmentsDto>>> GetPendingAssignments(
        [FromQuery] Guid? termId = null)
    {
        var query = new GetPendingAssignmentsQuery { TermId = termId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get student's courses for a specific term
    /// </summary>
    [HttpGet("student/courses/{termId}")]
    [Authorize(Roles = RoleConstants.Student)]
    public async Task<ActionResult<DashboardResponse<CurrentCoursesDto>>> GetCourses(Guid termId)
    {
        var query = new GetCoursesQuery { TermId = termId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get student performance analytics
    /// </summary>
    [HttpGet("student/analytics/performance")]
    [Authorize(Roles = RoleConstants.Student)]
    public async Task<ActionResult<DashboardResponse<StudentPerformanceAnalyticsDto>>> GetStudentPerformanceAnalytics(
        [FromQuery] Guid? termId = null)
    {
        var query = new GetStudentPerformanceAnalyticsQuery { TermId = termId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get detailed grade breakdown with weighted contributions
    /// </summary>
    [HttpGet("student/grades/breakdown/{courseId}")]
    [Authorize(Roles = RoleConstants.Student)]
    public async Task<ActionResult<DashboardResponse<StudentGradeBreakdownDto>>> GetStudentGradeBreakdown(Guid courseId)
    {
        var query = new GetStudentGradeBreakdownQuery { CourseId = courseId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    // ============= LECTURER ENDPOINTS =============

    /// <summary>
    /// Get lecturer's courses overview
    /// </summary>
    [HttpGet("lecturer/courses/overview")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    public async Task<ActionResult<DashboardResponse<LecturerCoursesOverviewDto>>> GetLecturerCoursesOverview(
        [FromQuery] Guid? termId = null)
    {
        var query = new GetLecturerCoursesOverviewQuery { TermId = termId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get all reports pending grading
    /// </summary>
    [HttpGet("lecturer/grading/pending")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    public async Task<ActionResult<DashboardResponse<GradingQueueDto>>> GetGradingQueue(
        [FromQuery] Guid? courseId = null)
    {
        var query = new GetGradingQueueQuery { CourseId = courseId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get student performance for a specific course
    /// </summary>
    [HttpGet("lecturer/students/performance/{courseId}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    public async Task<ActionResult<DashboardResponse<CourseStudentPerformanceDto>>> GetCourseStudentPerformance(Guid courseId)
    {
        var query = new GetCourseStudentPerformanceQuery { CourseId = courseId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get assignment statistics for a course
    /// </summary>
    [HttpGet("lecturer/assignments/statistics/{courseId}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    public async Task<ActionResult<DashboardResponse<AssignmentStatisticsDto>>> GetAssignmentStatistics(Guid courseId)
    {
        var query = new GetAssignmentStatisticsQuery { CourseId = courseId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Export course student grades to Excel
    /// </summary>
    [HttpGet("lecturer/grades/export/{courseId}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    public async Task<IActionResult> ExportCourseGrades(Guid courseId)
    {
        var query = new Features.Dashboard.Queries.ExportCourseGrades.ExportCourseGradesQuery(courseId);
        var excelBytes = await _mediator.Send(query);
        
        var fileName = $"course-grades-{courseId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // ============= SHARED ENDPOINTS =============

    /// <summary>
    /// Get available terms for the logged-in user (student or lecturer)
    /// </summary>
    [HttpGet("terms")]
    [Authorize(Roles = $"{RoleConstants.Student},{RoleConstants.Lecturer}")]
    public async Task<ActionResult<DashboardResponse<UserTermsDto>>> GetUserTerms()
    {
        var query = new Features.Dashboard.Queries.GetUserTerms.GetUserTermsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
