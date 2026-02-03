using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Reports.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Services;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class SubmitReportCommandHandler : IRequestHandler<SubmitReportCommand, SubmitReportResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ReportHistoryService _historyService;

    public SubmitReportCommandHandler(
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

    public async Task<SubmitReportResponse> Handle(SubmitReportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new SubmitReportResponse
                {
                    Success = false,
                    Message = "User not authenticated",
                    ReportId = null,
                    Report = null
                };
            }

            var userId = currentUserId.Value;

            // Validate assignment exists
            var assignment = await _unitOfWork.Assignments.GetAsync(a => a.Id == request.AssignmentId, cancellationToken);
            if (assignment == null)
            {
                return new SubmitReportResponse
                {
                    Success = false,
                    Message = "Assignment not found",
                    ReportId = null,
                    Report = null
                };
            }

            // Allow creating draft reports in Scheduled, Active, and Extended statuses
            // Students can prepare their work early when assignment is scheduled
            if (assignment.Status != AssignmentStatus.Scheduled && 
                assignment.Status != AssignmentStatus.Active && 
                assignment.Status != AssignmentStatus.Extended)
            {
                return new SubmitReportResponse
                {
                    Success = false,
                    Message = "Assignment is not accepting draft submissions",
                    ReportId = null,
                    Report = null
                };
            }

            // Check if submission type matches assignment type
            if (assignment.IsGroupAssignment != request.IsGroupSubmission)
            {
                return new SubmitReportResponse
                {
                    Success = false,
                    Message = assignment.IsGroupAssignment 
                        ? "This is a group assignment. Please submit as a group." 
                        : "This is an individual assignment. Group submissions are not allowed.",
                    ReportId = null,
                    Report = null
                };
            }

            // For group submissions, validate group and check if any member can create draft
            if (request.IsGroupSubmission)
            {
                if (!request.GroupId.HasValue)
                {
                    return new SubmitReportResponse
                    {
                        Success = false,
                        Message = "Group ID is required for group submissions",
                        ReportId = null,
                        Report = null
                    };
                }

                var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(request.GroupId.Value, cancellationToken);
                if (group == null)
                {
                    return new SubmitReportResponse
                    {
                        Success = false,
                        Message = "Group not found",
                        ReportId = null,
                        Report = null
                    };
                }

                // Verify current user is a member of this group (any member can create draft)
                // Use the StudentId convenience property which accesses Enrollment.StudentId
                var isMember = group.Members.Any(m => m.StudentId == userId);

                if (!isMember)
                {
                    return new SubmitReportResponse
                    {
                        Success = false,
                        Message = "Only group members can create draft reports for the group",
                        ReportId = null,
                        Report = null
                    };
                }

                // Check for existing draft or submitted report
                var existingSubmission = await _unitOfWork.Reports.GetGroupSubmissionAsync(
                    request.AssignmentId, request.GroupId.Value, cancellationToken);
                
                // Allow creating new report if existing one is rejected
                if (existingSubmission != null && existingSubmission.Status != ReportStatus.Rejected)
                {
                    return new SubmitReportResponse
                    {
                        Success = false,
                        Message = $"This group already has a report for this assignment (Status: {existingSubmission.Status}). Please edit the existing report instead.",
                        ReportId = null,
                        Report = null
                    };
                }
            }
            else
            {
                // For individual submissions, check for existing report
                var existingSubmission = await _unitOfWork.Reports.GetStudentSubmissionAsync(
                    request.AssignmentId, userId, cancellationToken);
                
                // Allow creating new report if existing one is rejected
                if (existingSubmission != null && existingSubmission.Status != ReportStatus.Rejected)
                {
                    return new SubmitReportResponse
                    {
                        Success = false,
                        Message = $"You already have a report for this assignment (Status: {existingSubmission.Status}). Please edit the existing report instead.",
                        ReportId = null,
                        Report = null
                    };
                }
            }

            // Validate submission content
            if (string.IsNullOrWhiteSpace(request.Submission))
            {
                return new SubmitReportResponse
                {
                    Success = false,
                    Message = "Submission content cannot be empty",
                    ReportId = null,
                    Report = null
                };
            }

            // Create report as Draft status - user must explicitly submit later
            var now = DateTime.UtcNow;

            // Create report
            var report = new Report
            {
                Id = Guid.NewGuid(),
                AssignmentId = request.AssignmentId,
                GroupId = request.GroupId,
                SubmittedBy = userId,
                SubmittedAt = null, // Not yet submitted
                Submission = request.Submission,
                Status = ReportStatus.Draft, // Always create as Draft
                IsGroupSubmission = request.IsGroupSubmission,
                Version = 1,
                CreatedAt = now,
                CreatedBy = userId
            };

            await _unitOfWork.Reports.AddAsync(report, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Track report creation in history
            await _historyService.TrackCreationAsync(
                report.Id,
                userId.ToString(),
                request.Submission,
                null,
                cancellationToken);
            
            // Save the history record
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Fetch contributor information and update history with name
            var contributorInfo = await _userService.GetUserByIdAsync(userId, cancellationToken);
            var contributorName = contributorInfo?.FullName ?? "Unknown";
            
            // Get all history records and find the most recent one
            var allHistory = await _unitOfWork.ReportHistory.GetReportHistoryAsync(report.Id, cancellationToken);
            var historyRecord = allHistory.OrderByDescending(h => h.ChangedAt).FirstOrDefault();
            
            if (historyRecord != null)
            {
                historyRecord.Comment = string.IsNullOrEmpty(historyRecord.Comment)
                    ? $"Contributors: {contributorName}"
                    : $"{historyRecord.Comment} | Contributors: {contributorName}";
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Load assignment for response
            var submittedReport = await _unitOfWork.Reports.GetReportWithDetailsAsync(report.Id, cancellationToken);
            
            var reportDto = submittedReport != null ? new ReportDto
            {
                Id = submittedReport.Id,
                AssignmentId = submittedReport.AssignmentId,
                AssignmentTitle = submittedReport.Assignment?.Title ?? string.Empty,
                GroupId = submittedReport.GroupId,
                GroupName = submittedReport.Group?.Name,
                SubmittedBy = submittedReport.SubmittedBy,
                SubmittedAt = submittedReport.SubmittedAt,
                Status = submittedReport.Status,
                Grade = submittedReport.Grade,
                Feedback = submittedReport.Feedback,
                GradedBy = submittedReport.GradedBy,
                GradedAt = submittedReport.GradedAt,
                IsGroupSubmission = submittedReport.IsGroupSubmission,
                Version = submittedReport.Version,
                CreatedAt = submittedReport.CreatedAt,
                UpdatedAt = submittedReport.UpdatedAt
            } : null;

            return new SubmitReportResponse
            {
                Success = true,
                Message = "Report draft created successfully. Use the submit endpoint to submit it for review.",
                ReportId = report.Id,
                Report = reportDto
            };
        }
        catch (Exception ex)
        {
            return new SubmitReportResponse
            {
                Success = false,
                Message = $"An error occurred while submitting the report: {ex.Message}",
                ReportId = null,
                Report = null
            };
        }
    }
}
