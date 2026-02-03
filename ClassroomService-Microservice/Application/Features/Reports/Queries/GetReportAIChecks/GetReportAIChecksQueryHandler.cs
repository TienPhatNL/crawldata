using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Reports.Queries;

/// <summary>
/// Handler for GetReportAIChecksQuery
/// </summary>
public class GetReportAIChecksQueryHandler : IRequestHandler<GetReportAIChecksQuery, GetReportAIChecksResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IKafkaUserService _userInfoService;
    private readonly ILogger<GetReportAIChecksQueryHandler> _logger;

    public GetReportAIChecksQueryHandler(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        IKafkaUserService userInfoService,
        ILogger<GetReportAIChecksQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _userInfoService = userInfoService;
        _logger = logger;
    }

    public async Task<GetReportAIChecksResponse> Handle(GetReportAIChecksQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return new GetReportAIChecksResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            var userRole = GetCurrentUserRole();

            // Get report with details
            var report = await _unitOfWork.Reports.GetReportWithDetailsAsync(request.ReportId, cancellationToken);
            if (report == null)
            {
                return new GetReportAIChecksResponse
                {
                    Success = false,
                    Message = "Report not found"
                };
            }

            // Authorization check
            // Students can only view AI checks for their own reports
            // Lecturers can view all reports in their courses
            if (userRole == RoleConstants.Student)
            {
                // Check if this is the student's own report
                if (report.SubmittedBy != currentUserId)
                {
                    // Check if student is in the group that submitted
                    if (report.GroupId.HasValue)
                    {
                        var groupMember = await _unitOfWork.GroupMembers.GetGroupMemberAsync(
                            report.GroupId.Value, currentUserId, cancellationToken);
                        
                        if (groupMember == null)
                        {
                            return new GetReportAIChecksResponse
                            {
                                Success = false,
                                Message = "You do not have permission to view AI checks for this report"
                            };
                        }
                    }
                    else
                    {
                        return new GetReportAIChecksResponse
                        {
                            Success = false,
                            Message = "You do not have permission to view AI checks for this report"
                        };
                    }
                }
            }
            else if (userRole == RoleConstants.Lecturer)
            {
                // Verify lecturer owns this course
                var assignment = report.Assignment;
                var course = await _unitOfWork.Courses.GetByIdAsync(assignment.CourseId, cancellationToken);
                
                if (course == null || course.LecturerId != currentUserId)
                {
                    return new GetReportAIChecksResponse
                    {
                        Success = false,
                        Message = "You do not have access to this course"
                    };
                }
            }

            // Get all AI checks for the report
            var aiChecks = await _unitOfWork.ReportAIChecks.GetChecksByReportIdAsync(request.ReportId, cancellationToken);

            // Build response DTOs
            var checkDtos = new List<AICheckResultDto>();
            
            foreach (var check in aiChecks)
            {
                var lecturerInfo = await _userInfoService.GetUserByIdAsync(check.CheckedBy, cancellationToken);
                var studentInfo = await _userInfoService.GetUserByIdAsync(report.SubmittedBy, cancellationToken);

                checkDtos.Add(new AICheckResultDto
                {
                    Id = check.Id,
                    ReportId = check.ReportId,
                    AIPercentage = check.AIPercentage,
                    Provider = check.Provider,
                    CheckedBy = check.CheckedBy,
                    CheckedByName = lecturerInfo?.FullName ?? lecturerInfo?.Email ?? "Unknown",
                    CheckedAt = check.CheckedAt,
                    Notes = check.Notes,
                    ReportStatus = report.Status,
                    StudentName = studentInfo?.FullName ?? studentInfo?.Email ?? "Unknown",
                    AssignmentTitle = report.Assignment.Title
                });
            }

            return new GetReportAIChecksResponse
            {
                Success = true,
                Message = $"Found {checkDtos.Count} AI check(s)",
                Checks = checkDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving AI checks for report {ReportId}", request.ReportId);
            return new GetReportAIChecksResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
                          ?? _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private string? GetCurrentUserRole()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value
               ?? _httpContextAccessor.HttpContext?.User.FindFirst("role")?.Value;
    }
}
