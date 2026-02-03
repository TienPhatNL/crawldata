using MediatR;

namespace ClassroomService.Application.Features.Enrollments.Queries;

/// <summary>
/// Query to get a specific enrolled student's details in a course
/// </summary>
public class GetEnrolledStudentByIdQuery : IRequest<GetEnrolledStudentByIdResponse>
{
    /// <summary>
    /// The course ID
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// The student ID to retrieve
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// The user making the request (for authorization)
    /// </summary>
    public Guid RequestedBy { get; set; }
}

/// <summary>
/// Response containing enrolled student details
/// </summary>
public class GetEnrolledStudentByIdResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public EnrolledStudentDetailDto? Student { get; set; }
}

/// <summary>
/// DTO for enrolled student details
/// </summary>
public class EnrolledStudentDetailDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public bool IsGroupLeader { get; set; }
}
