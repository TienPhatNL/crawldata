using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClassroomService.Application.Features.Enrollments.Commands;
using ClassroomService.Application.Features.Enrollments.Queries;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using HttpStatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for managing all enrollment operations
/// </summary>
[ApiController]
[Route("api/enrollments")]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<EnrollmentsController> _logger;

    public EnrollmentsController(IMediator mediator, ICurrentUserService currentUserService, ILogger<EnrollmentsController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    // ===== COURSE-SPECIFIC ENROLLMENT OPERATIONS =====

    /// <summary>
    /// Join a course (self-enrollment)
    /// </summary>
    /// <param name="courseId">The ID of the course to join</param>
    /// <param name="request">Join course request with optional access code</param>
    /// <returns>Enrollment result</returns>
    /// <response code="200">Successfully joined the course</response>
    /// <response code="400">Invalid request or already enrolled</response>
    /// <response code="404">Course not found</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("courses/{courseId:guid}/join")]
    [ProducesResponseType(typeof(EnrollmentResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EnrollmentResponse>> JoinCourse(Guid courseId, [FromBody] JoinCourseRequest? request = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var command = new JoinCourseWithCodeCommand
            {
                CourseId = courseId,
                StudentId = userId,
                AccessCode = request?.AccessCode
            };

            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation(Messages.Logging.StudentJoinedCourse, userId, courseId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while user was trying to join course {CourseId}", courseId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { message = Messages.Helpers.FormatError(Messages.Error.JoinCourseFailed, "An error occurred") });
        }
    }

    /// <summary>
    /// Leave a course (self-unenrollment)
    /// </summary>
    /// <param name="courseId">The ID of the course to leave</param>
    /// <returns>Unenrollment result</returns>
    /// <response code="200">Successfully left the course</response>
    /// <response code="400">Invalid request or not enrolled</response>
    /// <response code="404">Course not found</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("courses/{courseId:guid}/leave")]
    [ProducesResponseType(typeof(UnenrollStudentResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UnenrollStudentResponse>> LeaveCourse(Guid courseId)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var command = new SelfUnenrollCommand
            {
                CourseId = courseId,
                StudentId = userId
            };

            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation(Messages.Logging.StudentUnenrolled, userId, courseId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while user was trying to leave course {CourseId}", courseId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { message = Messages.Helpers.FormatError(Messages.Error.SelfUnenrollmentFailed, "An error occurred") });
        }
    }

    /// <summary>
    /// Check if current user is enrolled in the course
    /// </summary>
    /// <param name="courseId">The ID of the course to check</param>
    /// <returns>Enrollment status</returns>
    /// <response code="200">Enrollment status retrieved</response>
    /// <response code="404">Course not found</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("courses/{courseId:guid}/status")]
    [ProducesResponseType(typeof(EnrollmentStatusResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EnrollmentStatusResponse>> GetEnrollmentStatus(Guid courseId)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var query = new GetEnrollmentStatusQuery
            {
                CourseId = courseId,
                StudentId = userId
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking enrollment status for course {CourseId}", courseId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { message = Messages.Helpers.FormatError(Messages.Error.EnrollmentRetrievalFailed, "An error occurred") });
        }
    }

    /// <summary>
    /// Lecturer: Unenroll a specific student from the course
    /// </summary>
    /// <param name="courseId">The ID of the course</param>
    /// <param name="studentId">The ID of the student to unenroll</param>
    /// <param name="request">Unenrollment details</param>
    /// <returns>Unenrollment result</returns>
    /// <response code="200">Successfully unenrolled the student</response>
    /// <response code="400">Invalid request or not enrolled</response>
    /// <response code="403">Not authorized to unenroll students</response>
    /// <response code="404">Course not found</response>
    [HttpDelete("courses/{courseId:guid}/students/{studentId:guid}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(UnenrollStudentResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnenrollStudentResponse>> UnenrollStudent(
        Guid courseId, 
        Guid studentId,
        [FromBody] UnenrollStudentRequest request)
    {
        try
        {
            var command = new UnenrollStudentCommand
            {
                CourseId = courseId,
                StudentId = studentId,
                Reason = request.Reason,
                UnenrolledBy = GetCurrentUserId()
            };

            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation(Messages.Logging.StudentUnenrolled, studentId, courseId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while unenrolling student {StudentId} from course {CourseId}", 
                studentId, courseId);
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new { message = Messages.Helpers.FormatError(Messages.Error.UnenrollmentFailed, "An error occurred") });
        }
    }

    // ===== GENERAL ENROLLMENT OPERATIONS =====

    /// <summary>
    /// Imports student enrollments from Excel file (Lecturers only)
    /// This endpoint allows importing enrollments for multiple courses in one file
    /// </summary>
    /// <param name="file">Excel file with student enrollments</param>
    /// <param name="createAccountIfNotFound">Auto-create student accounts for allowed email domains (e.g., .edu)</param>
    /// <returns>Import result</returns>
    /// <response code="200">Import completed</response>
    /// <response code="400">Invalid file or data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturers only</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("import")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(ImportStudentEnrollmentsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ImportStudentEnrollmentsResponse>> ImportStudentEnrollments(
        [Required] IFormFile file,
        [FromForm] bool createAccountIfNotFound = false)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }

        var command = new ImportStudentEnrollmentsCommand
        {
            ExcelFile = file,
            ImportedBy = _currentUserService.UserId.Value,
            CreateAccountIfNotFound = createAccountIfNotFound
        };

        var response = await _mediator.Send(command);

        if (!response.Success && response.SuccessfulEnrollments == 0)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Downloads a template Excel file for student enrollment imports (Lecturers only)
    /// </summary>
    /// <returns>Excel template file</returns>
    /// <response code="200">Template file generated successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturers only</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("import-template")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(FileContentResult), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public IActionResult GetEnrollmentTemplate()
    {
        try
        {
            var excelService = HttpContext.RequestServices.GetRequiredService<Domain.Interfaces.IExcelService>();
            var templateData = excelService.GenerateStudentEnrollmentTemplate();
            var fileName = $"StudentEnrollmentTemplate_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(templateData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception)
        {
            return StatusCode(HttpStatusCodes.Status500InternalServerError, "Error generating template");
        }
    }

    /// <summary>
    /// Imports students into a specific course from Excel file (Lecturers only)
    /// This endpoint allows importing students directly into a specific course
    /// </summary>
    /// <param name="courseId">The course ID to import students into</param>
    /// <param name="file">Excel file with student data</param>
    /// <param name="createAccountIfNotFound">Auto-create student accounts for allowed email domains (e.g., .edu)</param>
    /// <returns>Import result</returns>
    /// <response code="200">Import completed</response>
    /// <response code="400">Invalid file or data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturers only, must be course lecturer</response>
    /// <response code="404">Course not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("courses/{courseId:guid}/import-students")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(ImportCourseStudentsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ImportCourseStudentsResponse>> ImportCourseStudents(
        Guid courseId,
        [Required] IFormFile file,
        [FromForm] bool createAccountIfNotFound = false)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }

        var command = new ImportCourseStudentsCommand
        {
            CourseId = courseId,
            ExcelFile = file,
            ImportedBy = _currentUserService.UserId.Value,
            CreateAccountIfNotFound = createAccountIfNotFound
        };

        var response = await _mediator.Send(command);

        if (!response.Success && response.SuccessfulEnrollments == 0)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("Unauthorized") || response.Message.Contains("Only lecturers"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Downloads a template Excel file for course-specific student imports (Lecturers only)
    /// </summary>
    /// <returns>Excel template file</returns>
    /// <response code="200">Template file generated successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturers only</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("courses/import-students-template")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(FileContentResult), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public IActionResult GetCourseStudentsTemplate()
    {
        try
        {
            var excelService = HttpContext.RequestServices.GetRequiredService<Domain.Interfaces.IExcelService>();
            var templateData = excelService.GenerateCourseStudentsTemplate();
            var fileName = $"CourseStudentsTemplate_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(templateData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception)
        {
            return StatusCode(HttpStatusCodes.Status500InternalServerError, "Error generating template");
        }
    }

    /// <summary>
    /// Get current user's enrolled courses
    /// </summary>
    /// <returns>List of enrolled courses</returns>
    /// <response code="200">Enrolled courses retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("my-courses")]
    [ProducesResponseType(typeof(GetMyEnrolledCoursesResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetMyEnrolledCoursesResponse>> GetMyEnrolledCourses()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var query = new GetMyEnrolledCoursesQuery
            {
                StudentId = userId
            };

            var result = await _mediator.Send(query);

            if (!result.Success)
            {
                return StatusCode(HttpStatusCodes.Status500InternalServerError, result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving enrolled courses for user");
            return StatusCode(HttpStatusCodes.Status500InternalServerError, new 
            { 
                success = false,
                message = Messages.Helpers.FormatError(Messages.Error.EnrollmentRetrievalFailed, "An error occurred") 
            });
        }
    }

    /// <summary>
    /// Get all enrolled students in a course (All authorized user)
    /// </summary>
    /// <param name="courseId">The course ID</param>
    /// <returns>List of enrolled students</returns>
    /// <response code="200">Enrolled students retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Only course authorized user can access</response>
    /// <response code="404">Course not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("courses/{courseId:guid}/students")]
    [Authorize]
    [ProducesResponseType(typeof(GetCourseEnrolledStudentsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetCourseEnrolledStudentsResponse>> GetCourseEnrolledStudents(Guid courseId)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }

        var query = new GetCourseEnrolledStudentsQuery
        {
            CourseId = courseId,
            RequestedBy = _currentUserService.UserId.Value
        };

        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("Unauthorized"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Get a specific enrolled student's details in a course
    /// </summary>
    /// <param name="courseId">The course ID</param>
    /// <param name="studentId">The student ID</param>
    /// <returns>Enrolled student details</returns>
    /// <response code="200">Student details retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Only lecturer or enrolled students can access</response>
    /// <response code="404">Course or student not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("courses/{courseId:guid}/students/{studentId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(GetEnrolledStudentByIdResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetEnrolledStudentByIdResponse>> GetEnrolledStudentById(
        Guid courseId, 
        Guid studentId)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }

        var query = new GetEnrolledStudentByIdQuery
        {
            CourseId = courseId,
            StudentId = studentId,
            RequestedBy = _currentUserService.UserId.Value
        };

        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("not authorized"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    private Guid GetCurrentUserId()
    {
        if (_currentUserService.UserId.HasValue)
        {
            return _currentUserService.UserId.Value;
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException(Messages.Error.UserIdNotFound);
        }
        return userId;
    }
}

/// <summary>
/// Request model for unenrolling a student
/// </summary>
public class UnenrollStudentRequest
{
    /// <summary>
    /// Reason for unenrolling the student
    /// </summary>
    /// <example>Student requested to drop the course</example>
    public string? Reason { get; set; }
}

/// <summary>
/// Request model for joining a course
/// </summary>
public class JoinCourseRequest
{
    /// <summary>
    /// Access code for the course (if required)
    /// </summary>
    /// <example>ABC123</example>
    public string? AccessCode { get; set; }
}