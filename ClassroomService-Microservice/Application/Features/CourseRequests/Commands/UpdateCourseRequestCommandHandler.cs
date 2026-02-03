using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

/// <summary>
/// Handler for updating course requests
/// Only the requesting lecturer can update, and only if status is Pending
/// </summary>
public class UpdateCourseRequestCommandHandler : IRequestHandler<UpdateCourseRequestCommand, UpdateCourseRequestResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateCourseRequestCommandHandler> _logger;

    public UpdateCourseRequestCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UpdateCourseRequestCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UpdateCourseRequestResponse> Handle(UpdateCourseRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the course request
            var courseRequest = await _unitOfWork.CourseRequests.GetAsync(
                cr => cr.Id == request.CourseRequestId,
                cancellationToken);

            if (courseRequest == null)
            {
                _logger.LogWarning("Course request not found: {CourseRequestId}", request.CourseRequestId);
                return new UpdateCourseRequestResponse
                {
                    Success = false,
                    Message = "Course request not found"
                };
            }

            // Check if the current user is the owner of the course request
            if (courseRequest.LecturerId != request.LecturerId)
            {
                _logger.LogWarning("Lecturer {LecturerId} attempted to update course request {CourseRequestId} owned by {OwnerId}",
                    request.LecturerId, request.CourseRequestId, courseRequest.LecturerId);
                return new UpdateCourseRequestResponse
                {
                    Success = false,
                    Message = "You are not authorized to update this course request"
                };
            }

            // Check if the course request is in Pending state
            if (courseRequest.Status != CourseRequestStatus.Pending)
            {
                _logger.LogWarning("Cannot update course request {CourseRequestId} - Status is {Status}, must be Pending",
                    request.CourseRequestId, courseRequest.Status);
                return new UpdateCourseRequestResponse
                {
                    Success = false,
                    Message = $"Cannot update course request. Only pending requests can be updated. Current status: {courseRequest.Status}"
                };
            }

            // Update CourseCodeId if provided
            if (request.CourseCodeId.HasValue)
            {
                // Validate course code exists and is active
                var courseCode = await _unitOfWork.CourseCodes
                    .GetAsync(cc => cc.Id == request.CourseCodeId.Value && cc.IsActive, cancellationToken);

                if (courseCode == null)
                {
                    return new UpdateCourseRequestResponse
                    {
                        Success = false,
                        Message = "Invalid course code. The specified course code does not exist or is inactive."
                    };
                }

                // Check for duplicate course request with same CourseCode, Term, and Lecturer
                var duplicateRequest = await _unitOfWork.CourseRequests.GetAsync(
                    cr => cr.CourseCodeId == request.CourseCodeId.Value
                        && cr.TermId == (request.TermId ?? courseRequest.TermId)
                        && cr.LecturerId == request.LecturerId
                        && cr.Id != request.CourseRequestId
                        && cr.Status == CourseRequestStatus.Pending, cancellationToken);

                if (duplicateRequest != null)
                {
                    return new UpdateCourseRequestResponse
                    {
                        Success = false,
                        Message = "A pending course request with the same course code and term already exists."
                    };
                }

                courseRequest.CourseCodeId = request.CourseCodeId.Value;
            }

            // Update TermId if provided
            if (request.TermId.HasValue)
            {
                // Validate term exists and is active
                var term = await _unitOfWork.Terms
                    .GetAsync(t => t.Id == request.TermId.Value && t.IsActive, cancellationToken);

                if (term == null)
                {
                    return new UpdateCourseRequestResponse
                    {
                        Success = false,
                        Message = "Invalid term. The specified term does not exist or is inactive."
                    };
                }

                // Check for duplicate course request with same CourseCode, Term, and Lecturer
                var duplicateRequest = await _unitOfWork.CourseRequests.GetAsync(
                    cr => cr.CourseCodeId == (request.CourseCodeId ?? courseRequest.CourseCodeId)
                        && cr.TermId == request.TermId.Value
                        && cr.LecturerId == request.LecturerId
                        && cr.Id != request.CourseRequestId
                        && cr.Status == CourseRequestStatus.Pending, cancellationToken);

                if (duplicateRequest != null)
                {
                    return new UpdateCourseRequestResponse
                    {
                        Success = false,
                        Message = "A pending course request with the same course code and term already exists."
                    };
                }

                courseRequest.TermId = request.TermId.Value;
            }

            // Update Description if provided
            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                courseRequest.Description = request.Description;
            }

            // Update RequestReason if provided
            if (request.RequestReason != null)
            {
                courseRequest.RequestReason = string.IsNullOrWhiteSpace(request.RequestReason) 
                    ? null 
                    : request.RequestReason;
            }

            // Update Announcement if provided
            if (request.Announcement != null)
            {
                courseRequest.Announcement = string.IsNullOrWhiteSpace(request.Announcement) 
                    ? null 
                    : request.Announcement;
            }

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Course request {CourseRequestId} updated by lecturer {LecturerId}",
                courseRequest.Id, request.LecturerId);

            return new UpdateCourseRequestResponse
            {
                Success = true,
                Message = "Course request updated successfully",
                CourseRequestId = courseRequest.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course request {CourseRequestId}", request.CourseRequestId);
            return new UpdateCourseRequestResponse
            {
                Success = false,
                Message = "An error occurred while updating the course request"
            };
        }
    }
}
