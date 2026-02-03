using MediatR;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Application.Common.Helpers;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

public class CreateSupportRequestCommandHandler : IRequestHandler<CreateSupportRequestCommand, CreateSupportRequestResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<CreateSupportRequestCommandHandler> _logger;

    public CreateSupportRequestCommandHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        IUploadService uploadService,
        ILogger<CreateSupportRequestCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<CreateSupportRequestResponse> Handle(CreateSupportRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate course exists
            var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null)
            {
                return new CreateSupportRequestResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            // Check if user has access to the course
            var hasAccess = false;
            var userRole = string.Empty;
            var userName = string.Empty;

            // Check if user is enrolled (Student)
            var enrollment = await _unitOfWork.CourseEnrollments
                .GetEnrollmentAsync(request.CourseId, request.RequesterId, cancellationToken);
            
            if (enrollment != null)
            {
                hasAccess = true;
                userRole = "Student";
                // Get user info from cache/API
                var userInfo = await _userService.GetUserByIdAsync(request.RequesterId, cancellationToken);
                userName = userInfo?.FullName ?? "Unknown";
            }
            else if (course.LecturerId == request.RequesterId)
            {
                hasAccess = true;
                userRole = "Lecturer";
                var userInfo = await _userService.GetUserByIdAsync(request.RequesterId, cancellationToken);
                userName = userInfo?.FullName ?? "Unknown";
            }

            if (!hasAccess)
            {
                return new CreateSupportRequestResponse
                {
                    Success = false,
                    Message = "You don't have access to this course"
                };
            }

            // Check for existing active support request in this course
            var existingRequest = await _unitOfWork.SupportRequests
                .GetActiveRequestForUserInCourseAsync(request.RequesterId, request.CourseId, cancellationToken);

            if (existingRequest != null)
            {
                return new CreateSupportRequestResponse
                {
                    Success = false,
                    Message = $"You already have an active support request in this course (Status: {existingRequest.Status})"
                };
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(request.Subject) || request.Subject.Length > 200)
            {
                return new CreateSupportRequestResponse
                {
                    Success = false,
                    Message = "Subject is required and must be less than 200 characters"
                };
            }

            if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 2000)
            {
                return new CreateSupportRequestResponse
                {
                    Success = false,
                    Message = "Description is required and must be less than 2000 characters"
                };
            }

            // Validate image count (max 5 images)
            if (request.Images != null && request.Images.Count > 5)
            {
                return new CreateSupportRequestResponse
                {
                    Success = false,
                    Message = "Maximum 5 images allowed per support request"
                };
            }

            // Validate each image file
            if (request.Images != null && request.Images.Any())
            {
                foreach (var image in request.Images)
                {
                    if (!FileValidationHelper.ValidateImageFile(image, out var validationError))
                    {
                        return new CreateSupportRequestResponse
                        {
                            Success = false,
                            Message = $"Image '{image.FileName}': {validationError}"
                        };
                    }
                }
            }

            // Cast priority and category from int to enum
            var priority = (SupportPriority)request.Priority;
            var category = (SupportRequestCategory)request.Category;

            // Upload images if provided
            string? imagesJson = null;
            if (request.Images != null && request.Images.Any())
            {
                var imageUrls = new List<string>();
                foreach (var image in request.Images)
                {
                    try
                    {
                        var imageUrl = await _uploadService.UploadFileAsync(image);
                        imageUrls.Add(imageUrl);
                        _logger.LogInformation("Uploaded image for support request: {Url}", imageUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading image for support request");
                        // Continue with other images
                    }
                }

                if (imageUrls.Any())
                {
                    imagesJson = System.Text.Json.JsonSerializer.Serialize(imageUrls);
                    _logger.LogInformation("Attached {Count} images to support request", imageUrls.Count);
                }
            }

            // Create support request
            var supportRequest = new SupportRequest
            {
                Id = Guid.NewGuid(),
                CourseId = request.CourseId,
                RequesterId = request.RequesterId,
                RequesterName = userName,
                RequesterRole = userRole,
                Status = SupportRequestStatus.Pending,
                Priority = priority,
                Category = category,
                Subject = request.Subject,
                Description = request.Description,
                Images = imagesJson,
                RequestedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.RequesterId
            };

            // Raise domain event (will be auto-dispatched by UnitOfWork.SaveChangesAsync)
            supportRequest.AddDomainEvent(new SupportRequestCreatedEvent(
                supportRequest.Id,
                course.Id,
                course.Name,
                request.RequesterId,
                userName,
                userRole,
                priority,
                category,
                request.Subject
            ));

            await _unitOfWork.SupportRequests.AddAsync(supportRequest, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Support request {RequestId} created by {UserId} for course {CourseId}", 
                supportRequest.Id, request.RequesterId, request.CourseId);

            return new CreateSupportRequestResponse
            {
                Success = true,
                Message = "Support request created successfully. Staff will be notified.",
                SupportRequestId = supportRequest.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating support request");
            return new CreateSupportRequestResponse
            {
                Success = false,
                Message = "An error occurred while creating the support request"
            };
        }
    }
}
