using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Application.Features.Enrollments.Queries;

public class GetEnrolledStudentByIdQueryHandler : IRequestHandler<GetEnrolledStudentByIdQuery, GetEnrolledStudentByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public GetEnrolledStudentByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _currentUserService = currentUserService;
    }

    public async Task<GetEnrolledStudentByIdResponse> Handle(GetEnrolledStudentByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate course exists
            var course = await _unitOfWork.Courses.GetAsync(c => c.Id == request.CourseId, cancellationToken);
            if (course == null)
            {
                return new GetEnrolledStudentByIdResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            // Authorization: Lecturer of the course OR the student themselves
            var currentUserRole = _currentUserService.Role;
            var isLecturer = currentUserRole == RoleConstants.Lecturer;
            var isStudent = request.RequestedBy == request.StudentId;

            if (!isLecturer && !isStudent)
            {
                // If not lecturer, verify they are enrolled in the same course
                var requesterEnrollment = await _unitOfWork.CourseEnrollments.GetEnrollmentAsync(
                    request.CourseId, request.RequestedBy, cancellationToken);

                if (requesterEnrollment == null)
                {
                    return new GetEnrolledStudentByIdResponse
                    {
                        Success = false,
                        Message = "You are not authorized to view this student's details"
                    };
                }
            }

            // Get enrollment with group details
            var enrollment = await _unitOfWork.CourseEnrollments.GetEnrollmentAsync(
                request.CourseId, request.StudentId, cancellationToken);

            var enrollmentData = enrollment;
            if (enrollmentData == null)
            {
                return new GetEnrolledStudentByIdResponse
                {
                    Success = false,
                    Message = "Student is not enrolled in this course"
                };
            }

            // Fetch student details from UserService
            var studentInfo = await _userService.GetUserByIdAsync(request.StudentId, cancellationToken);
            if (studentInfo == null)
            {
                return new GetEnrolledStudentByIdResponse
                {
                    Success = false,
                    Message = "Student details not found"
                };
            }

            // Get group information if student is in a group
            Guid? groupId = null;
            string? groupName = null;
            bool isGroupLeader = false;

            // Get all groups for the course with members
            var groups = await _unitOfWork.Groups.GetGroupsByCourseAsync(request.CourseId, cancellationToken);

            // Find if student is in any group
            foreach (var group in groups)
            {
                var member = group.Members.FirstOrDefault(m => m.EnrollmentId == enrollmentData.Id);
                if (member != null)
                {
                    groupId = group.Id;
                    groupName = group.Name;
                    isGroupLeader = member.IsLeader;
                    break;
                }
            }

            return new GetEnrolledStudentByIdResponse
            {
                Success = true,
                Message = "Student details retrieved successfully",
                Student = new EnrolledStudentDetailDto
                {
                    StudentId = request.StudentId,
                    StudentName = studentInfo.FullName ?? "Unknown",
                    Email = studentInfo.Email ?? "Unknown",
                    EnrolledAt = enrollmentData.JoinedAt,
                    Status = enrollmentData.Status.ToString(),
                    GroupId = groupId,
                    GroupName = groupName,
                    IsGroupLeader = isGroupLeader
                }
            };
        }
        catch (Exception ex)
        {
            return new GetEnrolledStudentByIdResponse
            {
                Success = false,
                Message = $"Error retrieving student details: {ex.Message}"
            };
        }
    }
}
