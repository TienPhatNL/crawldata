using MediatR;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Constants;

namespace ClassroomService.Application.Features.Courses.Queries;

public class GetCourseAccessCodeQueryHandler : IRequestHandler<GetCourseAccessCodeQuery, GetCourseAccessCodeResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessCodeService _accessCodeService;

    public GetCourseAccessCodeQueryHandler(IUnitOfWork unitOfWork, IAccessCodeService accessCodeService)
    {
        _unitOfWork = unitOfWork;
        _accessCodeService = accessCodeService;
    }

    public async Task<GetCourseAccessCodeResponse> Handle(GetCourseAccessCodeQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var course = await _unitOfWork.Courses
                .GetAsync(c => c.Id == request.CourseId, cancellationToken);

            if (course == null)
            {
                return new GetCourseAccessCodeResponse
                {
                    Success = false,
                    Message = Messages.Error.CourseNotFound
                };
            }

            // Verify lecturer ownership
            if (course.LecturerId != request.LecturerId)
            {
                return new GetCourseAccessCodeResponse
                {
                    Success = false,
                    Message = Messages.Error.OnlyOwnCourseAccess
                };
            }

            return new GetCourseAccessCodeResponse
            {
                Success = true,
                Message = Messages.Success.AccessCodeRetrieved,
                RequiresAccessCode = course.RequiresAccessCode,
                AccessCode = course.AccessCode,
                AccessCodeCreatedAt = course.AccessCodeCreatedAt,
                AccessCodeExpiresAt = course.AccessCodeExpiresAt,
                IsExpired = _accessCodeService.IsAccessCodeExpired(course),
                FailedAttempts = course.AccessCodeAttempts
            };
        }
        catch (Exception ex)
        {
            return new GetCourseAccessCodeResponse
            {
                Success = false,
                Message = Messages.Helpers.FormatError(Messages.Error.AccessCodeRetrievalFailed, ex.Message)
            };
        }
    }
}