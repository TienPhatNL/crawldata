using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Application.Features.Courses.Queries;

namespace ClassroomService.Application.Features.Courses.Commands
{
    public class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, UpdateCourseResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IKafkaUserService _userService;
        private readonly ICourseNameGenerationService _courseNameGenerationService;

        public UpdateCourseCommandHandler(
            IUnitOfWork unitOfWork, 
            IKafkaUserService userService,
            ICourseNameGenerationService courseNameGenerationService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
            _courseNameGenerationService = courseNameGenerationService;
        }

        public async Task<UpdateCourseResponse> Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate course exists
                var course = await _unitOfWork.Courses.GetCourseWithDetailsAsync(request.CourseId, cancellationToken);

                if (course == null)
                {
                    return new UpdateCourseResponse
                    {
                        Success = false,
                        Message = "Course not found",
                        UpdatedCourse = null
                    };
                }

                // Update description if provided
                if (!string.IsNullOrEmpty(request.Description))
                {
                    course.Description = request.Description;
                }

                // Update announcement if provided
                if (request.Announcement != null)
                {
                    course.Announcement = request.Announcement;
                }

                // Update term if provided (validation ensures Active courses cannot change term)
                if (request.TermId.HasValue && request.TermId.Value != course.TermId)
                {
                    // Validate new term exists and is active
                    var newTerm = await _unitOfWork.Terms.GetAsync(
                        t => t.Id == request.TermId.Value && t.IsActive,
                        cancellationToken);
                    
                    if (newTerm == null)
                    {
                        return new UpdateCourseResponse
                        {
                            Success = false,
                            Message = "Invalid or inactive term",
                            UpdatedCourse = null
                        };
                    }

                    course.TermId = request.TermId.Value;
                }

                course.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Courses.UpdateAsync(course, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Reload course with navigation properties for response
                course = await _unitOfWork.Courses.GetCourseWithDetailsAsync(request.CourseId, cancellationToken);

                // Get lecturer information for response
                var lecturer = await _userService.GetUserByIdAsync(course.LecturerId, cancellationToken);
                var lecturerName = lecturer != null 
                    ? $"{lecturer.LastName} {lecturer.FirstName}".Trim()
                    : "Unknown Lecturer";

                var courseDto = new CourseDto
                {
                    Id = course.Id,
                    CourseCode = course.CourseCode.Code,
                    CourseCodeTitle = course.CourseCode.Title,
                    Name = course.Name,
                    Description = course.Description,
                    Term = course.Term.Name,
                    LecturerId = course.LecturerId,
                    LecturerName = lecturerName,
                    CreatedAt = course.CreatedAt,
                    EnrollmentCount = course.Enrollments.Count,
                    RequiresAccessCode = course.RequiresAccessCode,
                    Announcement = course.Announcement,
                    SyllabusFile = course.SyllabusFile,
                    Department = course.CourseCode.Department
                };

                return new UpdateCourseResponse
                {
                    Success = true,
                    Message = "Course updated successfully",
                    UpdatedCourse = courseDto
                };
            }
            catch (Exception ex)
            {
                return new UpdateCourseResponse
                {
                    Success = false,
                    Message = $"Error updating course: {ex.Message}",
                    UpdatedCourse = null
                };
            }
        }
    }
}
