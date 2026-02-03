using ClassroomService.Application.Features.CourseRequests.DTOs;
using ClassroomService.Application.Features.Courses.Queries;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

public class ProcessCourseRequestCommandHandler : IRequestHandler<ProcessCourseRequestCommand, ProcessCourseRequestResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ICourseNameGenerationService _courseNameGenerationService;
    private readonly IAccessCodeService _accessCodeService;
    private readonly ICourseUniqueCodeService _courseUniqueCodeService;
    private readonly ILogger<ProcessCourseRequestCommandHandler> _logger;

    public ProcessCourseRequestCommandHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ICourseNameGenerationService courseNameGenerationService,
        IAccessCodeService accessCodeService,
        ICourseUniqueCodeService courseUniqueCodeService,
        ILogger<ProcessCourseRequestCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _courseNameGenerationService = courseNameGenerationService;
        _accessCodeService = accessCodeService;
        _courseUniqueCodeService = courseUniqueCodeService;
        _logger = logger;
    }

    public async Task<ProcessCourseRequestResponse> Handle(ProcessCourseRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing course request {CourseRequestId} with status {Status} by staff {ProcessedBy}",
                request.CourseRequestId, request.Status, request.ProcessedBy);

            // Validate staff member
            var staffMember = await _userService.GetUserByIdAsync(request.ProcessedBy, cancellationToken);
            if (staffMember == null || (staffMember.Role != RoleConstants.Staff && staffMember.Role != RoleConstants.Admin))
            {
                _logger.LogWarning("User {ProcessedBy} is not authorized to process course requests", request.ProcessedBy);
                return new ProcessCourseRequestResponse
                {
                    Success = false,
                    Message = "You are not authorized to process course requests",
                    CourseRequest = null,
                    CreatedCourse = null
                };
            }

            // Get the course request
            var courseRequest = await _unitOfWork.CourseRequests.GetByIdAsync(request.CourseRequestId, cancellationToken);

            if (courseRequest == null)
            {
                _logger.LogWarning("Course request {CourseRequestId} not found", request.CourseRequestId);
                return new ProcessCourseRequestResponse
                {
                    Success = false,
                    Message = "Course request not found",
                    CourseRequest = null,
                    CreatedCourse = null
                };
            }

            // Check if request is already processed
            if (courseRequest.Status != CourseRequestStatus.Pending)
            {
                _logger.LogWarning("Course request {CourseRequestId} is already processed with status {Status}",
                    request.CourseRequestId, courseRequest.Status);
                return new ProcessCourseRequestResponse
                {
                    Success = false,
                    Message = $"Course request is already {courseRequest.Status.ToString().ToLower()}",
                    CourseRequest = null,
                    CreatedCourse = null
                };
            }

            // Validate status
            if (request.Status != CourseRequestStatus.Approved && request.Status != CourseRequestStatus.Rejected)
            {
                return new ProcessCourseRequestResponse
                {
                    Success = false,
                    Message = "Invalid status. Only Approved or Rejected are allowed",
                    CourseRequest = null,
                    CreatedCourse = null
                };
            }

            // Update the course request
            courseRequest.Status = request.Status;
            courseRequest.ProcessedBy = request.ProcessedBy;
            courseRequest.ProcessedAt = DateTime.UtcNow;
            courseRequest.ProcessingComments = request.ProcessingComments;

            // Get staff user info for domain events
            var staffUser = await _userService.GetUserByIdAsync(request.ProcessedBy, cancellationToken);
            var staffName = staffUser != null && !string.IsNullOrEmpty(staffUser.LastName) && !string.IsNullOrEmpty(staffUser.FirstName)
                ? $"{staffUser.LastName} {staffUser.FirstName}".Trim()
                : staffUser?.FullName ?? "Unknown Staff";

            CourseDto? createdCourseDto = null;

            // If approved, create the course
            if (request.Status == CourseRequestStatus.Approved)
            {
                // Generate unique code for the course
                var uniqueCode = await _courseUniqueCodeService.GenerateUniqueCodeAsync(cancellationToken);

                // Generate course name
                var courseName = await _courseNameGenerationService.GenerateCourseNameAsync(
                    courseRequest.CourseCodeId, uniqueCode, courseRequest.LecturerId, cancellationToken);

                // Create the course (without access code - lecturer will add it later)
                var course = new Course
                {
                    Id = Guid.NewGuid(),
                    CourseCodeId = courseRequest.CourseCodeId,
                    Name = courseName,
                    UniqueCode = uniqueCode,
                    Description = courseRequest.Description,
                    TermId = courseRequest.TermId,
                    LecturerId = courseRequest.LecturerId,
                    Status = CourseStatus.Active,
                    ApprovedBy = request.ProcessedBy,
                    ApprovedAt = DateTime.UtcNow,
                    ApprovalComments = request.ProcessingComments,
                    RequiresAccessCode = false, // Staff cannot set access codes - lecturer will configure later
                    AccessCode = null,
                    AccessCodeCreatedAt = null,
                    AccessCodeExpiresAt = null,
                AccessCodeAttempts = 0,
                LastAccessCodeAttempt = null,
                Announcement = courseRequest.Announcement,
                CreatedAt = DateTime.UtcNow
            };                course.AddDomainEvent(new CourseCreatedEvent(
                    course.Id,
                    courseRequest.CourseCode.Code,
                    course.Name,
                    course.LecturerId));

                await _unitOfWork.Courses.AddAsync(course);
                courseRequest.CreatedCourseId = course.Id;

                // Raise domain event for course request approval
                courseRequest.AddDomainEvent(new Domain.Events.CourseRequestApprovedEvent(
                    courseRequest.Id,
                    course.Id,
                    course.Name,
                    courseRequest.CourseCode.Code,
                    courseRequest.LecturerId,
                    request.ProcessedBy,
                    staffName,
                    request.ProcessingComments));

                // Save changes to get the course ID persisted
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Course created with ID {CourseId} from request {CourseRequestId}",
                    course.Id, courseRequest.Id);

                // Fetch the created course with all navigation properties for proper DTO building
                var createdCourse = await _unitOfWork.Courses.GetCourseWithDetailsAsync(course.Id, cancellationToken);

                if (createdCourse != null)
                {
                    // Get lecturer info for DTO
                    var lecturer = await _userService.GetUserByIdAsync(courseRequest.LecturerId, cancellationToken);
                    var lecturerName = lecturer != null && !string.IsNullOrEmpty(lecturer.LastName) && !string.IsNullOrEmpty(lecturer.FirstName)
                        ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                        : lecturer?.FullName ?? "Unknown Lecturer";

                    // Use CourseDtoBuilder to create proper course DTO with all fields populated
                    createdCourseDto = CourseDtoBuilder.BuildCourseDto(
                        course: createdCourse,
                        lecturerName: lecturerName,
                        enrollmentCount: 0, // New course has no enrollments yet
                        currentUserId: request.ProcessedBy, // Staff member processing the request
                        currentUserRole: RoleConstants.Staff,
                        accessCodeService: _accessCodeService,
                        showFullAccessCodeInfo: false // Staff should not see access codes
                    );
                }
            }
            else
            {
                // Raise domain event for course request rejection
                courseRequest.AddDomainEvent(new Domain.Events.CourseRequestRejectedEvent(
                    courseRequest.Id,
                    courseRequest.LecturerId,
                    request.ProcessedBy,
                    staffName,
                    courseRequest.CourseCode.Code,
                    courseRequest.CourseCode.Title,
                    request.ProcessingComments));

                // Save changes for rejected requests
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Build response DTO
            var requestLecturer = await _userService.GetUserByIdAsync(courseRequest.LecturerId, cancellationToken);
            var requestLecturerName = requestLecturer != null && !string.IsNullOrEmpty(requestLecturer.LastName) && !string.IsNullOrEmpty(requestLecturer.FirstName)
                ? $"{requestLecturer.LastName} {requestLecturer.FirstName}".Trim()
                : requestLecturer?.FullName ?? "Unknown Lecturer";

            var courseRequestDto = new CourseRequestDto
            {
                Id = courseRequest.Id,
                CourseCodeId = courseRequest.CourseCodeId,
                CourseCode = courseRequest.CourseCode.Code,
                CourseCodeTitle = courseRequest.CourseCode.Title,
                Description = courseRequest.Description,
                Term = courseRequest.Term.Name,
                LecturerId = courseRequest.LecturerId,
                LecturerName = requestLecturerName,
                Status = courseRequest.Status,
                RequestReason = courseRequest.RequestReason,
                ProcessedBy = courseRequest.ProcessedBy,
                ProcessedByName = staffName,
                ProcessedAt = courseRequest.ProcessedAt,
                ProcessingComments = courseRequest.ProcessingComments,
                CreatedCourseId = courseRequest.CreatedCourseId,
                Announcement = courseRequest.Announcement,
                CreatedAt = courseRequest.CreatedAt,
                Department = courseRequest.CourseCode.Department
            };

            _logger.LogInformation("Course request {CourseRequestId} processed successfully with status {Status}",
                courseRequest.Id, request.Status);

            return new ProcessCourseRequestResponse
            {
                Success = true,
                Message = $"Course request {request.Status.ToString().ToLower()} successfully",
                CourseRequest = courseRequestDto,
                CreatedCourse = createdCourseDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing course request {CourseRequestId}: {ErrorMessage}",
                request.CourseRequestId, ex.Message);
            return new ProcessCourseRequestResponse
            {
                Success = false,
                Message = $"Error processing course request: {ex.Message}",
                CourseRequest = null,
                CreatedCourse = null
            };
        }
    }
}