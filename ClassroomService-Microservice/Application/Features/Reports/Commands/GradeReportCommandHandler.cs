using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Services;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class GradeReportCommandHandler : IRequestHandler<GradeReportCommand, GradeReportResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ReportHistoryService _historyService;

    public GradeReportCommandHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        IKafkaUserService userService,
        ReportHistoryService historyService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
        _historyService = historyService;
    }

    public async Task<GradeReportResponse> Handle(GradeReportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            
            // Get report with assignment details
            var report = await _unitOfWork.Reports.GetReportWithDetailsAsync(request.ReportId, cancellationToken);
            if (report == null)
            {
                return new GradeReportResponse
                {
                    Success = false,
                    Message = "Report not found"
                };
            }

            // Validate report status - only allow grading Submitted, Resubmitted, or Late reports
            if (report.Status != ReportStatus.Submitted && 
                report.Status != ReportStatus.Resubmitted &&
                report.Status != ReportStatus.Late)
            {
                return new GradeReportResponse
                {
                    Success = false,
                    Message = $"Cannot grade report with status: {report.Status}. Only submitted, resubmitted, or late reports can be graded."
                };
            }

            // Validate grade against assignment max points
            if (report.Assignment?.MaxPoints.HasValue == true)
            {
                if (request.Grade < 0 || request.Grade > report.Assignment.MaxPoints.Value)
                {
                    return new GradeReportResponse
                    {
                        Success = false,
                        Message = $"Grade must be between 0 and {report.Assignment.MaxPoints.Value}"
                    };
                }
            }
            else if (request.Grade < 0)
            {
                return new GradeReportResponse
                {
                    Success = false,
                    Message = "Grade must be a positive number"
                };
            }

            // Capture old values before update
            var oldGrade = report.Grade;
            var oldFeedback = report.Feedback;
            var oldStatus = report.Status.ToString();

            // Update report
            report.Grade = request.Grade;
            report.Feedback = request.Feedback;
            report.GradedBy = currentUserId;
            report.GradedAt = DateTime.UtcNow;
            report.Status = ReportStatus.Graded;
            report.UpdatedAt = DateTime.UtcNow;
            report.LastModifiedBy = currentUserId;
            report.LastModifiedAt = DateTime.UtcNow;

            // Get lecturer name and student IDs for event
            var lecturer = await _userService.GetUserByIdAsync(currentUserId!.Value, cancellationToken);
            var lecturerName = lecturer?.FullName ?? "Unknown Lecturer";

            // Get student IDs based on submission type
            var studentIds = new List<Guid>();
            if (report.IsGroupSubmission && report.GroupId.HasValue)
            {
                var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(report.GroupId.Value, cancellationToken);
                if (group != null)
                {
                    studentIds = group.Members.Select(m => m.StudentId).ToList();
                }
            }
            else
            {
                studentIds.Add(report.SubmittedBy);
            }

            // Raise domain event
            report.AddDomainEvent(new ReportGradedEvent(
                report.Id,
                report.AssignmentId,
                report.Assignment?.Title ?? "Unknown Assignment",
                report.Assignment?.CourseId ?? Guid.Empty,
                request.Grade,
                report.Assignment?.MaxPoints,
                request.Feedback,
                currentUserId.Value,
                lecturerName,
                studentIds,
                report.IsGroupSubmission,
                report.GroupId));

            await _unitOfWork.Reports.UpdateAsync(report, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Track grading in history
            if (currentUserId.HasValue)
            {
                await _historyService.TrackGradingAsync(
                    report.Id,
                    currentUserId.Value.ToString(),
                    report.Version,
                    oldGrade,
                    request.Grade,
                    oldFeedback,
                    request.Feedback,
                    oldStatus,
                    cancellationToken);
                
                // Save the history record
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                // Fetch contributor information and update history with name
                var contributorInfo = await _userService.GetUserByIdAsync(currentUserId.Value, cancellationToken);
                var contributorName = contributorInfo?.FullName ?? "Unknown";
                
                var historyRecord = await _unitOfWork.ReportHistory.GetVersionAsync(report.Id, report.Version, cancellationToken);
                if (historyRecord != null)
                {
                    historyRecord.Comment = $"Graded by lecturer | Contributors: {contributorName}";
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            // Fetch contributor information for response
            var contributorInfo2 = currentUserId.HasValue ? await _userService.GetUserByIdAsync(currentUserId.Value, cancellationToken) : null;
            var currentUserRole = _currentUserService.Role;

            return new GradeReportResponse
            {
                Success = true,
                Message = "Report graded successfully",
                ContributorId = currentUserId,
                ContributorName = contributorInfo2?.FullName ?? "Unknown",
                ContributorRole = currentUserRole ?? "Lecturer"
            };
        }
        catch (Exception ex)
        {
            return new GradeReportResponse
            {
                Success = false,
                Message = $"An error occurred while grading the report: {ex.Message}"
            };
        }
    }
}
