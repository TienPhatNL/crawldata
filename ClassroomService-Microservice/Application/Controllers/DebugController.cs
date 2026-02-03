using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassroomService.Application.Controllers;

/// <summary>
/// Controller for JWT debugging and testing
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Debug")]
public class DebugController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DebugController> _logger;

    public DebugController(
        ICurrentUserService currentUserService, 
        IUnitOfWork unitOfWork,
        ILogger<DebugController> logger)
    {
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint to check JWT authentication (no authorization required)
    /// </summary>
    [HttpGet("test-auth")]
    [AllowAnonymous]
    public ActionResult<object> TestAuth()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        var hasValidToken = !string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ");
        
        return Ok(new
        {
            HasAuthHeader = !string.IsNullOrEmpty(authHeader),
            HasValidBearerToken = hasValidToken,
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            UserClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
            CurrentUser = new
            {
                UserId = _currentUserService.UserId,
                Email = _currentUserService.Email,
                Role = _currentUserService.Role,
                IsAuthenticated = _currentUserService.IsAuthenticated
            }
        });
    }

    /// <summary>
    /// Test endpoint that requires authentication
    /// </summary>
    [HttpGet("test-auth-required")]
    [Authorize]
    public ActionResult<object> TestAuthRequired()
    {
        return Ok(new
        {
            Message = "Authentication successful!",
            UserId = _currentUserService.UserId,
            Email = _currentUserService.Email,
            Role = _currentUserService.Role,
            Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }

    /// <summary>
    /// Test endpoint that requires Lecturer role
    /// </summary>
    [HttpGet("test-lecturer")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    public ActionResult<object> TestLecturer()
    {
        return Ok(new
        {
            Message = "Lecturer authorization successful!",
            UserId = _currentUserService.UserId,
            Email = _currentUserService.Email,
            Role = _currentUserService.Role
        });
    }

    /// <summary>
    /// Test endpoint that requires Admin or Staff role
    /// </summary>
    [HttpGet("test-admin")]
    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.Staff}")]
    public ActionResult<object> TestAdmin()
    {
        return Ok(new
        {
            Message = "Admin/Staff authorization successful!",
            UserId = _currentUserService.UserId,
            Email = _currentUserService.Email,
            Role = _currentUserService.Role
        });
    }

    /// <summary>
    /// DEBUG ONLY: Force activate a specific assignment (bypasses schedule, sets to Active immediately)
    /// </summary>
    /// <remarks>
    /// **FOR TESTING PURPOSES ONLY**
    /// 
    /// This endpoint allows you to force-activate an assignment regardless of its current status or schedule.
    /// It will change the assignment status to Active and set StartDate to now.
    /// 
    /// Use this for testing assignment functionality without waiting for scheduled activation.
    /// </remarks>
    [HttpPost("activate-assignment/{assignmentId:guid}")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    public async Task<ActionResult<object>> ForceActivateAssignment(Guid assignmentId)
    {
        try
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(assignmentId);
            if (assignment == null)
            {
                return NotFound(new { success = false, message = "Assignment not found" });
            }

            var utcNow = DateTime.UtcNow;
            var seAsiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var nowUtcPlus7 = TimeZoneInfo.ConvertTimeFromUtc(utcNow, seAsiaTimeZone);

            var oldStatus = assignment.Status;

            // Force activate the assignment
            assignment.Status = AssignmentStatus.Active;
            assignment.StartDate = utcNow; // Set start date to now (UTC)
            assignment.UpdatedAt = utcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogWarning(
                "DEBUG: Assignment {AssignmentId} ({Title}) force-activated by user {UserId}. Status changed from {OldStatus} to Active",
                assignmentId, assignment.Title, _currentUserService.UserId, oldStatus);

            return Ok(new
            {
                success = true,
                message = $"Assignment '{assignment.Title}' has been force-activated",
                assignment = new
                {
                    id = assignment.Id,
                    title = assignment.Title,
                    oldStatus = oldStatus.ToString(),
                    newStatus = assignment.Status.ToString(),
                    startDate = assignment.StartDate,
                    dueDate = assignment.DueDate,
                    extendedDueDate = assignment.ExtendedDueDate,
                    currentTimeUtc = utcNow,
                    currentTimeUtcPlus7 = nowUtcPlus7
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error force-activating assignment {AssignmentId}", assignmentId);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// DEBUG ONLY: Manually trigger assignment status update check (same as background service)
    /// </summary>
    /// <remarks>
    /// **FOR TESTING PURPOSES ONLY**
    /// 
    /// This endpoint manually runs the same logic as the background service to update assignment statuses.
    /// It checks all assignments and updates their statuses based on current time (UTC+7).
    /// 
    /// Use this to test the background service logic without waiting for the hourly check.
    /// </remarks>
    [HttpPost("trigger-status-update")]
    [Authorize(Roles = RoleConstants.Lecturer)]
    public async Task<ActionResult<object>> TriggerAssignmentStatusUpdate()
    {
        try
        {
            var utcNow = DateTime.UtcNow;
            var seAsiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var nowUtcPlus7 = TimeZoneInfo.ConvertTimeFromUtc(utcNow, seAsiaTimeZone);

            _logger.LogWarning("DEBUG: Manual assignment status update triggered by user {UserId} at {UtcTime} UTC / {LocalTime} UTC+7",
                _currentUserService.UserId, utcNow, nowUtcPlus7);

            // Get all assignments that are not Closed
            // Note: Graded status is obsolete - using Report.Status = ReportStatus.Graded instead
            var assignments = await _unitOfWork.Assignments.GetManyAsync(
                a => a.Status != AssignmentStatus.Closed);

            if (!assignments.Any())
            {
                return Ok(new
                {
                    success = true,
                    message = "No assignments found to update",
                    currentTimeUtc = utcNow,
                    currentTimeUtcPlus7 = nowUtcPlus7
                });
            }

            var updates = new List<object>();
            var updatedCount = 0;

            foreach (var assignment in assignments)
            {
                var oldStatus = assignment.Status;
                var newStatus = DetermineAssignmentStatus(assignment, nowUtcPlus7);

                if (oldStatus != newStatus)
                {
                    assignment.Status = newStatus;
                    assignment.UpdatedAt = utcNow;
                    updatedCount++;

                    updates.Add(new
                    {
                        id = assignment.Id,
                        title = assignment.Title,
                        oldStatus = oldStatus.ToString(),
                        newStatus = newStatus.ToString(),
                        startDate = assignment.StartDate,
                        dueDate = assignment.DueDate,
                        extendedDueDate = assignment.ExtendedDueDate
                    });

                    _logger.LogInformation(
                        "DEBUG: Updated assignment {AssignmentId} ({Title}) status from {OldStatus} to {NewStatus}",
                        assignment.Id, assignment.Title, oldStatus, newStatus);
                }
            }

            if (updatedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                message = $"Checked {assignments.Count()} assignment(s), updated {updatedCount}",
                currentTimeUtc = utcNow,
                currentTimeUtcPlus7 = nowUtcPlus7,
                totalChecked = assignments.Count(),
                updatedCount,
                updates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in manual assignment status update");
            return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Helper method to determine assignment status (same logic as background service)
    /// </summary>
    private static AssignmentStatus DetermineAssignmentStatus(Domain.Entities.Assignment assignment, DateTime now)
    {
        // Permanent state - never change automatically
        // Note: Graded status is obsolete - using Report.Status = ReportStatus.Graded instead
        if (assignment.Status == AssignmentStatus.Closed)
        {
            return assignment.Status;
        }

        // Draft assignments are NOT auto-activated
        if (assignment.Status == AssignmentStatus.Draft)
        {
            return AssignmentStatus.Draft;
        }

        // Determine effective due date
        var effectiveDueDate = assignment.ExtendedDueDate ?? assignment.DueDate;

        // Check if overdue (past all due dates)
        if (effectiveDueDate < now)
        {
            return AssignmentStatus.Overdue;
        }

        // Check if extended (DueDate passed but ExtendedDueDate not yet)
        if (assignment.ExtendedDueDate.HasValue && assignment.DueDate < now && assignment.ExtendedDueDate.Value >= now)
        {
            return AssignmentStatus.Extended;
        }

        // Scheduled -> Active transition when StartDate arrives
        if (assignment.Status == AssignmentStatus.Scheduled)
        {
            if (!assignment.StartDate.HasValue || assignment.StartDate.Value <= now)
            {
                return AssignmentStatus.Active;
            }
            return AssignmentStatus.Scheduled;
        }

        // For Active assignments, stay active if not past due date
        if (assignment.Status == AssignmentStatus.Active)
        {
            return AssignmentStatus.Active;
        }

        // Default: keep current status
        return assignment.Status;
    }
}