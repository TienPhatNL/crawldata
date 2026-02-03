using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class ScheduleAssignmentCommandHandler : IRequestHandler<ScheduleAssignmentCommand, ScheduleAssignmentResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ScheduleAssignmentCommandHandler> _logger;

    public ScheduleAssignmentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ScheduleAssignmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ScheduleAssignmentResponse> Handle(ScheduleAssignmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the assignment
            var assignment = await _unitOfWork.Assignments
                .GetAsync(a => a.Id == request.AssignmentId, cancellationToken);

            if (assignment == null)
            {
                return new ScheduleAssignmentResponse
                {
                    Success = false,
                    Message = "Assignment not found"
                };
            }

            // Get course for building response
            var course = await _unitOfWork.Courses
                .GetAsync(c => c.Id == assignment.CourseId, cancellationToken);

            if (request.Schedule)
            {
                // Schedule: Draft -> Scheduled or Active
                
                // Only Draft status can be scheduled
                if (assignment.Status != AssignmentStatus.Draft)
                {
                    return new ScheduleAssignmentResponse
                    {
                        Success = false,
                        Message = $"Only Draft assignments can be scheduled. Current status: {assignment.Status}"
                    };
                }

                // Validate StartDate exists
                if (!assignment.StartDate.HasValue)
                {
                    return new ScheduleAssignmentResponse
                    {
                        Success = false,
                        Message = "Assignment must have a StartDate to be scheduled"
                    };
                }

                // Check if StartDate is in the past
                if (assignment.StartDate.Value < DateTime.UtcNow)
                {
                    return new ScheduleAssignmentResponse
                    {
                        Success = false,
                        Message = "Cannot schedule assignment with StartDate in the past. Please update the StartDate first."
                    };
                }

                // Store old status for event
                var oldStatus = assignment.Status;
                AssignmentStatus newStatus;

                // If StartDate is now or very close (within 1 minute), set to Active
                if (assignment.StartDate.Value <= DateTime.UtcNow.AddMinutes(1))
                {
                    newStatus = AssignmentStatus.Active;
                    assignment.Status = newStatus;
                    _logger.LogInformation("Assignment {AssignmentId} activated immediately as StartDate is now", assignment.Id);
                }
                else
                {
                    newStatus = AssignmentStatus.Scheduled;
                    assignment.Status = newStatus;
                    _logger.LogInformation("Assignment {AssignmentId} scheduled for {StartDate}", 
                        assignment.Id, assignment.StartDate.Value);
                }

                assignment.UpdatedAt = DateTime.UtcNow;

                // Raise domain event for status change (Draft -> Scheduled/Active)
                assignment.AddDomainEvent(new Domain.Events.AssignmentStatusChangedEvent(
                    assignment.Id,
                    assignment.CourseId,
                    assignment.Title,
                    oldStatus,
                    newStatus,
                    course?.LecturerId ?? Guid.Empty,
                    isAutomatic: false));

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var scheduledDto = BuildAssignmentDto(assignment, course?.Name ?? "Unknown Course");

                return new ScheduleAssignmentResponse
                {
                    Success = true,
                    Message = assignment.Status == AssignmentStatus.Active 
                        ? "Assignment activated successfully" 
                        : $"Assignment scheduled successfully for {assignment.StartDate.Value:yyyy-MM-dd HH:mm}",
                    Assignment = scheduledDto
                };
            }
            else
            {
                // Unschedule: Scheduled -> Draft
                
                // Only Scheduled status can be unscheduled
                if (assignment.Status != AssignmentStatus.Scheduled)
                {
                    return new ScheduleAssignmentResponse
                    {
                        Success = false,
                        Message = $"Only Scheduled assignments can be unscheduled. Current status: {assignment.Status}"
                    };
                }

                // Store old status for event
                var oldStatus = assignment.Status;
                assignment.Status = AssignmentStatus.Draft;
                assignment.UpdatedAt = DateTime.UtcNow;

                // Raise domain event for status change (Scheduled -> Draft)
                assignment.AddDomainEvent(new Domain.Events.AssignmentStatusChangedEvent(
                    assignment.Id,
                    assignment.CourseId,
                    assignment.Title,
                    oldStatus,
                    AssignmentStatus.Draft,
                    course?.LecturerId ?? Guid.Empty,
                    isAutomatic: false));

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Assignment {AssignmentId} unscheduled back to Draft", assignment.Id);

                var unscheduledDto = BuildAssignmentDto(assignment, course?.Name ?? "Unknown Course");

                return new ScheduleAssignmentResponse
                {
                    Success = true,
                    Message = "Assignment unscheduled successfully (returned to Draft)",
                    Assignment = unscheduledDto
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling/unscheduling assignment {AssignmentId}", request.AssignmentId);
            return new ScheduleAssignmentResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    private DTOs.AssignmentDetailDto BuildAssignmentDto(Domain.Entities.Assignment assignment, string courseName)
    {
        return new DTOs.AssignmentDetailDto
        {
            Id = assignment.Id,
            CourseId = assignment.CourseId,
            CourseName = courseName,
            Title = assignment.Title,
            Description = assignment.Description,
            StartDate = assignment.StartDate,
            DueDate = assignment.DueDate,
            Format = assignment.Format,
            Status = assignment.Status,
            StatusDisplay = assignment.Status.ToString(),
            IsGroupAssignment = assignment.IsGroupAssignment,
            MaxPoints = assignment.MaxPoints,
            IsOverdue = assignment.DueDate < DateTime.UtcNow,
            DaysUntilDue = (int)(assignment.DueDate - DateTime.UtcNow).TotalDays,
            AssignedGroupsCount = 0,
            CreatedAt = assignment.CreatedAt,
            UpdatedAt = assignment.UpdatedAt
        };
    }
}
