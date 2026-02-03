using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Reports.Commands;

/// <summary>
/// Handler for CheckReportAICommand
/// </summary>
public class CheckReportAICommandHandler : IRequestHandler<CheckReportAICommand, CheckReportAIResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIDetectionService _aiDetectionService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IKafkaUserService _userInfoService;
    private readonly ILogger<CheckReportAICommandHandler> _logger;

    public CheckReportAICommandHandler(
        IUnitOfWork unitOfWork,
        IAIDetectionService aiDetectionService,
        IHttpContextAccessor httpContextAccessor,
        IKafkaUserService userInfoService,
        ILogger<CheckReportAICommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _aiDetectionService = aiDetectionService;
        _httpContextAccessor = httpContextAccessor;
        _userInfoService = userInfoService;
        _logger = logger;
    }

    public async Task<CheckReportAIResponse> Handle(CheckReportAICommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return new CheckReportAIResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            // Verify user is a lecturer
            var userRole = GetCurrentUserRole();
            if (userRole != RoleConstants.Lecturer)
            {
                return new CheckReportAIResponse
                {
                    Success = false,
                    Message = "Only lecturers can perform AI content checks"
                };
            }

            // Get report with related data
            var report = await _unitOfWork.Reports.GetReportWithDetailsAsync(request.ReportId, cancellationToken);
            if (report == null)
            {
                return new CheckReportAIResponse
                {
                    Success = false,
                    Message = "Report not found"
                };
            }

            // Validate report status - only Submitted or Resubmitted
            if (report.Status != ReportStatus.Submitted && report.Status != ReportStatus.Resubmitted)
            {
                return new CheckReportAIResponse
                {
                    Success = false,
                    Message = $"AI checks can only be performed on submitted reports. Current status: {report.Status}"
                };
            }

            // Validate report has content
            if (string.IsNullOrWhiteSpace(report.Submission))
            {
                return new CheckReportAIResponse
                {
                    Success = false,
                    Message = "Report has no text content to analyze"
                };
            }

            // Get course to verify lecturer access
            var assignment = report.Assignment;
            var course = await _unitOfWork.Courses.GetByIdAsync(assignment.CourseId, cancellationToken);
            if (course == null)
            {
                return new CheckReportAIResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            // Verify lecturer owns this course
            if (course.LecturerId != currentUserId)
            {
                return new CheckReportAIResponse
                {
                    Success = false,
                    Message = "You do not have access to this course"
                };
            }

            _logger.LogInformation("Lecturer {LecturerId} initiating AI check for report {ReportId}", 
                currentUserId, request.ReportId);

            // Perform AI detection
            var (success, aiPercentage, errorMessage, rawResponse) = 
                await _aiDetectionService.CheckContentAsync(report.Submission, cancellationToken);

            if (!success || !aiPercentage.HasValue)
            {
                _logger.LogError("AI detection failed for report {ReportId}: {Error}", 
                    request.ReportId, errorMessage);
                
                return new CheckReportAIResponse
                {
                    Success = false,
                    Message = $"AI detection failed: {errorMessage}"
                };
            }

            // Create AI check record
            var aiCheck = new ReportAICheck
            {
                Id = Guid.NewGuid(),
                ReportId = request.ReportId,
                AIPercentage = aiPercentage.Value,
                Provider = "ZeroGPT",
                RawResponse = rawResponse,
                CheckedBy = currentUserId,
                CheckedAt = DateTime.UtcNow,
                Notes = request.Notes
            };

            await _unitOfWork.ReportAIChecks.AddAsync(aiCheck, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("AI check completed for report {ReportId}: {Percentage}% AI-generated", 
                request.ReportId, aiPercentage.Value);

            // Get user info for response
            var lecturerInfo = await _userInfoService.GetUserByIdAsync(currentUserId, cancellationToken);
            var studentInfo = await _userInfoService.GetUserByIdAsync(report.SubmittedBy, cancellationToken);

            // Build response
            var result = new AICheckResultDto
            {
                Id = aiCheck.Id,
                ReportId = report.Id,
                AIPercentage = aiCheck.AIPercentage,
                Provider = aiCheck.Provider,
                CheckedBy = currentUserId,
                CheckedByName = lecturerInfo?.FullName ?? lecturerInfo?.Email ?? "Unknown",
                CheckedAt = aiCheck.CheckedAt,
                Notes = aiCheck.Notes,
                ReportStatus = report.Status,
                StudentName = studentInfo?.FullName ?? studentInfo?.Email ?? "Unknown",
                AssignmentTitle = assignment.Title
            };

            return new CheckReportAIResponse
            {
                Success = true,
                Message = "AI content check completed successfully",
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing AI check for report {ReportId}", request.ReportId);
            return new CheckReportAIResponse
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
