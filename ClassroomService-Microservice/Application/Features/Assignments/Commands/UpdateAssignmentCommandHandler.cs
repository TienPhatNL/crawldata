using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Application.Features.Assignments.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Events;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class UpdateAssignmentCommandHandler : IRequestHandler<UpdateAssignmentCommand, UpdateAssignmentResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserInfoCacheService _cacheService;

    public UpdateAssignmentCommandHandler(IUnitOfWork unitOfWork, IUserInfoCacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<UpdateAssignmentResponse> Handle(UpdateAssignmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await _unitOfWork.Assignments
                .GetAsync(
                    a => a.Id == request.AssignmentId, 
                    cancellationToken,
                    a => a.Course,
                    a => a.AssignedGroups);

            if (assignment == null)
            {
                return new UpdateAssignmentResponse
                {
                    Success = false,
                    Message = "Assignment not found",
                    Assignment = null
                };
            }

            // Check if any reports have been graded for this assignment
            var gradedReports = await _unitOfWork.Reports
                .GetManyAsync(r => r.AssignmentId == assignment.Id && r.Status == ReportStatus.Graded, cancellationToken);
            
            if (gradedReports.Any())
            {
                return new UpdateAssignmentResponse
                {
                    Success = false,
                    Message = "Cannot update assignment with graded reports. Graded reports are locked and cannot be modified.",
                    Assignment = null
                };
            }

            // Cannot update Closed assignments
            if (assignment.Status == AssignmentStatus.Closed)
            {
                return new UpdateAssignmentResponse
                {
                    Success = false,
                    Message = "Cannot update assignment in Closed status",
                    Assignment = null
                };
            }

            // For Active, Extended, Overdue statuses, prevent updating StartDate and DueDate
            if (assignment.Status == AssignmentStatus.Active || 
                assignment.Status == AssignmentStatus.Extended || 
                assignment.Status == AssignmentStatus.Overdue)
            {
                // Only check if dates are provided AND different
                bool isUpdatingStartDate = request.StartDate.HasValue && request.StartDate != assignment.StartDate;
                bool isUpdatingDueDate = request.DueDate != default(DateTime) && request.DueDate != assignment.DueDate;

                if (isUpdatingStartDate || isUpdatingDueDate)
                {
                    return new UpdateAssignmentResponse
                    {
                        Success = false,
                        Message = $"Cannot update StartDate or DueDate for assignments in {assignment.Status} status. Use ExtendDueDate endpoint to extend the due date.",
                        Assignment = null
                    };
                }
            }

            // Update fields
            assignment.Title = request.Title;
            assignment.Description = request.Description ?? string.Empty;
            assignment.Format = request.Format ?? string.Empty;
            assignment.MaxPoints = request.MaxPoints;
            
            // Only update dates if in Draft status
            if (assignment.Status == AssignmentStatus.Draft)
            {
                assignment.StartDate = request.StartDate;
                // Only update DueDate if a valid date is provided
                if (request.DueDate != default(DateTime))
                {
                    assignment.DueDate = request.DueDate;
                }
            }
            
            assignment.UpdatedAt = DateTime.UtcNow;

            // Re-determine status based on StartDate (only for Draft)
            if (assignment.Status == AssignmentStatus.Draft)
            {
                if (!assignment.StartDate.HasValue || assignment.StartDate.Value <= DateTime.UtcNow)
                {
                    assignment.Status = AssignmentStatus.Active;
                }
                else
                {
                    assignment.Status = AssignmentStatus.Draft;
                }
            }

            // Get course to get lecturer ID
            var course = assignment.Course ?? await _unitOfWork.Courses
                .GetAsync(c => c.Id == assignment.CourseId, cancellationToken);

            // Add domain event
            assignment.AddDomainEvent(new AssignmentUpdatedEvent(
                assignment.Id,
                assignment.CourseId,
                assignment.Title,
                assignment.UpdatedAt.Value,
                course?.LecturerId ?? Guid.Empty));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var effectiveDueDate = assignment.ExtendedDueDate ?? assignment.DueDate;
            var daysUntilDue = (int)(effectiveDueDate - DateTime.UtcNow).TotalDays;

            // Parse attachments
            var attachments = AssignmentAttachmentsMetadata.FromJson(assignment.Attachments);

            var assignmentDto = new AssignmentDetailDto
            {
                Id = assignment.Id,
                CourseId = assignment.CourseId,
                CourseName = assignment.Course?.Name ?? "Unknown Course",
                Title = assignment.Title,
                Description = assignment.Description ?? string.Empty,
                StartDate = assignment.StartDate,
                DueDate = assignment.DueDate,
                ExtendedDueDate = assignment.ExtendedDueDate,
                Format = assignment.Format ?? string.Empty,
                Status = assignment.Status,
                StatusDisplay = assignment.Status.ToString(),
                IsGroupAssignment = assignment.IsGroupAssignment,
                MaxPoints = assignment.MaxPoints,
                IsOverdue = effectiveDueDate < DateTime.UtcNow,
                DaysUntilDue = daysUntilDue,
                AssignedGroupsCount = assignment.AssignedGroups?.Count ?? 0,
                Attachments = attachments.Files.Any() ? attachments.Files : null,
                CreatedAt = assignment.CreatedAt,
                UpdatedAt = assignment.UpdatedAt,
                AssignedGroups = assignment.AssignedGroups?.Select(g => new GroupDto
                {
                    Id = g.Id,
                    CourseId = g.CourseId,
                    Name = g.Name,
                    Description = g.Description ?? string.Empty,
                    MaxMembers = g.MaxMembers,
                    IsLocked = g.IsLocked,
                    AssignmentId = g.AssignmentId,
                    MemberCount = g.Members?.Count ?? 0,
                    CreatedAt = g.CreatedAt
                }).ToList() ?? new List<GroupDto>()
            };

            // Invalidate assignment cache so next crawl gets fresh data
            await _cacheService.InvalidateAssignmentAsync(assignment.Id, cancellationToken);

            return new UpdateAssignmentResponse
            {
                Success = true,
                Message = "Assignment updated successfully",
                Assignment = assignmentDto
            };
        }
        catch (Exception ex)
        {
            return new UpdateAssignmentResponse
            {
                Success = false,
                Message = $"Error updating assignment: {ex.Message}",
                Assignment = null
            };
        }
    }
}
