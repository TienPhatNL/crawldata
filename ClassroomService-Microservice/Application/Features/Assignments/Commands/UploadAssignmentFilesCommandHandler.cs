using ClassroomService.Application.Common.Helpers;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Assignments.Commands;

/// <summary>
/// Handler for uploading multiple file attachments to assignments
/// </summary>
public class UploadAssignmentFilesCommandHandler : IRequestHandler<UploadAssignmentFilesCommand, UploadAssignmentFilesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<UploadAssignmentFilesCommandHandler> _logger;

    public UploadAssignmentFilesCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ILogger<UploadAssignmentFilesCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<UploadAssignmentFilesResponse> Handle(UploadAssignmentFilesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user is authenticated
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new UploadAssignmentFilesResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            var userId = currentUserId.Value;

            // Validate at least one file
            if (request.Files == null || !request.Files.Any())
            {
                return new UploadAssignmentFilesResponse
                {
                    Success = false,
                    Message = "No files provided for upload"
                };
            }

            // Get assignment with course
            var assignment = await _unitOfWork.Assignments.GetAsync(
                a => a.Id == request.AssignmentId,
                cancellationToken,
                a => a.Course);

            if (assignment == null)
            {
                return new UploadAssignmentFilesResponse
                {
                    Success = false,
                    Message = "Assignment not found"
                };
            }

            // Check if user is the lecturer of the course
            if (assignment.Course.LecturerId != userId)
            {
                return new UploadAssignmentFilesResponse
                {
                    Success = false,
                    Message = "Only the course lecturer can upload assignment attachments"
                };
            }

            // Deserialize existing attachments to check count
            var existingAttachments = AssignmentAttachmentsMetadata.FromJson(assignment.Attachments);
            var existingCount = existingAttachments.Files.Count;
            var newCount = request.Files.Count();
            var totalCount = existingCount + newCount;

            // Validate total file count (max 10 files)
            if (totalCount > 10)
            {
                return new UploadAssignmentFilesResponse
                {
                    Success = false,
                    Message = $"Cannot upload {newCount} file(s). Assignment already has {existingCount} attachment(s). Maximum 10 files allowed."
                };
            }

            // Validate all files first
            foreach (var file in request.Files)
            {
                if (!FileValidationHelper.ValidateAssignmentFile(file, out var validationError))
                {
                    return new UploadAssignmentFilesResponse
                    {
                        Success = false,
                        Message = $"File '{file.FileName}': {validationError}"
                    };
                }
            }

            var uploadedFiles = new List<AttachmentMetadata>();

            // Upload each file
            foreach (var file in request.Files)
            {
                try
                {
                    var fileUrl = await _uploadService.UploadFileAsync(file);
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    
                    var attachment = new AttachmentMetadata
                    {
                        Id = Guid.NewGuid(),
                        FileName = file.FileName,
                        FileUrl = fileUrl,
                        FileSize = file.Length,
                        ContentType = FileValidationHelper.GetContentType(extension),
                        UploadedAt = DateTime.UtcNow,
                        UploadedBy = userId
                    };

                    existingAttachments.AddAttachment(attachment);
                    uploadedFiles.Add(attachment);

                    _logger.LogInformation(
                        "Uploaded attachment {FileName} to Assignment {AssignmentId} by User {UserId}",
                        file.FileName,
                        request.AssignmentId,
                        userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload file {FileName}", file.FileName);
                    
                    // Clean up successfully uploaded files if any upload fails
                    foreach (var uploaded in uploadedFiles)
                    {
                        try
                        {
                            await _uploadService.DeleteFileAsync(uploaded.FileUrl);
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogWarning(deleteEx, "Failed to clean up uploaded file {FileUrl}", uploaded.FileUrl);
                        }
                    }
                    
                    return new UploadAssignmentFilesResponse
                    {
                        Success = false,
                        Message = $"Failed to upload file '{file.FileName}': {ex.Message}"
                    };
                }
            }

            // Save updated attachments JSON
            assignment.Attachments = existingAttachments.ToJson();
            await _unitOfWork.Assignments.UpdateAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UploadAssignmentFilesResponse
            {
                Success = true,
                Message = $"Successfully uploaded {uploadedFiles.Count} file(s)",
                UploadedFiles = uploadedFiles,
                UploadedCount = uploadedFiles.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading assignment files for Assignment {AssignmentId}", request.AssignmentId);
            return new UploadAssignmentFilesResponse
            {
                Success = false,
                Message = $"An error occurred while uploading files: {ex.Message}"
            };
        }
    }
}
