using MediatR;
using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Constants;
using ClassroomService.Application.Features.CourseCodes.DTOs;

namespace ClassroomService.Application.Features.CourseCodes.Commands;

public class CreateCourseCodeCommandHandler : IRequestHandler<CreateCourseCodeCommand, CreateCourseCodeResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateCourseCodeCommandHandler> _logger;

    public CreateCourseCodeCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateCourseCodeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CreateCourseCodeResponse> Handle(CreateCourseCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating course code: {Code} - {Title}", request.Code, request.Title);

            // Check if course code already exists
            var existingCourseCode = await _unitOfWork.CourseCodes
                .GetAsync(cc => cc.Code == request.Code, cancellationToken);

            if (existingCourseCode != null)
            {
                _logger.LogWarning("Course code {Code} already exists", request.Code);
                return new CreateCourseCodeResponse
                {
                    Success = false,
                    Message = Messages.Helpers.FormatCourseCodeExists(request.Code),
                    CourseCodeId = null,
                    CourseCode = null
                };
            }

            var courseCode = new CourseCode
            {
                Id = Guid.NewGuid(),
                Code = request.Code.ToUpperInvariant(), // Standardize to uppercase
                Title = request.Title,
                Description = request.Description,
                Department = request.Department,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.CourseCodes.AddAsync(courseCode);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Course code created successfully with ID: {Id}", courseCode.Id);

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
                ActiveCoursesCount = 0, // New course code has no courses yet
                TotalCoursesCount = 0
            };

            return new CreateCourseCodeResponse
            {
                Success = true,
                Message = Messages.Success.CourseCodeCreated,
                CourseCodeId = courseCode.Id,
                CourseCode = courseCodeDto
            };
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_CourseCodes_Code") == true)
        {
            _logger.LogError(ex, "Duplicate course code error for {Code}", request.Code);
            return new CreateCourseCodeResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatCourseCodeExists(request.Code),
                CourseCodeId = null,
                CourseCode = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course code {Code}: {ErrorMessage}", request.Code, ex.Message);
            return new CreateCourseCodeResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.CourseCodeCreationFailed, ex.Message),
                CourseCodeId = null,
                CourseCode = null
            };
        }
    }
}