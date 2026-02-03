using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Assignments.Commands;

/// <summary>
/// Handler for deleting a specific file attachment from an assignment
/// </summary>
public class DeleteAssignmentFileCommandHandler : IRequestHandler<DeleteAssignmentFileCommand, DeleteAssignmentFileResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<DeleteAssignmentFileCommandHandler> _logger;

    public DeleteAssignmentFileCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ILogger<DeleteAssignmentFileCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<DeleteAssignmentFileResponse> Handle(DeleteAssignmentFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user is authenticated
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new DeleteAssignmentFileResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            var userId = currentUserId.Value;

            // Get assignment with course
            var assignment = await _unitOfWork.Assignments.GetAsync(
                a => a.Id == request.AssignmentId,
                cancellationToken,
                a => a.Course);

            if (assignment == null)
            {
                return new DeleteAssignmentFileResponse
                {
                    Success = false,
                    Message = "Assignment not found"
                };
            }

            // Check if user is the lecturer of the course
            if (assignment.Course.LecturerId != userId)
            {
                return new DeleteAssignmentFileResponse
                {
                    Success = false,
                    Message = "Only the course lecturer can delete assignment attachments"
                };
            }

            // Deserialize existing attachments
            var attachments = AssignmentAttachmentsMetadata.FromJson(assignment.Attachments);
            
            // Find the attachment to delete
            var attachment = attachments.GetAttachment(request.FileId);
            if (attachment == null)
            {
                return new DeleteAssignmentFileResponse
                {
                    Success = false,
                    Message = "Attachment not found"
                };
            }

            // Delete the file from storage
            try
            {
                await _uploadService.DeleteFileAsync(attachment.FileUrl);
                _logger.LogInformation(
                    "Deleted attachment {FileName} (ID: {FileId}) from Assignment {AssignmentId}",
                    attachment.FileName,
                    request.FileId,
                    request.AssignmentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file from storage: {FileUrl}", attachment.FileUrl);
                // Continue with removing from metadata even if storage deletion fails
            }

            // Remove from attachments metadata
            attachments.RemoveAttachment(request.FileId);

            // Save updated attachments JSON (or null if no attachments remain)
            assignment.Attachments = attachments.Files.Any() ? attachments.ToJson() : null;
            await _unitOfWork.Assignments.UpdateAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteAssignmentFileResponse
            {
                Success = true,
                Message = $"Successfully deleted attachment '{attachment.FileName}'"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assignment file {FileId} from Assignment {AssignmentId}", 
                request.FileId, request.AssignmentId);
            return new DeleteAssignmentFileResponse
            {
                Success = false,
                Message = $"An error occurred while deleting the file: {ex.Message}"
            };
        }
    }
}
