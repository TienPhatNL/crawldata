using MediatR;
using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Application.Features.CourseCodes.DTOs;

namespace ClassroomService.Application.Features.CourseCodes.Commands;

public class UpdateCourseCodeCommandHandler : IRequestHandler<UpdateCourseCodeCommand, UpdateCourseCodeResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICourseNameGenerationService _courseNameGenerationService;
    private readonly ILogger<UpdateCourseCodeCommandHandler> _logger;

    public UpdateCourseCodeCommandHandler(
        IUnitOfWork unitOfWork,
        ICourseNameGenerationService courseNameGenerationService,
        ILogger<UpdateCourseCodeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _courseNameGenerationService = courseNameGenerationService;
        _logger = logger;
    }

    public async Task<UpdateCourseCodeResponse> Handle(UpdateCourseCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating course code with ID: {Id}", request.Id);

            var courseCode = await _unitOfWork.CourseCodes
                .GetAsync(cc => cc.Id == request.Id, cancellationToken);

            if (courseCode == null)
            {
                _logger.LogWarning("Course code with ID {Id} not found", request.Id);
                return new UpdateCourseCodeResponse
                {
                    Success = false,
                    Message = Messages.Error.CourseCodeNotFound,
                    CourseCode = null
                };
            }

            bool codeChanged = false;

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Code) && request.Code != courseCode.Code)
            {
                // Check if new code already exists
                var existingCode = await _unitOfWork.CourseCodes
                    .GetAsync(cc => cc.Code == request.Code && cc.Id != request.Id, cancellationToken);

                if (existingCode != null)
                {
                    return new UpdateCourseCodeResponse
                    {
                        Success = false,
                        Message = Messages.Helpers.FormatCourseCodeExists(request.Code),
                        CourseCode = null
                    };
                }

                courseCode.Code = request.Code.ToUpperInvariant();
                codeChanged = true;
            }

            if (!string.IsNullOrEmpty(request.Title))
            {
                courseCode.Title = request.Title;
            }

            if (request.Description != null)
            {
                courseCode.Description = request.Description;
            }

            if (!string.IsNullOrEmpty(request.Department))
            {
                courseCode.Department = request.Department;
            }

            if (request.IsActive.HasValue)
            {
                courseCode.IsActive = request.IsActive.Value;
            }

            courseCode.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Update course names if code changed
            if (codeChanged && courseCode.Courses.Any())
            {
                _logger.LogInformation("Updating course names for changed course code {Id}", request.Id);
                await _courseNameGenerationService.UpdateCourseNamesForCourseCodeAsync(request.Id, cancellationToken);
            }

            _logger.LogInformation("Course code updated successfully: {Id}", request.Id);

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
                ActiveCoursesCount = courseCode.Courses.Count, // This includes all courses
                TotalCoursesCount = courseCode.Courses.Count
            };

            return new UpdateCourseCodeResponse
            {
                Success = true,
                Message = Messages.Success.CourseCodeUpdated,
                CourseCode = courseCodeDto
            };
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_CourseCodes_Code") == true)
        {
            _logger.LogError(ex, "Duplicate course code error when updating {Id}", request.Id);
            return new UpdateCourseCodeResponse
            {
                Success = false,
                Message = "Course code already exists",
                CourseCode = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course code {Id}: {ErrorMessage}", request.Id, ex.Message);
            return new UpdateCourseCodeResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.CourseCodeUpdateFailed, ex.Message),
                CourseCode = null
            };
        }
    }
}