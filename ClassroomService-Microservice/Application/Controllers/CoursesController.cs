using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClassroomService.Application.Features.Courses.Commands;
using ClassroomService.Application.Features.Courses.Queries;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;
using HttpStatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for managing courses
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Courses")]
[Authorize] // Require authentication for all endpoints
public class CoursesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the CoursesController
    /// </summary>
    /// <param name="mediator">The mediator instance</param>
    /// <param name="currentUserService">Current user service</param>
    public CoursesController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Creates a new course (Lecturer only)
    /// </summary>
    /// <param name="command">The course creation details</param>
    /// <returns>The course creation response</returns>
    /// <response code="200">Course created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturer role required</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(CreateCourseResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateCourseResponse>> CreateCourse([FromBody] CreateCourseCommand command)
    {
        // Ensure the lecturer creating the course is the current user
        if (_currentUserService.UserId.HasValue)
        {
            command.LecturerId = (Guid)_currentUserService.UserId;
        }

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets a course by ID
    /// </summary>
    /// <param name="id">The course ID</param>
    /// <returns>The course details</returns>
    /// <response code="200">Course found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Course not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetCourseResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetCourseResponse>> GetCourse(Guid id)
    {
        var query = new GetCourseQuery
        {
            CourseId = id,
            CurrentUserId = _currentUserService.UserId,
            CurrentUserRole = _currentUserService.Role
        };
        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets a course by unique code
    /// </summary>
    /// <param name="uniqueCode">The 6-character unique course code (e.g., A1B2C3)</param>
    /// <returns>The course details</returns>
    /// <response code="200">Course found</response>
    /// <response code="400">Invalid unique code format</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Course not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("by-code/{uniqueCode}")]
    [ProducesResponseType(typeof(GetCourseResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetCourseResponse>> GetCourseByUniqueCode(string uniqueCode)
    {
        if (string.IsNullOrWhiteSpace(uniqueCode) || uniqueCode.Length != 6)
        {
            return BadRequest(new { message = "Unique code must be exactly 6 characters" });
        }

        var query = new GetCourseByUniqueCodeQuery
        {
            UniqueCode = uniqueCode.ToUpper(),
            CurrentUserId = _currentUserService.UserId,
            CurrentUserRole = _currentUserService.Role
        };
        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets courses for the current user with advanced filtering and pagination
    /// </summary>
    /// <param name="asLecturer">Whether to get courses as lecturer (true) or student (false)</param>
    /// <param name="name">Search by course name (partial match)</param>
    /// <param name="courseCode">Search by course code (partial match)</param>
    /// <param name="lecturerName">Filter by lecturer name (partial match)</param>
    /// <param name="createdAfter">Filter courses created after this date</param>
    /// <param name="createdBefore">Filter courses created before this date</param>
    /// <param name="minEnrollmentCount">Minimum number of enrolled students</param>
    /// <param name="maxEnrollmentCount">Maximum number of enrolled students</param>
    /// <param name="page">Page number for pagination (1-based)</param>
    /// <param name="pageSize">Number of items per page (max 100)</param>
    /// <param name="sortBy">Sort field (Name, CourseCode, CreatedAt, EnrollmentCount)</param>
    /// <param name="sortDirection">Sort direction (asc or desc)</param>
    /// <returns>Filtered and paginated list of user's courses</returns>
    /// <response code="200">Courses retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("my-courses")]
    [ProducesResponseType(typeof(GetMyCoursesResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetMyCoursesResponse>> GetMyCourses(
        [FromQuery] bool asLecturer = false,
        [FromQuery] string? name = null,
        [FromQuery] string? courseCode = null,
        [FromQuery] string? lecturerName = null,
        [FromQuery] DateTime? createdAfter = null,
        [FromQuery] DateTime? createdBefore = null,
        [FromQuery] int? minEnrollmentCount = null,
        [FromQuery] int? maxEnrollmentCount = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "desc")
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = Messages.Error.UserIdNotFound });
        }

        // Create the command with filter parameters
        var command = new GetMyCoursesCommand
        {
            UserId = _currentUserService.UserId.Value,
            AsLecturer = asLecturer,
            Filter = new CourseFilterDto
            {
                Name = name,
                CourseCode = courseCode,
                LecturerName = lecturerName,
                CreatedAfter = createdAfter,
                CreatedBefore = createdBefore,
                MinEnrollmentCount = minEnrollmentCount,
                MaxEnrollmentCount = maxEnrollmentCount,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDirection = sortDirection
            }
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets all courses with filtering and pagination (Staff only)
    /// </summary>
    /// <param name="name">Search by course name (partial match)</param>
    /// <param name="courseCode">Search by course code (partial match)</param>
    /// <param name="lecturerName">Filter by lecturer name (partial match)</param>
    /// <param name="status">Filter by course status (Active, PendingApproval, Rejected)</param>
    /// <param name="createdAfter">Filter courses created after this date</param>
    /// <param name="createdBefore">Filter courses created before this date</param>
    /// <param name="minEnrollmentCount">Minimum number of enrolled students</param>
    /// <param name="maxEnrollmentCount">Maximum number of enrolled students</param>
    /// <param name="page">Page number for pagination (1-based)</param>
    /// <param name="pageSize">Number of items per page (max 100)</param>
    /// <param name="sortBy">Sort field (Name, CourseCode, CreatedAt, EnrollmentCount)</param>
    /// <param name="sortDirection">Sort direction (asc or desc)</param>
    /// <returns>Filtered and paginated list of all courses</returns>
    /// <response code="200">Courses retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Staff role required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("all")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(GetAllCoursesResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetAllCoursesResponse>> GetAllCourses(
        [FromQuery] string? name = null,
        [FromQuery] string? courseCode = null,
        [FromQuery] string? lecturerName = null,
        [FromQuery] CourseStatus? status = null,
        [FromQuery] DateTime? createdAfter = null,
        [FromQuery] DateTime? createdBefore = null,
        [FromQuery] int? minEnrollmentCount = null,
        [FromQuery] int? maxEnrollmentCount = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "desc")
    {
        var query = new GetAllCoursesQuery
        {
            Filter = new CourseFilterDto
            {
                Name = name,
                CourseCode = courseCode,
                LecturerName = lecturerName,
                Status = status,
                CreatedAfter = createdAfter,
                CreatedBefore = createdBefore,
                MinEnrollmentCount = minEnrollmentCount,
                MaxEnrollmentCount = maxEnrollmentCount,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDirection = sortDirection
            },
            CurrentUserId = _currentUserService.UserId,
            CurrentUserRole = _currentUserService.Role
        };

        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Approves a pending course (Staff only)
    /// </summary>
    /// <param name="id">Course ID</param>
    /// <param name="command">Approval details</param>
    /// <returns>Approved course details</returns>
    /// <response code="200">Course approved successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Staff only</response>
    /// <response code="404">Course not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}/approve")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(ApproveCourseResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApproveCourseResponse>> ApproveCourse(
        Guid id,
        [FromBody] ApproveCourseCommand command)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = Messages.Error.UserIdNotFound });
        }

        command.CourseId = id;
        command.ApprovedBy = _currentUserService.UserId.Value;

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Rejects a pending course (Staff only)
    /// </summary>
    /// <param name="id">Course ID</param>
    /// <param name="command">Rejection details</param>
    /// <returns>Rejected course details</returns>
    /// <response code="200">Course rejected successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Staff only</response>
    /// <response code="404">Course not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}/reject")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(RejectCourseResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RejectCourseResponse>> RejectCourse(
        Guid id,
        [FromBody] RejectCourseCommand command)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = Messages.Error.UserIdNotFound });
        }

        command.CourseId = id;
        command.RejectedBy = _currentUserService.UserId.Value;

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets detailed statistics for a specific course (Lecturer/Staff only)
    /// </summary>
    /// <param name="id">The course ID</param>
    /// <returns>Course statistics including enrollments, assignments, and activity data</returns>
    /// <response code="200">Course statistics retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Lecturer/Staff role required</response>
    /// <response code="404">Course not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}/statistics")]
    [Authorize(Roles = $"{RoleConstants.Lecturer},{RoleConstants.Staff}")]
    [ProducesResponseType(typeof(GetCourseStatisticsResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetCourseStatisticsResponse>> GetCourseStatistics(Guid id)
    {
        var query = new GetCourseStatisticsQuery { CourseId = id };
        var response = await _mediator.Send(query);
        
        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Updates an existing course (Lecturer only - own courses)
    /// </summary>
    /// <param name="command">The course update details</param>
    /// <returns>The updated course</returns>
    /// <response code="200">Course updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - can only update own courses</response>
    /// <response code="404">Course not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(UpdateCourseResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UpdateCourseResponse>> UpdateCourse([FromBody] UpdateCourseCommand command)
    {        
        var response = await _mediator.Send(command);
        
        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets course information for joining (public/authenticated)
    /// </summary>
    /// <param name="id">The course ID</param>
    /// <returns>Course join information including enrollment status if authenticated</returns>
    /// <response code="200">Course join information retrieved successfully</response>
    /// <response code="404">Course not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}/join-info")]
    [AllowAnonymous] // Allow both authenticated and anonymous access
    [ProducesResponseType(typeof(GetCourseJoinInfoResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetCourseJoinInfoResponse>> GetCourseJoinInfo(Guid id)
    {
        var query = new GetCourseJoinInfoQuery 
        { 
            CourseId = id,
            UserId = _currentUserService.UserId // Will be null if not authenticated
        };
        
        var response = await _mediator.Send(query);
        
        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Generate or update course access code (Lecturer only)
    /// </summary>
    /// <param name="id">The course ID</param>
    /// <param name="request">Access code update request</param>
    /// <returns>Updated access code information</returns>
    /// <response code="200">Access code updated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - can only update own courses</response>
    /// <response code="404">Course not found</response>
    [HttpPost("{id}/access-code")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(UpdateCourseAccessCodeResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateCourseAccessCodeResponse>> UpdateAccessCode(
        Guid id, 
        [FromBody] UpdateCourseAccessCodeRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = Messages.Error.UserIdNotFound });
        }

        var command = new UpdateCourseAccessCodeCommand
        {
            CourseId = id,
            LecturerId = _currentUserService.UserId.Value,
            RequiresAccessCode = request.RequiresAccessCode,
            AccessCodeType = request.AccessCodeType,
            CustomAccessCode = request.CustomAccessCode,
            ExpiresAt = request.ExpiresAt,
            RegenerateCode = request.RegenerateCode
        };

        var response = await _mediator.Send(command);
        
        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Get course access code (Lecturer only)
    /// </summary>
    /// <param name="id">The course ID</param>
    /// <returns>Access code information</returns>
    /// <response code="200">Access code retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - can only view own courses</response>
    /// <response code="404">Course not found</response>
    [HttpGet("{id}/access-code")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(GetCourseAccessCodeResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCourseAccessCodeResponse>> GetAccessCode(Guid id)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = Messages.Error.UserIdNotFound });
        }

        var query = new GetCourseAccessCodeQuery 
        { 
            CourseId = id,
            LecturerId = _currentUserService.UserId.Value
        };
        
        var response = await _mediator.Send(query);
        
        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Inactivates a course (Lecturer only - own courses)
    /// </summary>
    /// <param name="id">The course ID</param>
    /// <param name="command">Inactivation details</param>
    /// <returns>Inactivation confirmation</returns>
    /// <response code="200">Course inactivated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - can only inactivate own courses</response>
    /// <response code="404">Course not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id}/inactivate")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(InactivateCourseResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InactivateCourseResponse>> InactivateCourse(
        Guid id,
        [FromBody] InactivateCourseCommand command)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new { message = Messages.Error.UserIdNotFound });
        }

        command.CourseId = id;
        command.LecturerId = _currentUserService.UserId.Value;

        var response = await _mediator.Send(command);
        
        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Request model for updating course access code
    /// </summary>
    public class UpdateCourseAccessCodeRequest
    {
        /// <summary>
        /// Whether the course requires an access code
        /// </summary>
        public bool RequiresAccessCode { get; set; }

        /// <summary>
        /// Type of access code to generate
        /// </summary>
        public ClassroomService.Domain.Enums.AccessCodeType? AccessCodeType { get; set; }

        /// <summary>
        /// Custom access code (only used if AccessCodeType is Custom)
        /// </summary>
        public string? CustomAccessCode { get; set; }

        /// <summary>
        /// When the access code expires
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Whether to regenerate the current code
        /// </summary>
        public bool RegenerateCode { get; set; } = false;
    }

    /// <summary>
    /// Gets available courses for students to browse and join (public access)
    /// </summary>
    /// <param name="name">Search by course name (partial match)</param>
    /// <param name="courseCode">Search by course code (partial match)</param>
    /// <param name="lecturerName">Filter by lecturer name (partial match)</param>
    /// <param name="createdAfter">Filter courses created after this date</param>
    /// <param name="createdBefore">Filter courses created before this date</param>
    /// <param name="minEnrollmentCount">Minimum number of enrolled students</param>
    /// <param name="maxEnrollmentCount">Maximum number of enrolled students</param>
    /// <param name="page">Page number for pagination (1-based)</param>
    /// <param name="pageSize">Number of items per page (max 100)</param>
    /// <param name="sortBy">Sort field (Name, CourseCode, CreatedAt, EnrollmentCount)</param>
    /// <param name="sortDirection">Sort direction (asc or desc)</param>
    /// <returns>Filtered and paginated list of available courses for students</returns>
    /// <response code="200">Available courses retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("available")]
    [AllowAnonymous] // Allow both authenticated and anonymous access
    [ProducesResponseType(typeof(GetAvailableCoursesResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetAvailableCoursesResponse>> GetAvailableCourses(
        [FromQuery] string? name = null,
        [FromQuery] string? courseCode = null,
        [FromQuery] string? lecturerName = null,
        [FromQuery] DateTime? createdAfter = null,
        [FromQuery] DateTime? createdBefore = null,
        [FromQuery] int? minEnrollmentCount = null,
        [FromQuery] int? maxEnrollmentCount = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "desc")
    {
        var query = new GetAvailableCoursesQuery 
        { 
            Filter = new CourseFilterDto
            {
                Name = name,
                CourseCode = courseCode,
                LecturerName = lecturerName,
                CreatedAfter = createdAfter,
                CreatedBefore = createdBefore,
                MinEnrollmentCount = minEnrollmentCount,
                MaxEnrollmentCount = maxEnrollmentCount,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDirection = sortDirection
            },
            UserId = _currentUserService.UserId // Will be null if not authenticated
        };
        
        var response = await _mediator.Send(query);
        
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets courses by term with filtering and pagination
    /// </summary>
    /// <param name="termId">Term ID to filter courses</param>
    /// <param name="status">Optional: Filter by course status (Active, PendingApproval, Rejected, Inactive)</param>
    /// <param name="lecturerId">Optional: Filter by lecturer ID</param>
    /// <param name="courseCode">Optional: Search by course code (partial match)</param>
    /// <param name="page">Page number for pagination (1-based)</param>
    /// <param name="pageSize">Number of items per page (max 100)</param>
    /// <param name="sortBy">Sort field (Name, CourseCode, EnrollmentCount, CreatedAt)</param>
    /// <param name="sortDirection">Sort direction (asc or desc)</param>
    /// <returns>Filtered and paginated list of courses for the specified term</returns>
    /// <response code="200">Courses retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="404">Term not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("by-term-year")]
    [AllowAnonymous] // Allow both authenticated and anonymous access
    [ProducesResponseType(typeof(GetCoursesByTermAndYearResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpStatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetCoursesByTermAndYearResponse>> GetCoursesByTermAndYear(
        [FromQuery] Guid termId,
        [FromQuery] CourseStatus? status = null,
        [FromQuery] Guid? lecturerId = null,
        [FromQuery] string? courseCode = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "Name",
        [FromQuery] string sortDirection = "asc")
    {
        var query = new GetCoursesByTermAndYearQuery
        {
            TermId = termId,
            Status = status,
            LecturerId = lecturerId,
            CourseCode = courseCode,
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy ?? "Name",
            SortDirection = sortDirection,
            CurrentUserId = _currentUserService.UserId,
            CurrentUserRole = _currentUserService.Role
        };

        var response = await _mediator.Send(query);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Uploads an image for a course (Lecturer only)
    /// </summary>
    /// <param name="courseId">The course ID</param>
    /// <param name="image">The image file (JPG, PNG, GIF, WEBP - max 5MB)</param>
    /// <returns>The upload response with image URL</returns>
    /// <response code="200">Image uploaded successfully</response>
    /// <response code="400">Invalid file or request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Must be course lecturer</response>
    /// <response code="404">Course not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{courseId}/upload-image")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(UploadCourseImageResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)] // 50MB
    [RequestSizeLimit(52428800)] // 50MB
    public async Task<ActionResult<UploadCourseImageResponse>> UploadCourseImage(
        Guid courseId,
        IFormFile image)
    {
        var command = new UploadCourseImageCommand
        {
            CourseId = courseId,
            Image = image
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("permission") || response.Message.Contains("lecturer"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Deletes the image for a course (Lecturer only)
    /// </summary>
    /// <param name="courseId">The course ID</param>
    /// <returns>The deletion response</returns>
    /// <response code="200">Image deleted successfully</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Must be course lecturer</response>
    /// <response code="404">Course not found or no image to delete</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{courseId}/image")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(DeleteCourseImageResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeleteCourseImageResponse>> DeleteCourseImage(
        [FromRoute] Guid courseId)
    {
        var command = new DeleteCourseImageCommand
        {
            CourseId = courseId
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("not found") || response.Message.Contains("does not have"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("permission") || response.Message.Contains("lecturer"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Uploads a syllabus file for a course (Lecturer only)
    /// </summary>
    /// <param name="courseId">The course ID</param>
    /// <param name="file">The syllabus file (PDF, DOCX, PPTX, ZIP - max 50MB)</param>
    /// <returns>The upload response with file URL</returns>
    /// <response code="200">File uploaded successfully</response>
    /// <response code="400">Invalid file or request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Must be course lecturer</response>
    /// <response code="404">Course not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{courseId}/syllabus/upload")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(UploadCourseSyllabusResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)] // 50MB
    [RequestSizeLimit(52428800)] // 50MB
    public async Task<ActionResult<UploadCourseSyllabusResponse>> UploadCourseSyllabus(
        Guid courseId,
        IFormFile file)
    {
        var command = new UploadCourseSyllabusCommand
        {
            CourseId = courseId,
            File = file
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("permission") || response.Message.Contains("lecturer"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Deletes the syllabus file for a course (Lecturer only)
    /// </summary>
    /// <param name="courseId">The course ID</param>
    /// <returns>The deletion response</returns>
    /// <response code="200">File deleted successfully</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Must be course lecturer</response>
    /// <response code="404">Course not found or no syllabus file to delete</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{courseId}/syllabus")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    [ProducesResponseType(typeof(DeleteCourseSyllabusResponse), HttpStatusCodes.Status200OK)]
    [ProducesResponseType(HttpStatusCodes.Status400BadRequest)]
    [ProducesResponseType(HttpStatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpStatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeleteCourseSyllabusResponse>> DeleteCourseSyllabus(
        [FromRoute] Guid courseId)
    {
        var command = new DeleteCourseSyllabusCommand
        {
            CourseId = courseId
        };

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            if (response.Message.Contains("not found") || response.Message.Contains("does not have"))
            {
                return NotFound(response);
            }
            if (response.Message.Contains("permission") || response.Message.Contains("lecturer"))
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }
}