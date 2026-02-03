namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service for generating course names based on course code and lecturer information
/// </summary>
public interface ICourseNameGenerationService
{
    /// <summary>
    /// Generates a course name using CourseCode, UniqueCode, and Lecturer information from their IDs
    /// </summary>
    /// <param name="courseCodeId">The course code ID</param>
    /// <param name="uniqueCode">The unique course code</param>
    /// <param name="lecturerId">The lecturer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated course name in format: "CourseCode - UniqueCode - Lecturer Full Name"</returns>
    Task<string> GenerateCourseNameAsync(Guid courseCodeId, string uniqueCode, Guid lecturerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a course name using direct values
    /// </summary>
    /// <param name="courseCode">The course code string</param>
    /// <param name="uniqueCode">The unique course code</param>
    /// <param name="lecturerFullName">The lecturer's full name</param>
    /// <returns>Generated course name in format: "CourseCode - UniqueCode - Lecturer Full Name"</returns>
    string GenerateCourseName(string courseCode, string uniqueCode, string lecturerFullName);
    
    /// <summary>
    /// Updates course names for all courses taught by a specific lecturer
    /// (useful when lecturer name changes)
    /// </summary>
    /// <param name="lecturerId">The lecturer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of courses updated</returns>
    Task<int> UpdateCourseNamesForLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates course names for all courses using a specific course code
    /// (useful when course code details change)
    /// </summary>
    /// <param name="courseCodeId">The course code ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of courses updated</returns>
    Task<int> UpdateCourseNamesForCourseCodeAsync(Guid courseCodeId, CancellationToken cancellationToken = default);
}