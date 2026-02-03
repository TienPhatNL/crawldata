using MediatR;
using ClassroomService.Infrastructure.Persistence;
using ClassroomService.Domain.Constants;
using ClassroomService.Application.Features.CourseCodes.DTOs;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.CourseCodes.Queries;

public class GetCourseCodeQueryHandler : IRequestHandler<GetCourseCodeQuery, GetCourseCodeResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetCourseCodeQueryHandler> _logger;

    public GetCourseCodeQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetCourseCodeQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetCourseCodeResponse> Handle(GetCourseCodeQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving course code with ID: {Id}", request.Id);

            var courseCode = await _unitOfWork.CourseCodes
                .GetAsync(cc => cc.Id == request.Id, cancellationToken);

            if (courseCode == null)
            {
                _logger.LogWarning("Course code with ID {Id} not found", request.Id);
                return new GetCourseCodeResponse
                {
                    Success = false,
                    Message = Messages.Error.CourseCodeNotFound,
                    CourseCode = null
                };
            }

            var courseCodeDto = new CourseCodeDto
            {
                Id = courseCode.Id,
                Code = courseCode.Code,
                Title = courseCode.Title,
                Description = courseCode.Description,
                Department = courseCode.Department,
                IsActive = courseCode.IsActive,
                CreatedAt = courseCode.CreatedAt,
                UpdatedAt = courseCode.UpdatedAt,
                ActiveCoursesCount = courseCode.Courses.Count, // For now, count all courses
                TotalCoursesCount = courseCode.Courses.Count
            };

            _logger.LogInformation("Course code retrieved successfully: {Id}", request.Id);

            return new GetCourseCodeResponse
            {
                Success = true,
                Message = Messages.Success.CourseCodeRetrieved,
                CourseCode = courseCodeDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving course code {Id}: {ErrorMessage}", request.Id, ex.Message);
            return new GetCourseCodeResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.CourseCodeRetrievalFailed, ex.Message),
                CourseCode = null
            };
        }
    }
}