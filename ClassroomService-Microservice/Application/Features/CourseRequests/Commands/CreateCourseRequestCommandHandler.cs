using ClassroomService.Application.Features.CourseRequests.DTOs;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

public class CreateCourseRequestCommandHandler : IRequestHandler<CreateCourseRequestCommand, CreateCourseRequestResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<CreateCourseRequestCommandHandler> _logger;

    public CreateCourseRequestCommandHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ILogger<CreateCourseRequestCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _logger = logger;
    }

    public async Task<CreateCourseRequestResponse> Handle(CreateCourseRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating course request for CourseCodeId {CourseCodeId}, TermId {TermId}, Lecturer {LecturerId}",
                request.CourseCodeId, request.TermId, request.LecturerId);

            // Validate term exists and is active
            var term = await _unitOfWork.Terms
                .GetAsync(t => t.Id == request.TermId && t.IsActive, cancellationToken);

            if (term == null)
            {
                _logger.LogWarning("Term with ID {TermId} not found or inactive", request.TermId);
                return new CreateCourseRequestResponse
                {
                    Success = false,
                    Message = "Term not found or inactive",
                    CourseRequestId = null,
                    CourseRequest = null
                };
            }

            // Validate course code exists and is active
            var courseCode = await _unitOfWork.CourseCodes
                .GetAsync(cc => cc.Id == request.CourseCodeId && cc.IsActive, cancellationToken);

            if (courseCode == null)
            {
                _logger.LogWarning("CourseCode with ID {CourseCodeId} not found or inactive", request.CourseCodeId);
                return new CreateCourseRequestResponse
                {
                    Success = false,
                    Message = Messages.Error.CourseCodeNotFound,
                    CourseRequestId = null,
                    CourseRequest = null
                };
            }

            // Validate lecturer exists and has correct role
            var lecturer = await _userService.GetUserByIdAsync(request.LecturerId, cancellationToken);
            if (lecturer == null)
            {
                _logger.LogWarning("Lecturer with ID {LecturerId} not found", request.LecturerId);
                return new CreateCourseRequestResponse
                {
                    Success = false,
                    Message = Messages.Error.LecturerNotFound,
                    CourseRequestId = null,
                    CourseRequest = null
                };
            }

            if (lecturer.Role != RoleConstants.Lecturer)
            {
                _logger.LogWarning("User {LecturerId} is not a lecturer, role: {Role}", request.LecturerId, lecturer.Role);
                return new CreateCourseRequestResponse
                {
                    Success = false,
                    Message = Messages.Error.InvalidLecturerRole,
                    CourseRequestId = null,
                    CourseRequest = null
                };
            }

            // Check if there's already a pending request for this combination
            var existingRequest = await _unitOfWork.CourseRequests
                .GetAsync(cr => cr.CourseCodeId == request.CourseCodeId 
                    && cr.TermId == request.TermId 
                    && cr.LecturerId == request.LecturerId
                    && cr.Status == CourseRequestStatus.Pending, cancellationToken);

            if (existingRequest != null)
            {
                _logger.LogWarning("Pending course request already exists for CourseCode {CourseCodeId}, Term {TermName}, Lecturer {LecturerId}",
                    request.CourseCodeId, term.Name, request.LecturerId);
                return new CreateCourseRequestResponse
                {
                    Success = false,
                    Message = $"A pending request already exists for {courseCode.Code} in {term.Name}",
                    CourseRequestId = null,
                    CourseRequest = null
                };
            }

            // Create the course request
            var courseRequest = new CourseRequest
            {
                Id = Guid.NewGuid(),
                CourseCodeId = request.CourseCodeId,
                Description = request.Description,
                TermId = request.TermId,
                LecturerId = request.LecturerId,
                Status = CourseRequestStatus.Pending,
                RequestReason = request.RequestReason,
                Announcement = request.Announcement,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.CourseRequests.AddAsync(courseRequest);
            
            // Raise domain event for notifications
            courseRequest.AddDomainEvent(new Domain.Events.CourseRequestCreatedEvent(
                courseRequest.Id,
                courseRequest.LecturerId,
                $"{lecturer.LastName} {lecturer.FirstName}".Trim(),
                courseCode.Code,
                courseCode.Title,
                term.Name,
                term.StartDate.Year,
                courseRequest.RequestReason));
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Course request created with ID {CourseRequestId}", courseRequest.Id);

            // Build response DTO
            var lecturerName = !string.IsNullOrEmpty(lecturer.LastName) && !string.IsNullOrEmpty(lecturer.FirstName)
                ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                : lecturer.FullName ?? "Unknown Lecturer";

            var courseRequestDto = new CourseRequestDto
            {
                Id = courseRequest.Id,
                CourseCodeId = courseRequest.CourseCodeId,
                CourseCode = courseCode.Code,
                CourseCodeTitle = courseCode.Title,
                Description = courseRequest.Description,
                Term = term.Name,
                LecturerId = courseRequest.LecturerId,
                LecturerName = lecturerName,
                Status = courseRequest.Status,
                RequestReason = courseRequest.RequestReason,
                ProcessedBy = courseRequest.ProcessedBy,
                ProcessedByName = null,
                ProcessedAt = courseRequest.ProcessedAt,
                ProcessingComments = courseRequest.ProcessingComments,
                Announcement = courseRequest.Announcement,
                SyllabusFile = courseRequest.SyllabusFile,
                CreatedCourseId = courseRequest.CreatedCourseId,
                CreatedAt = courseRequest.CreatedAt,
                Department = courseCode.Department
            };

            return new CreateCourseRequestResponse
            {
                Success = true,
                Message = "Course request created successfully",
                CourseRequestId = courseRequest.Id,
                CourseRequest = courseRequestDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course request for CourseCodeId {CourseCodeId}: {ErrorMessage}",
                request.CourseCodeId, ex.Message);
            return new CreateCourseRequestResponse
            {
                Success = false,
                Message = $"Error creating course request: {ex.Message}",
                CourseRequestId = null,
                CourseRequest = null
            };
        }
    }
}