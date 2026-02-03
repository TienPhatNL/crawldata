using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClassroomService.Application.Features.Reports.Commands;
using ClassroomService.Application.Features.Reports.Queries;
using ClassroomService.Domain.Constants;
using HttpStatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for managing assignment reports (submissions)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Reports")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IMediator mediator, ILogger<ReportsController> _logger)
    {
        _mediator = mediator;
        this._logger = _logger;
    }

    #region Submit & Manage Reports

    /// <summary>
    /// Create a draft report for an assignment
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student
    /// 
    /// Creates a draft report that can be collaboratively edited by team members.
    /// - **Individual Assignments**: Only the student can create their draft
    /// - **Group Assignments**: Any group member can create the draft for the team
    /// 
    /// The report is created with **Draft** status. Team members can then:
    /// 1. View and edit the draft (updates increment version number)
    /// 2. Group leader (or individual student) submits the final version
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Student)]
    [ProducesResponseType(typeof(SubmitReportResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SubmitReportResponse>> SubmitReport([FromBody] SubmitReportCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting report");
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update a draft report (individual)
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student
    /// 
    /// Allows collaborative editing of draft reports:
    /// - **Individual Assignments**: Only the report creator can edit
    /// - **Group Assignments**: Any group member can edit the draft
    /// 
    /// Each update increments the version number for tracking changes.
    /// Reports can only be updated when in **Draft** or **RequiresRevision** status.
    /// </remarks>
    [HttpPut("{id:guid}")]
     [Authorize(Roles = RoleConstants.Student)]
    [ProducesResponseType(typeof(UpdateReportResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateReportResponse>> UpdateReport(Guid id, [FromBody] UpdateReportCommand command)
    {
        try
        {
            command.ReportId = id;
            var response = await _mediator.Send(command);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating report {ReportId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a report (only Draft or Submitted status)
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student
    /// 
    /// Users can only delete their own reports.
    /// Hard delete is only allowed for reports in Draft or Submitted status.
    /// Reports in other statuses cannot be deleted.
    /// </remarks>
    [HttpDelete("{id:guid}")]
     [Authorize(Roles = RoleConstants.Student)]
    [ProducesResponseType(typeof(DeleteReportResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DeleteReportResponse>> DeleteReport(Guid id)
    {
        try
        {
            var command = new DeleteReportCommand { ReportId = id };
            var response = await _mediator.Send(command);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report {ReportId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Resubmit a report after revision request
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student
    /// 
    /// Used to resubmit a report after receiving a revision request.
    /// Report must be in RequiresRevision status.
    /// Increments the version number.
    /// </remarks>
    [HttpPost("resubmit")]
    [Authorize(Roles = RoleConstants.Student)]
    [ProducesResponseType(typeof(ResubmitReportResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResubmitReportResponse>> ResubmitReport([FromBody] ResubmitReportCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resubmitting report {ReportId}", command.ReportId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Revert report content to a previous historical version
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student
    /// 
    /// Allows students to restore Submission (text content) and FileUrl (file attachment) to a previous version from history.
    /// Status remains unchanged - only the content is reverted.
    /// 
    /// **Authorization Rules:**
    /// - Individual reports: Only the original submitter can revert content
    /// - Group reports: Only the group leader can revert content
    /// 
    /// **Request Body:**
    /// - `reportId`: The ID of the report
    /// - `version`: The version number to restore content from (from report history)
    /// - `comment`: Optional comment explaining the revert (not a Report property, just for history tracking)
    /// 
    /// **Use Cases:**
    /// - Student wants to restore content from an earlier version
    /// - Group leader needs to undo recent changes made by team members
    /// - Reverting to a better previous submission
    /// </remarks>
    [HttpPost("revert")]
    [Authorize(Roles = RoleConstants.Student)]
    [ProducesResponseType(typeof(RevertContentReportResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RevertContentReportResponse>> RevertContentReport([FromBody] RevertContentReportCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverting report {ReportId}", command.ReportId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update report status (Student/Leader only)
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student
    /// 
    /// **Allowed Status Changes:**
    /// - Submitted → Draft: Allows student/leader to revert a submitted report back to draft
    /// - Resubmitted → RequiresRevision: Allows student/leader to revert resubmission back to revision state
    /// 
    /// **Authorization Rules:**
    /// - Individual reports: Only the report submitter can update status
    /// - Group reports: Only the group leader can update status
    /// 
    /// **Assignment Requirements:**
    /// - Assignment must be in Active or Extended status
    /// - Assignment cannot be Draft, Scheduled, Closed, or Archived
    /// 
    /// **Use Cases:**
    /// - Student accidentally submitted too early and wants to make more changes
    /// - Student resubmitted but realizes they need more time to work on revisions
    /// - Group leader needs to pull back submission for team review
    /// 
    /// </remarks>
    /// <param name="id">Report ID</param>
    /// <param name="command">Status update details</param>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = RoleConstants.Student)]
    [ProducesResponseType(typeof(UpdateReportStatusResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UpdateReportStatusResponse>> UpdateReportStatus(
        Guid id, 
        [FromBody] UpdateReportStatusCommand command)
    {
        try
        {
            command.ReportId = id;
            var response = await _mediator.Send(command);
            
            if (!response.Success)
            {
                if (response.Message.Contains("Only the group leader") || 
                    response.Message.Contains("Only students") ||
                    response.Message.Contains("You can only update your own"))
                {
                    return StatusCode(HttpStatusCodes.Status403Forbidden, response);
                }
                return BadRequest(response);
            }
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating report status for report {ReportId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, 
                new { success = false, message = "Internal server error" });
        }
    }

    #endregion

    #region Query Reports

    /// <summary>
    /// Get a report by ID
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** 
    /// - Student: Can view own reports only
    /// - Lecturer: Can view all reports
    /// </remarks>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{RoleConstants.Student},{RoleConstants.Lecturer}")]
    [ProducesResponseType(typeof(GetReportByIdResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetReportByIdResponse>> GetReportById(Guid id)
    {
        try
        {
            var query = new GetReportByIdQuery { ReportId = id };
            var response = await _mediator.Send(query);
            return response.Success ? Ok(response) : NotFound(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report {ReportId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all reports for a specific assignment
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** 
    /// - Student: Can view own submission only
    /// - Lecturer: Can view all submissions for the assignment
    /// 
    /// Supports pagination and filtering by status.
    /// </remarks>
    [HttpGet("assignment/{assignmentId:guid}")]
    [Authorize(Roles = $"{RoleConstants.Student},{RoleConstants.Lecturer}")]
    [ProducesResponseType(typeof(GetReportsByAssignmentResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetReportsByAssignmentResponse>> GetReportsByAssignment(
        Guid assignmentId, 
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = new GetReportsByAssignmentQuery 
            { 
                AssignmentId = assignmentId,
                Status = status != null && Enum.TryParse<Domain.Enums.ReportStatus>(status, out var s) ? s : null,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var response = await _mediator.Send(query);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports for assignment {AssignmentId}", assignmentId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get current user's own reports across all assignments
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student
    /// 
    /// Returns all reports submitted by the current user.
    /// Supports filtering by course, assignment, and status.
    /// </remarks>
    [HttpGet("my-reports")]
    [Authorize(Roles = RoleConstants.Student)]
    [ProducesResponseType(typeof(GetMyReportsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetMyReportsResponse>> GetMyReports(
        [FromQuery] Guid? courseId = null,
        [FromQuery] Guid? assignmentId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? courseName = null,
        [FromQuery] string? assignmentName = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = new GetMyReportsQuery
            {
                CourseId = courseId,
                AssignmentId = assignmentId,
                Status = status != null && Enum.TryParse<Domain.Enums.ReportStatus>(status, out var s) ? s : null,
                CourseName = courseName,
                AssignmentName = assignmentName,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var response = await _mediator.Send(query);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user's reports");
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all reports across a course
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Lecturer ONLY
    /// 
    /// Returns all reports for all assignments in a course.
    /// Supports filtering by status and date range.
    /// </remarks>
    [HttpGet("course/{courseId:guid}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(GetReportsByCourseResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetReportsByCourseResponse>> GetReportsByCourse(
        Guid courseId,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = new GetReportsByCourseQuery
            {
                CourseId = courseId,
                Status = status != null && Enum.TryParse<Domain.Enums.ReportStatus>(status, out var s) ? s : null,
                FromDate = fromDate,
                ToDate = toDate,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var response = await _mediator.Send(query);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports for course {CourseId}", courseId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get reports requiring grading (pending review queue)
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Lecturer ONLY
    /// 
    /// Returns reports in Submitted, Resubmitted, or UnderReview status.
    /// Useful for managing grading workload.
    /// Sorted by submission date (oldest first).
    /// </remarks>
    [HttpGet("requiring-grading")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(GetReportsRequiringGradingResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetReportsRequiringGradingResponse>> GetReportsRequiringGrading(
        [FromQuery] Guid? courseId = null,
        [FromQuery] Guid? assignmentId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = new GetReportsRequiringGradingQuery
            {
                CourseId = courseId,
                AssignmentId = assignmentId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var response = await _mediator.Send(query);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports requiring grading");
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get late submissions
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Lecturer ONLY
    /// 
    /// Returns reports submitted after the assignment deadline.
    /// Includes days late calculation for penalty application.
    /// </remarks>
    [HttpGet("late-submissions")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(GetLateSubmissionsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetLateSubmissionsResponse>> GetLateSubmissions(
        [FromQuery] Guid? courseId = null,
        [FromQuery] Guid? assignmentId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = new GetLateSubmissionsQuery
            {
                CourseId = courseId,
                AssignmentId = assignmentId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var response = await _mediator.Send(query);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting late submissions");
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get assignment grading statistics
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Lecturer ONLY
    /// 
    /// Returns comprehensive statistics:
    /// - Total submissions
    /// - Graded count
    /// - Pending count
    /// - Average grade
    /// - Late submissions count
    /// - Status breakdown
    /// </remarks>
    [HttpGet("statistics/{assignmentId:guid}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(GetReportStatisticsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetReportStatisticsResponse>> GetReportStatistics(Guid assignmentId)
    {
        try
        {
            var query = new GetReportStatisticsQuery { AssignmentId = assignmentId };
            var response = await _mediator.Send(query);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report statistics for assignment {AssignmentId}", assignmentId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    #endregion

    #region Grading Actions

    /// <summary>
    /// Grade a report
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Lecturer ONLY
    /// 
    /// Assigns a grade and feedback to a submitted report.
    /// Grade must not exceed the assignment's maximum points.
    /// </remarks>
    [HttpPost("grade")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(GradeReportResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GradeReportResponse>> GradeReport([FromBody] GradeReportCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error grading report {ReportId}", command.ReportId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Request revision for a report
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Lecturer ONLY
    /// 
    /// Sends report back to student for revisions.
    /// Changes status to RequiresRevision.
    /// Student can then update and resubmit.
    /// </remarks>
    [HttpPost("request-revision")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(RequestRevisionResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RequestRevisionResponse>> RequestRevision([FromBody] RequestRevisionCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting revision for report {ReportId}", command.ReportId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Reject a report with feedback
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Lecturer ONLY
    /// 
    /// Rejects a submitted/under review report and provides feedback.
    /// Changes status to RequiresRevision so student can revise and resubmit.
    /// </remarks>
    [HttpPost("reject")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(RejectReportResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RejectReportResponse>> RejectReport([FromBody] RejectReportCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting report {ReportId}", command.ReportId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Submit a draft report for grading
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student
    /// 
    /// Submits the final version of a draft report for lecturer review:
    /// - **Individual Assignments**: Only the report creator can submit
    /// - **Group Assignments**: Only the group leader can submit
    /// 
    /// Changes status from **Draft** to **Submitted** (or **Late** if past deadline).
    /// Once submitted, the report cannot be edited unless lecturer requests revision.
    /// </remarks>
    [HttpPost("{id:guid}/submit-draft")]
    [Authorize(Roles = RoleConstants.Student)]
    [ProducesResponseType(typeof(SubmitDraftReportResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SubmitDraftReportResponse>> SubmitDraftReport(Guid id)
    {
        try
        {
            var command = new SubmitDraftReportCommand { ReportId = id };
            var response = await _mediator.Send(command);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting draft report {ReportId}", id);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    #endregion

    #region Export

    /// <summary>
    /// Export assignment grades to Excel
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Lecturer ONLY
    /// 
    /// Exports all non-draft submissions for an assignment to an Excel file.
    /// Includes student/group info, submission details, and grades.
    /// </remarks>
    [HttpGet("export/{assignmentId:guid}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(FileResult), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportAssignmentGrades(Guid assignmentId)
    {
        try
        {
            var query = new ExportAssignmentGradesQuery { AssignmentId = assignmentId };
            var response = await _mediator.Send(query);
            
            if (!response.Success)
            {
                return NotFound(new { success = false, message = response.Message });
            }

            return File(response.FileContent!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting grades for assignment {AssignmentId}", assignmentId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    #endregion

    #region Report History & Change Tracking

    /// <summary>
    /// Get complete history of changes for a report with pagination
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student (own reports), Lecturer
    /// 
    /// Returns detailed audit trail of all changes made to a report including:
    /// - Who made each change (with contributor names)
    /// - When it was made
    /// - What was changed (with detailed diffs)
    /// - Version at time of change
    /// - Change summaries and unified diffs
    /// 
    /// **Pagination:**
    /// - Default: 20 items per page
    /// - Max: 100 items per page
    /// - Returns metadata: total count, total pages, has previous/next
    /// 
    /// **Authorization:**
    /// - Students: Can view history of reports they created or group reports they're a member of
    /// - Lecturers: Can view history of all reports in their courses
    /// 
    /// **Example:** GET /api/reports/{reportId}/history?pageNumber=1&amp;pageSize=20
    /// </remarks>
    [HttpGet("{reportId:guid}/history")]
    [Authorize(Roles = $"{RoleConstants.Student},{RoleConstants.Lecturer}")]
    [ProducesResponseType(typeof(Domain.DTOs.ReportHistoryResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<Domain.DTOs.ReportHistoryResponse>> CGetReportHistory(
        Guid reportId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = new GetReportHistoryQuery 
            { 
                ReportId = reportId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var response = await _mediator.Send(query);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report history for {ReportId}", reportId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get detailed information about a specific version
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student (own reports), Lecturer
    /// 
    /// Returns complete details for a single version including:
    /// - All fields changed
    /// - Change summary (e.g., "+2 lines, -1 lines")
    /// - Unified diff for visualization
    /// - Detailed change operations (JSON)
    /// - Contributor names
    /// 
    /// **Example:** GET /api/reports/{reportId}/history/{version}
    /// </remarks>
    [HttpGet("{reportId:guid}/history/{version:int}")]
    [Authorize(Roles = $"{RoleConstants.Student},{RoleConstants.Lecturer}")]
    [ProducesResponseType(typeof(Domain.DTOs.ReportHistoryDto), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<Domain.DTOs.ReportHistoryDto>> GetVersionDetail(
        Guid reportId,
        int version)
    {
        try
        {
            var query = new GetVersionDetailQuery 
            { 
                ReportId = reportId, 
                Version = version 
            };
            var response = await _mediator.Send(query);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version detail for report {ReportId} version {Version}", reportId, version);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Compare report versions with flexible modes
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student (own reports), Lecturer
    /// 
    /// **Three Comparison Modes:**
    /// 
    /// **1. Sequence Mode (default):** Compare specific sequences
    /// - Example: `?version1=2&amp;seq1=1&amp;version2=3&amp;seq2=2&amp;mode=sequence`
    /// - Compares v2.1 vs v3.2 (specific records)
    /// - Omit seq parameters to auto-select content changes
    /// 
    /// **2. Version Mode:** Compare aggregate versions (all sequences)
    /// - Example: `?version1=1&amp;version2=4&amp;mode=version`
    /// - Shows everything that changed from Version 1 to Version 4
    /// - Includes final state after all sequences in each version
    /// - Shows status changes, grading, content updates, etc.
    /// 
    /// **3. IntraVersion Mode:** Compare within same version
    /// - Example: `?version1=2&amp;version2=2&amp;seq1=1&amp;seq2=3&amp;mode=intraVersion`
    /// - Shows evolution from v2.1 → v2.2 → v2.3
    /// - Useful for understanding review cycles
    /// - Version1 and Version2 must be equal
    /// 
    /// **Returns:**
    /// - Content/status at each point
    /// - Field-level differences
    /// - Unified diff
    /// - Change summary
    /// - Contributors
    /// </remarks>
    [HttpGet("{reportId:guid}/compare")]
    [Authorize(Roles = $"{RoleConstants.Student},{RoleConstants.Lecturer}")]
    [ProducesResponseType(typeof(Domain.DTOs.CompareVersionsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<Domain.DTOs.CompareVersionsResponse>> CompareVersions(
        Guid reportId,
        [FromQuery] int version1,
        [FromQuery] int version2)
    {
        try
        {
            if (version1 <= 0 || version2 <= 0)
            {
                return BadRequest(new { success = false, message = "Version numbers must be positive integers" });
            }

            var query = new CompareReportVersionsQuery 
            { 
                ReportId = reportId, 
                Version1 = version1, 
                Version2 = version2
            };
            var response = await _mediator.Send(query);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing versions for report {ReportId}", reportId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get human-readable timeline of report changes
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student (own reports), Lecturer
    /// 
    /// Returns a chronological timeline of all actions performed on the report in human-readable format.
    /// 
    /// Example timeline items:
    /// - "student@example.com created the draft"
    /// - "teammate@example.com edited content"
    /// - "leader@example.com submitted for review"
    /// - "lecturer@example.com graded the report"
    /// 
    /// Perfect for viewing the report lifecycle at a glance.
    /// </remarks>
    [HttpGet("{reportId:guid}/timeline")]
    [Authorize(Roles = $"{RoleConstants.Student},{RoleConstants.Lecturer}")]
    [ProducesResponseType(typeof(Domain.DTOs.TimelineResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<Domain.DTOs.TimelineResponse>> GetReportTimeline(Guid reportId)
    {
        try
        {
            var query = new GetReportTimelineQuery { ReportId = reportId };
            var response = await _mediator.Send(query);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timeline for report {ReportId}", reportId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    #endregion

    #region File Management

    /// <summary>
    /// Upload a file attachment for a report (Student only)
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student
    /// 
    /// Uploads a file attachment to an existing report.
    /// - **Status Restriction**: Only allowed when report is in **Draft** or **RequiresRevision** status
    /// - **Ownership**: Only the report submitter (or group members for group reports) can upload files
    /// - **File Types**: PDF, DOC, DOCX, TXT, ZIP, RAR
    /// - **File Size**: Maximum 50MB
    /// - **Versioning**: Old files are preserved in history (not deleted on replacement)
    /// - **Tracking**: All file changes are tracked in ReportHistory for audit trail
    /// 
    /// For group reports, any group member can upload files.
    /// </remarks>
    /// <param name="reportId">The report ID</param>
    /// <param name="file">The file to upload (PDF, DOCX, TXT, ZIP, RAR - max 50MB)</param>
    /// <returns>The upload response with file URL</returns>
    /// <response code="200">File uploaded successfully</response>
    /// <response code="400">Invalid file, wrong status, or request error</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Not report owner or group member</response>
    /// <response code="404">Report not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{reportId}/upload-file")]
    [Authorize(Roles = RoleConstants.Student)]
    [ProducesResponseType(typeof(UploadReportFileResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)] // 50MB
    [RequestSizeLimit(52428800)] // 50MB
    public async Task<ActionResult<UploadReportFileResponse>> UploadReportFile(
        Guid reportId,
        IFormFile file)
    {
        try
        {
            var command = new UploadReportFileCommand
            {
                ReportId = reportId,
                File = file
            };

            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                if (response.Message.Contains("permission") || response.Message.Contains("do not have"))
                {
                    return StatusCode(HttpStatusCodes.Status403Forbidden, response);
                }
                if (response.Message.Contains("status"))
                {
                    return BadRequest(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for report {ReportId}", reportId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete the file attachment from a report (Student only)
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student
    /// 
    /// Deletes the file attachment from an existing report.
    /// - **Status Restriction**: Only allowed when report is in **Draft** or **RequiresRevision** status
    /// - **Ownership**: Only the report submitter (or group members for group reports) can delete files
    /// - **Permanent Deletion**: File is deleted from S3 storage
    /// - **Tracking**: Deletion is tracked in ReportHistory for audit trail
    /// 
    /// Note: This is different from replacing a file (which preserves old versions).
    /// Use this only when explicitly removing the file attachment.
    /// </remarks>
    /// <param name="reportId">The report ID</param>
    /// <returns>The deletion response</returns>
    /// <response code="200">File deleted successfully</response>
    /// <response code="400">Bad request or wrong status</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Not report owner or group member</response>
    /// <response code="404">Report not found or no file to delete</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{reportId}/file")]
    [Authorize(Roles = RoleConstants.Student)]
    [ProducesResponseType(typeof(DeleteReportFileResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeleteReportFileResponse>> DeleteReportFile(
        [FromRoute] Guid reportId)
    {
        try
        {
            var command = new DeleteReportFileCommand
            {
                ReportId = reportId
            };

            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                if (response.Message.Contains("not found") || response.Message.Contains("does not have"))
                {
                    return NotFound(response);
                }
                if (response.Message.Contains("permission") || response.Message.Contains("do not have"))
                {
                    return StatusCode(HttpStatusCodes.Status403Forbidden, response);
                }
                if (response.Message.Contains("status"))
                {
                    return BadRequest(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file for report {ReportId}", reportId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { success = false, message = "Internal server error" });
        }
    }

    #endregion

    #region AI Content Detection

    /// <summary>
    /// Check report for AI-generated content
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Lecturer ONLY
    /// 
    /// Performs AI content detection analysis on a submitted report using ZeroGPT detector.
    /// 
    /// **Requirements:**
    /// - Report must be in **Submitted** or **Resubmitted** status
    /// - Report must have text content (Submission field not empty)
    /// - Lecturer must be enrolled in the course
    /// 
    /// **Returns:**
    /// - AI percentage (0-100): Higher values indicate more likely AI-generated content
    /// - Provider: AI detection service used (e.g., "ZeroGPT")
    /// - Timestamp and checker information
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "message": "AI content check completed successfully",
    ///   "result": {
    ///     "aiPercentage": 72.5,
    ///     "provider": "ZeroGPT",
    ///     "checkedByName": "Dr. Smith",
    ///     "checkedAt": "2025-12-04T10:30:00Z"
    ///   }
    /// }
    /// ```
    /// 
    /// **Note:** File attachments are NOT analyzed, only text content in the Submission field.
    /// </remarks>
    /// <param name="request">Optional notes about the check</param>
    [HttpPost("ai-check")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(CheckReportAIResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckReportAIResponse>> CheckReportAI(
        [FromBody] Domain.DTOs.AICheckRequestDto? request = null)
    {
        try
        {
            var command = new CheckReportAICommand
            {
                ReportId = request?.ReportId ?? Guid.Empty,
                Notes = request?.Notes
            };

            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                if (response.Message.Contains("not authenticated") || 
                    response.Message.Contains("Only lecturers") ||
                    response.Message.Contains("do not have access"))
                {
                    return StatusCode(HttpStatusCodes.Status403Forbidden, response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking AI content for report {ReportId}", request?.ReportId ?? Guid.Empty);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, 
                new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all AI check history for a report
    /// </summary>
    /// <remarks>
    /// **Supported Roles:** Student (own reports), Lecturer
    /// 
    /// Retrieves the complete history of all AI content checks performed on a report.
    /// 
    /// **Authorization:**
    /// - **Students:** Can view AI checks for their own reports or group reports they're part of
    /// - **Lecturers:** Can view AI checks for all reports in their courses
    /// 
    /// **Returns:**
    /// List of all AI checks ordered by most recent first, including:
    /// - AI percentage scores
    /// - Who performed each check
    /// - Timestamps
    /// - Optional notes
    /// 
    /// **Use Cases:**
    /// - Tracking changes in AI detection over multiple submissions
    /// - Viewing historical checks by different lecturers
    /// - Monitoring resubmissions after revisions
    /// </remarks>
    /// <param name="reportId">The ID of the report</param>
    [HttpGet("{reportId}/ai-checks")]
    [Authorize(Roles = $"{RoleConstants.Student},{RoleConstants.Lecturer}")]
    [ProducesResponseType(typeof(GetReportAIChecksResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetReportAIChecksResponse>> GetReportAIChecks(Guid reportId)
    {
        try
        {
            var query = new GetReportAIChecksQuery
            {
                ReportId = reportId
            };

            var response = await _mediator.Send(query);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }
                if (response.Message.Contains("not authenticated") || 
                    response.Message.Contains("do not have permission") ||
                    response.Message.Contains("do not have access"))
                {
                    return StatusCode(HttpStatusCodes.Status403Forbidden, response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving AI checks for report {ReportId}", reportId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, 
                new { success = false, message = "Internal server error" });
        }
    }

    #endregion
}
