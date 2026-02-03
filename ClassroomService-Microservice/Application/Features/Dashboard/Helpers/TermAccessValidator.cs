using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Dashboard.Helpers;

public class TermAccessValidator
{
    private readonly IUnitOfWork _unitOfWork;

    public TermAccessValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Validates that a student has enrollments in the specified term
    /// </summary>
    public async Task<bool> ValidateStudentAccessToTermAsync(Guid studentId, Guid termId, CancellationToken cancellationToken = default)
    {
        var enrollments = await _unitOfWork.CourseEnrollments
            .GetManyAsync(e => e.StudentId == studentId, cancellationToken);

        if (!enrollments.Any()) return false;

        var courseIds = enrollments.Select(e => e.CourseId).ToList();
        var courses = await _unitOfWork.Courses
            .GetManyAsync(c => courseIds.Contains(c.Id) && c.TermId == termId, cancellationToken);

        return courses.Any();
    }

    /// <summary>
    /// Validates that a lecturer has courses in the specified term
    /// </summary>
    public async Task<bool> ValidateLecturerAccessToTermAsync(Guid lecturerId, Guid termId, CancellationToken cancellationToken = default)
    {
        var courses = await _unitOfWork.Courses
            .GetManyAsync(c => c.LecturerId == lecturerId && c.TermId == termId, cancellationToken);

        return courses.Any();
    }

    /// <summary>
    /// Validates that a lecturer owns the specified course
    /// </summary>
    public async Task<bool> ValidateLecturerOwnsCourseAsync(Guid lecturerId, Guid courseId, CancellationToken cancellationToken = default)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(courseId, cancellationToken);
        return course != null && course.LecturerId == lecturerId;
    }

    /// <summary>
    /// Validates that a student is enrolled in the specified course
    /// </summary>
    public async Task<bool> ValidateStudentEnrolledInCourseAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
    {
        var enrollment = await _unitOfWork.CourseEnrollments
            .GetAsync(e => e.StudentId == studentId && e.CourseId == courseId, cancellationToken);

        return enrollment != null;
    }

    /// <summary>
    /// Gets the default term ID for a user based on current active term
    /// </summary>
    public async Task<Guid?> GetDefaultTermIdAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var currentTerm = await _unitOfWork.Terms
            .GetAsync(t => t.IsActive && t.StartDate <= now && t.EndDate >= now, cancellationToken);

        return currentTerm?.Id;
    }

    /// <summary>
    /// Validates term access based on user role
    /// </summary>
    public async Task<bool> ValidateUserAccessToTermAsync(Guid userId, string userRole, Guid termId, CancellationToken cancellationToken = default)
    {
        if (userRole == RoleConstants.Student)
        {
            return await ValidateStudentAccessToTermAsync(userId, termId, cancellationToken);
        }
        else if (userRole == RoleConstants.Lecturer)
        {
            return await ValidateLecturerAccessToTermAsync(userId, termId, cancellationToken);
        }

        return false;
    }
}
