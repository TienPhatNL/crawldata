using ClassroomService.Application.Features.Courses.Queries;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Application.Features.Courses.Commands;

public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CreateCourseResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly IAccessCodeService _accessCodeService;
    private readonly ICourseNameGenerationService _courseNameGenerationService;
    private readonly ICourseUniqueCodeService _courseUniqueCodeService;
    private readonly ILogger<CreateCourseCommandHandler> _logger;

    public CreateCourseCommandHandler(
        IUnitOfWork unitOfWork, 
        IKafkaUserService userService, 
        IAccessCodeService accessCodeService,
        ICourseNameGenerationService courseNameGenerationService,
        ICourseUniqueCodeService courseUniqueCodeService,
        ILogger<CreateCourseCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _accessCodeService = accessCodeService;
        _courseNameGenerationService = courseNameGenerationService;
        _courseUniqueCodeService = courseUniqueCodeService;
        _logger = logger;
    }

    public async Task<CreateCourseResponse> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(Messages.Logging.CourseCreating, 
                request.CourseCodeId, request.Description, request.LecturerId);

            // Validate term exists and is active
            var term = await _unitOfWork.Terms.GetAsync(
                predicate: t => t.Id == request.TermId && t.IsActive,
                cancellationToken: cancellationToken);

            if (term == null)
            {
                _logger.LogWarning("Term with ID {TermId} not found or inactive", request.TermId);
                return new CreateCourseResponse
                {
                    Success = false,
                    Message = "Term not found or inactive",
                    CourseId = null,
                    Course = null
                };
            }

            // Get course code to validate it exists and is active
            var courseCode = await _unitOfWork.CourseCodes.GetAsync(
                predicate: cc => cc.Id == request.CourseCodeId && cc.IsActive,
                cancellationToken: cancellationToken);

            if (courseCode == null)
            {
                _logger.LogWarning("CourseCode with ID {CourseCodeId} not found or inactive", request.CourseCodeId);
                return new CreateCourseResponse
                {
                    Success = false,
                    Message = Messages.Error.CourseCodeNotFound,
                    CourseId = null,
                    Course = null
                };
            }

            // Generate unique code for the course
            var uniqueCode = await _courseUniqueCodeService.GenerateUniqueCodeAsync(cancellationToken);
            _logger.LogInformation("Generated unique code {UniqueCode} for new course", uniqueCode);

            // Validate that the lecturer exists
            _logger.LogInformation(Messages.Logging.ValidatingLecturer, request.LecturerId);
            var lecturer = await _userService.GetUserByIdAsync(request.LecturerId, cancellationToken);
            
            if (lecturer == null)
            {
                _logger.LogWarning(Messages.Logging.LecturerNotFound, request.LecturerId);
                return new CreateCourseResponse
                {
                    Success = false,
                    Message = Messages.Error.LecturerNotFound,
                    CourseId = null,
                    Course = null
                };
            }

            _logger.LogInformation(Messages.Logging.LecturerFound, 
                lecturer.FirstName, lecturer.LastName, lecturer.Role);

            if (lecturer.Role != RoleConstants.Lecturer)
            {
                _logger.LogWarning(Messages.Logging.InvalidRole, 
                    request.LecturerId, lecturer.Role, RoleConstants.Lecturer);
                return new CreateCourseResponse
                {
                    Success = false,
                    Message = Messages.Error.InvalidLecturerRole,
                    CourseId = null,
                    Course = null
                };
            }

            // Generate course name with unique code
            var courseName = await _courseNameGenerationService.GenerateCourseNameAsync(
                request.CourseCodeId, uniqueCode, request.LecturerId, cancellationToken);

            // Handle access code logic only if required
            string? accessCode = null;
            DateTime? accessCodeCreatedAt = null;
            DateTime? accessCodeExpiresAt = null;

            if (request.RequiresAccessCode)
            {
                if (request.AccessCodeType == AccessCodeType.Custom)
                {
                    if (string.IsNullOrWhiteSpace(request.CustomAccessCode))
                    {
                        return new CreateCourseResponse
                        {
                            Success = false,
                            Message = Messages.Error.CustomAccessCodeRequired,
                            CourseId = null,
                            Course = null
                        };
                    }

                    if (!_accessCodeService.IsValidAccessCodeFormat(request.CustomAccessCode, AccessCodeType.Custom))
                    {
                        return new CreateCourseResponse
                        {
                            Success = false,
                            Message = Messages.Error.InvalidAccessCodeFormat,
                            CourseId = null,
                            Course = null
                        };
                    }

                    accessCode = request.CustomAccessCode;
                }
                else
                {
                    accessCode = _accessCodeService.GenerateAccessCode(request.AccessCodeType);
                }

                accessCodeCreatedAt = DateTime.UtcNow;
                accessCodeExpiresAt = request.AccessCodeExpiresAt;

                _logger.LogInformation("Generating access code for course {CourseCodeId}", request.CourseCodeId);
            }
            else
            {
                _logger.LogInformation("Course {CourseCodeId} will not require an access code", request.CourseCodeId);
            }

            var course = new Course
            {
                Id = Guid.NewGuid(),
                CourseCodeId = request.CourseCodeId,
                UniqueCode = uniqueCode,
                Name = courseName,
                Description = request.Description,
                TermId = request.TermId,
                LecturerId = request.LecturerId,
                Status = CourseStatus.PendingApproval,
                RequiresAccessCode = request.RequiresAccessCode,
                AccessCode = accessCode,
                AccessCodeCreatedAt = accessCodeCreatedAt,
                AccessCodeExpiresAt = accessCodeExpiresAt,
                AccessCodeAttempts = 0,
                LastAccessCodeAttempt = null,
                Announcement = request.Announcement,
                CreatedAt = DateTime.UtcNow
            };

            course.AddDomainEvent(new CourseCreatedEvent(
                course.Id, 
                courseCode.Code, 
                course.Name, 
                course.LecturerId));

            await _unitOfWork.Courses.AddAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(Messages.Logging.CourseCreated, course.Id);

            // Construct lecturer name as LastName FirstName
            var lecturerName = !string.IsNullOrEmpty(lecturer.LastName) && !string.IsNullOrEmpty(lecturer.FirstName)
                ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                : lecturer.FullName ?? "Unknown Lecturer";

            _logger.LogInformation("Lecturer name constructed as: {LecturerName}", lecturerName);

            var courseDto = new CourseDto
            {
                Id = course.Id,
                CourseCode = courseCode.Code,
                CourseCodeTitle = courseCode.Title,
                Name = course.Name,
                Description = course.Description,
                Term = term.Name,
                LecturerId = course.LecturerId,
                LecturerName = lecturerName,
                CreatedAt = course.CreatedAt,
                EnrollmentCount = 0, // New course has no enrollments
                RequiresAccessCode = course.RequiresAccessCode,
                AccessCode = course.AccessCode, // Include access code for lecturer
                AccessCodeCreatedAt = course.AccessCodeCreatedAt,
                AccessCodeExpiresAt = course.AccessCodeExpiresAt,
                IsAccessCodeExpired = course.AccessCodeExpiresAt.HasValue && DateTime.UtcNow > course.AccessCodeExpiresAt,
                Announcement = course.Announcement,
                SyllabusFile = course.SyllabusFile,
                Department = courseCode.Department
            };

            return new CreateCourseResponse
            {
                Success = true,
                Message = "Course created successfully and is pending staff approval",
                CourseId = course.Id,
                Course = courseDto
            };
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Courses_CourseCodeId_TermId_Year_LecturerId") == true)
        {
            _logger.LogError(ex, "Course section already exists for CourseCodeId {CourseCodeId}", request.CourseCodeId);
            return new CreateCourseResponse
            {
                Success = false,
                Message = "A course section with the same details already exists",
                CourseId = null,
                Course = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course with CourseCodeId {CourseCodeId}: {ErrorMessage}", request.CourseCodeId, ex.Message);
            return new CreateCourseResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.CourseCreationFailed, ex.Message),
                CourseId = null,
                Course = null
            };
        }
    }
}