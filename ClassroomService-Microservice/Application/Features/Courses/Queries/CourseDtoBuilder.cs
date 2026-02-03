using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Helper class for building CourseDto with proper access control
/// </summary>
public static class CourseDtoBuilder
{
    /// <summary>
    /// Builds a CourseDto from a Course entity with appropriate access control
    /// </summary>
    /// <param name="course">The course entity</param>
    /// <param name="lecturerName">The lecturer's name</param>
    /// <param name="enrollmentCount">Number of active enrollments</param>
    /// <param name="currentUserId">Current user ID (optional)</param>
    /// <param name="currentUserRole">Current user role (optional)</param>
    /// <param name="accessCodeService">Access code service for checking expiration</param>
    /// <param name="showFullAccessCodeInfo">Whether to show full access code information (for lecturer's own courses in my-courses)</param>
    /// <param name="approvedByName">Name of the user who approved the course (optional, for display purposes)</param>
    /// <param name="lecturerImage">The lecturer's profile picture URL (optional)</param>
    /// <returns>CourseDto with appropriate access control applied</returns>
    public static CourseDto BuildCourseDto(
        Course course, 
        string lecturerName, 
        int enrollmentCount,
        Guid? currentUserId = null,
        string? currentUserRole = null,
        IAccessCodeService? accessCodeService = null,
        bool showFullAccessCodeInfo = false,
        string? approvedByName = null,
        string? lecturerImage = null)
    {
        var isAccessCodeExpired = course.AccessCodeExpiresAt.HasValue && DateTime.UtcNow > course.AccessCodeExpiresAt;
        
        // If access code service is available, use it for more accurate expiration check
        if (accessCodeService != null)
        {
            isAccessCodeExpired = accessCodeService.IsAccessCodeExpired(course);
        }

        // Determine access level based on user role and context
        var accessLevel = DetermineAccessLevel(course, currentUserId, currentUserRole, showFullAccessCodeInfo);

        var courseDto = new CourseDto
        {
            Id = course.Id,
            CourseCode = course.CourseCode.Code,
            UniqueCode = course.UniqueCode ?? string.Empty,
            CourseCodeTitle = course.CourseCode.Title,
            Name = course.Name,
            Description = course.Description,
            Term = course.Term.Name,
            TermStartDate = course.Term.StartDate,
            TermEndDate = course.Term.EndDate,
            LecturerId = course.LecturerId,
            LecturerName = lecturerName,
            LecturerImage = lecturerImage,
            CreatedAt = course.CreatedAt,
            EnrollmentCount = enrollmentCount,
            Status = course.Status,
            ApprovedBy = course.ApprovedBy,
            ApprovedByName = approvedByName,
            ApprovedAt = course.ApprovedAt,
            ApprovalComments = course.ApprovalComments,
            RejectionReason = course.RejectionReason,
            CanEnroll = course.Status == CourseStatus.Active,
            RequiresAccessCode = course.RequiresAccessCode,
            Department = course.CourseCode.Department,
            Img = course.Img,
            Announcement = course.Announcement,
            SyllabusFile = course.SyllabusFile
        };

        // Conditionally add access code details based on access level
        if (accessLevel.ShowAccessCode)
        {
            courseDto.AccessCode = course.AccessCode;
        }

        if (accessLevel.ShowAccessCodeDetails)
        {
            courseDto.AccessCodeCreatedAt = course.AccessCodeCreatedAt;
            courseDto.AccessCodeExpiresAt = course.AccessCodeExpiresAt;
        }

        if (accessLevel.ShowExpirationStatus && course.RequiresAccessCode)
        {
            courseDto.IsAccessCodeExpired = isAccessCodeExpired;
        }

        return courseDto;
    }

    /// <summary>
    /// Determines the access level for a user based on their role and context
    /// </summary>
    /// <param name="course">The course</param>
    /// <param name="currentUserId">Current user ID</param>
    /// <param name="currentUserRole">Current user role</param>
    /// <param name="showFullAccessCodeInfo">Whether this is a my-courses lecturer view</param>
    /// <returns>Access level configuration</returns>
    private static AccessLevel DetermineAccessLevel(Course course, Guid? currentUserId, string? currentUserRole, bool showFullAccessCodeInfo)
    {
        // Anonymous users or users without role - minimal access
        if (!currentUserId.HasValue || string.IsNullOrEmpty(currentUserRole))
        {
            return new AccessLevel
            {
                ShowAccessCode = false,
                ShowAccessCodeDetails = false,
                ShowExpirationStatus = false
            };
        }

        // Lecturers viewing their own courses in my-courses view get full access
        if (showFullAccessCodeInfo && 
            currentUserRole.Equals("Lecturer", StringComparison.OrdinalIgnoreCase) && 
            course.LecturerId == currentUserId.Value)
        {
            return new AccessLevel
            {
                ShowAccessCode = true,
                ShowAccessCodeDetails = true,
                ShowExpirationStatus = true
            };
        }

        // Admin/Staff can see expiration status for administrative purposes
        if (currentUserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) || 
            currentUserRole.Equals("Staff", StringComparison.OrdinalIgnoreCase))
        {
            return new AccessLevel
            {
                ShowAccessCode = false,
                ShowAccessCodeDetails = false,
                ShowExpirationStatus = true // Admin/Staff can see if code is expired
            };
        }

        // Students and other users get minimal access (only RequiresAccessCode is visible)
        // This includes:
        // - Students in any view
        // - Lecturers viewing other lecturers' courses
        // - Lecturers in general course listings (not my-courses)
        return new AccessLevel
        {
            ShowAccessCode = false,
            ShowAccessCodeDetails = false,
            ShowExpirationStatus = false
        };
    }

    /// <summary>
    /// Represents the access level for course access code information
    /// </summary>
    private class AccessLevel
    {
        /// <summary>
        /// Whether to show the actual access code
        /// </summary>
        public bool ShowAccessCode { get; set; }

        /// <summary>
        /// Whether to show access code details (creation date, expiry, etc.)
        /// </summary>
        public bool ShowAccessCodeDetails { get; set; }

        /// <summary>
        /// Whether to show access code expiration status
        /// </summary>
        public bool ShowExpirationStatus { get; set; }
    }
}