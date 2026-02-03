using ClassroomService.Application.Features.CourseRequests.DTOs;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.Queries;

public class GetCourseRequestQueryHandler : IRequestHandler<GetCourseRequestQuery, GetCourseRequestResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<GetCourseRequestQueryHandler> _logger;

    public GetCourseRequestQueryHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ILogger<GetCourseRequestQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _logger = logger;
    }

    public async Task<GetCourseRequestResponse> Handle(GetCourseRequestQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting course request {CourseRequestId} for user {UserId}",
                request.CourseRequestId, request.CurrentUserId);

            // Get the course request with navigation properties
            var courseRequest = await _unitOfWork.CourseRequests.GetAsync(
                cr => cr.Id == request.CourseRequestId,
                cancellationToken,
                cr => cr.CourseCode,
                cr => cr.Term,
                cr => cr.CreatedCourse);

            if (courseRequest == null)
            {
                _logger.LogWarning("Course request {CourseRequestId} not found", request.CourseRequestId);
                return new GetCourseRequestResponse
                {
                    Success = false,
                    Message = "Course request not found",
                    CourseRequest = null
                };
            }

            // Authorization check: Staff can see all, Lecturers can only see their own
            if (request.CurrentUserRole != RoleConstants.Staff && 
                request.CurrentUserRole != RoleConstants.Admin &&
                courseRequest.LecturerId != request.CurrentUserId)
            {
                _logger.LogWarning("User {UserId} is not authorized to view course request {CourseRequestId}",
                    request.CurrentUserId, request.CourseRequestId);
                return new GetCourseRequestResponse
                {
                    Success = false,
                    Message = "You are not authorized to view this course request",
                    CourseRequest = null
                };
            }

            // Get lecturer info
            var lecturer = await _userService.GetUserByIdAsync(courseRequest.LecturerId, cancellationToken);
            var lecturerName = lecturer != null && !string.IsNullOrEmpty(lecturer.LastName) && !string.IsNullOrEmpty(lecturer.FirstName)
                ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                : lecturer?.FullName ?? "Unknown Lecturer";

            // Get processor info if processed
            string? processedByName = null;
            if (courseRequest.ProcessedBy.HasValue)
            {
                var processor = await _userService.GetUserByIdAsync(courseRequest.ProcessedBy.Value, cancellationToken);
                processedByName = processor != null && !string.IsNullOrEmpty(processor.LastName) && !string.IsNullOrEmpty(processor.FirstName)
                    ? $"{processor.LastName} {processor.FirstName}".Trim()
                    : processor?.FullName ?? "Unknown Staff";
            }

            var courseRequestDto = new CourseRequestDto
            {
                Id = courseRequest.Id,
                CourseCodeId = courseRequest.CourseCodeId,
                CourseCode = courseRequest.CourseCode?.Code ?? "N/A",
                CourseCodeTitle = courseRequest.CourseCode?.Title ?? "N/A",
                Description = courseRequest.Description ?? string.Empty,
                Term = courseRequest.Term?.Name ?? "N/A",
                LecturerId = courseRequest.LecturerId,
                LecturerName = lecturerName,
                Status = courseRequest.Status,
                RequestReason = courseRequest.RequestReason ?? string.Empty,
                ProcessedBy = courseRequest.ProcessedBy,
                ProcessedByName = processedByName,
                ProcessedAt = courseRequest.ProcessedAt,
                ProcessingComments = courseRequest.ProcessingComments ?? string.Empty,
                CreatedCourseId = courseRequest.CreatedCourseId,
                Announcement = courseRequest.Announcement,
                SyllabusFile = courseRequest.SyllabusFile,
                CreatedAt = courseRequest.CreatedAt,
                Department = courseRequest.CourseCode?.Department ?? "N/A"
            };

            return new GetCourseRequestResponse
            {
                Success = true,
                Message = "Course request retrieved successfully",
                CourseRequest = courseRequestDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving course request {CourseRequestId}: {ErrorMessage}",
                request.CourseRequestId, ex.Message);
            return new GetCourseRequestResponse
            {
                Success = false,
                Message = $"Error retrieving course request: {ex.Message}",
                CourseRequest = null
            };
        }
    }
}
