using MediatR;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.DTOs;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class DeleteAssignmentCommandHandler : IRequestHandler<DeleteAssignmentCommand, DeleteAssignmentResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUploadService _uploadService;
    private readonly ILogger<DeleteAssignmentCommandHandler> _logger;
    private readonly IUserInfoCacheService _cacheService;

    public DeleteAssignmentCommandHandler(
        IUnitOfWork unitOfWork,
        IUploadService uploadService,
        ILogger<DeleteAssignmentCommandHandler> logger,
        IUserInfoCacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _uploadService = uploadService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<DeleteAssignmentResponse> Handle(DeleteAssignmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await _unitOfWork.Assignments.GetAssignmentWithGroupsAsync(request.AssignmentId, cancellationToken);

            if (assignment == null)
            {
                return new DeleteAssignmentResponse
                {
                    Success = false,
                    Message = "Assignment not found",
                    GroupsUnassigned = 0
                };
            }

            // Check if any reports have been graded for this assignment
            var gradedReports = await _unitOfWork.Reports
                .GetManyAsync(r => r.AssignmentId == assignment.Id && r.Status == ReportStatus.Graded, cancellationToken);
            
            if (gradedReports.Any())
            {
                return new DeleteAssignmentResponse
                {
                    Success = false,
                    Message = "Cannot delete assignment with graded reports. Graded reports are permanently locked.",
                    GroupsUnassigned = 0
                };
            }

            // Only allow deletion if assignment is in Draft status
            if (assignment.Status != AssignmentStatus.Draft)
            {
                return new DeleteAssignmentResponse
                {
                    Success = false,
                    Message = "Can only delete assignments in Draft status",
                    GroupsUnassigned = 0
                };
            }

            // Delete attachment files from S3 if exists
            if (!string.IsNullOrEmpty(assignment.Attachments))
            {
                try
                {
                    var attachments = AssignmentAttachmentsMetadata.FromJson(assignment.Attachments);
                    foreach (var attachment in attachments.Files)
                    {
                        try
                        {
                            await _uploadService.DeleteFileAsync(attachment.FileUrl);
                            _logger.LogInformation("Deleted attachment {FileName} from assignment {AssignmentId}",
                                attachment.FileName, assignment.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to delete attachment {FileName} from S3: {FileUrl}",
                                attachment.FileName, attachment.FileUrl);
                            // Continue with other deletions even if one fails
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse attachments JSON for assignment {AssignmentId}", assignment.Id);
                    // Continue with deletion even if attachment parsing fails
                }
            }

            // Unassign all groups first
            var groupsToUnassign = assignment.AssignedGroups.ToList();
            var groupsCount = groupsToUnassign.Count;
            
            foreach (var group in groupsToUnassign)
            {
                group.AssignmentId = null;
            }

            // Get course to get lecturer ID
            var course = assignment.Course ?? await _unitOfWork.Courses
                .GetAsync(c => c.Id == assignment.CourseId, cancellationToken);

            // Add domain event before deletion
            assignment.AddDomainEvent(new AssignmentDeletedEvent(
                assignment.Id,
                assignment.CourseId,
                assignment.Title,
                groupsCount,
                course?.LecturerId ?? Guid.Empty));

            // Delete the assignment
            await _unitOfWork.Assignments.DeleteAsync(assignment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Invalidate assignment cache
            await _cacheService.InvalidateAssignmentAsync(assignment.Id, cancellationToken);

            return new DeleteAssignmentResponse
            {
                Success = true,
                Message = $"Assignment deleted successfully. {groupsCount} group(s) unassigned.",
                GroupsUnassigned = groupsCount
            };
        }
        catch (Exception ex)
        {
            return new DeleteAssignmentResponse
            {
                Success = false,
                Message = $"Error deleting assignment: {ex.Message}",
                GroupsUnassigned = 0
            };
        }
    }
}
