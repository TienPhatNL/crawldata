using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Services;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class RequestRevisionCommandHandler : IRequestHandler<RequestRevisionCommand, RequestRevisionResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ReportHistoryService _historyService;

    public RequestRevisionCommandHandler(
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

    public async Task<RequestRevisionResponse> Handle(RequestRevisionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _unitOfWork.Reports.GetAsync(r => r.Id == request.ReportId, cancellationToken);
            if (report == null)
            {
                return new RequestRevisionResponse { Success = false, Message = "Report not found" };
            }

            // Only allow requesting revision on Submitted, Resubmitted, or Late reports
            if (report.Status != ReportStatus.Submitted && 
                report.Status != ReportStatus.Resubmitted &&
                report.Status != ReportStatus.Late)
            {
                return new RequestRevisionResponse 
                { 
                    Success = false, 
                    Message = $"Cannot request revision for report with status: {report.Status}. Only submitted, resubmitted, or late reports can have revision requested." 
                };
            }

            var oldStatus = report.Status.ToString();
            
            report.Status = ReportStatus.RequiresRevision;
            report.Feedback = request.Feedback;
            report.UpdatedAt = DateTime.UtcNow;

            // Get lecturer name and student IDs for event
            var currentUserId = _currentUserService.UserId;
            var lecturer = currentUserId.HasValue ? await _userService.GetUserByIdAsync(currentUserId.Value, cancellationToken) : null;
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
            if (currentUserId.HasValue)
            {
                report.AddDomainEvent(new ReportRevisionRequestedEvent(
                    report.Id,
                    report.AssignmentId,
                    report.Assignment?.Title ?? "Unknown Assignment",
                    report.Assignment?.CourseId ?? Guid.Empty,
                    request.Feedback,
                    currentUserId.Value,
                    lecturerName,
                    studentIds,
                    report.IsGroupSubmission,
                    report.GroupId));
            }

            await _unitOfWork.Reports.UpdateAsync(report, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Track revision request in history
            if (currentUserId.HasValue)
            {
                await _historyService.TrackRevisionRequestAsync(
                    report.Id,
                    currentUserId.Value.ToString(),
                    report.Version,
                    request.Feedback ?? string.Empty,
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
                    historyRecord.Comment = $"Revision requested by lecturer | Contributors: {contributorName}";
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            // Fetch contributor information for response
            var contributorInfo2 = currentUserId.HasValue ? await _userService.GetUserByIdAsync(currentUserId.Value, cancellationToken) : null;
            var currentUserRole = _currentUserService.Role;

            return new RequestRevisionResponse 
            { 
                Success = true, 
                Message = "Revision requested successfully",
                ContributorId = currentUserId,
                ContributorName = contributorInfo2?.FullName ?? "Unknown",
                ContributorRole = currentUserRole ?? "Lecturer"
            };
        }
        catch (Exception ex)
        {
            return new RequestRevisionResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }
}
