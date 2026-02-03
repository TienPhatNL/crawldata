using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Constants;

namespace ClassroomService.Application.Features.CourseCodes.Commands;

public class DeleteCourseCodeCommandHandler : IRequestHandler<DeleteCourseCodeCommand, DeleteCourseCodeResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCourseCodeCommandHandler> _logger;

    public DeleteCourseCodeCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteCourseCodeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DeleteCourseCodeResponse> Handle(DeleteCourseCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting course code with ID: {Id}", request.Id);

            var courseCode = await _unitOfWork.CourseCodes
                .GetAsync(cc => cc.Id == request.Id, cancellationToken);

            if (courseCode == null)
            {
                _logger.LogWarning("Course code with ID {Id} not found", request.Id);
                return new DeleteCourseCodeResponse
                {
                    Success = false,
                    Message = Messages.Error.CourseCodeNotFound
                };
            }

            // Check if the course code is being used by any courses
            if (courseCode.Courses.Any())
            {
                _logger.LogWarning("Cannot delete course code {Code} as it is being used by {Count} courses", 
                    courseCode.Code, courseCode.Courses.Count);
                return new DeleteCourseCodeResponse
                {
                    Success = false,
                    Message = Messages.Helpers.FormatError(Messages.Error.CourseCodeInUse, courseCode.Code)
                };
            }

            await _unitOfWork.CourseCodes.DeleteAsync(courseCode);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Course code deleted successfully: {Code}", courseCode.Code);

            return new DeleteCourseCodeResponse
            {
                Success = true,
                Message = Messages.Success.CourseCodeDeleted
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course code {Id}: {ErrorMessage}", request.Id, ex.Message);
            return new DeleteCourseCodeResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.CourseCodeDeletionFailed, ex.Message)
            };
        }
    }
}