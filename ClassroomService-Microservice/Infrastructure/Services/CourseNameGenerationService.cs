using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// Service for generating course names based on course code and lecturer information
/// </summary>
public class CourseNameGenerationService : ICourseNameGenerationService
{
    private readonly ClassroomDbContext _context;
    private readonly IKafkaUserService _kafkaUserService;
    private readonly ILogger<CourseNameGenerationService> _logger;

    public CourseNameGenerationService(
        ClassroomDbContext context,
        IKafkaUserService kafkaUserService,
        ILogger<CourseNameGenerationService> logger)
    {
        _context = context;
        _kafkaUserService = kafkaUserService;
        _logger = logger;
    }

    public async Task<string> GenerateCourseNameAsync(Guid courseCodeId, string uniqueCode, Guid lecturerId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get course code
            var courseCode = await _context.CourseCodes
                .FirstOrDefaultAsync(cc => cc.Id == courseCodeId, cancellationToken);

            if (courseCode == null)
            {
                _logger.LogWarning("CourseCode with ID {CourseCodeId} not found", courseCodeId);
                return "Unknown Course";
            }

            // Get lecturer information
            var lecturer = await _kafkaUserService.GetUserByIdAsync(lecturerId, cancellationToken);
            if (lecturer == null)
            {
                _logger.LogWarning("Lecturer with ID {LecturerId} not found", lecturerId);
                return $"{courseCode.Code}#{uniqueCode} - Unknown Lecturer";
            }

            var lecturerFullName = GetLecturerFullName(lecturer);
            return GenerateCourseName(courseCode.Code, uniqueCode, lecturerFullName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating course name for CourseCodeId {CourseCodeId} and LecturerId {LecturerId}", 
                courseCodeId, lecturerId);
            return "Unknown Course";
        }
    }

    public string GenerateCourseName(string courseCode, string uniqueCode, string lecturerFullName)
    {
        if (string.IsNullOrWhiteSpace(courseCode))
        {
            courseCode = "UNKNOWN";
        }

        if (string.IsNullOrWhiteSpace(uniqueCode))
        {
            uniqueCode = "XXXXXX";
        }

        if (string.IsNullOrWhiteSpace(lecturerFullName))
        {
            lecturerFullName = "Unknown Lecturer";
        }

        return $"{courseCode}#{uniqueCode} - {lecturerFullName}";
    }

    public async Task<int> UpdateCourseNamesForLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get lecturer information
            var lecturer = await _kafkaUserService.GetUserByIdAsync(lecturerId, cancellationToken);
            if (lecturer == null)
            {
                _logger.LogWarning("Lecturer with ID {LecturerId} not found for name update", lecturerId);
                return 0;
            }

            var lecturerFullName = GetLecturerFullName(lecturer);

            // Get all courses for this lecturer
            var courses = await _context.Courses
                .Include(c => c.CourseCode)
                .Where(c => c.LecturerId == lecturerId)
                .ToListAsync(cancellationToken);

            int updatedCount = 0;
            foreach (var course in courses)
            {
                var newName = GenerateCourseName(course.CourseCode.Code, course.UniqueCode, lecturerFullName);
                if (course.Name != newName)
                {
                    course.Name = newName;
                    course.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated course names for {UpdatedCount} courses for lecturer {LecturerId}", 
                    updatedCount, lecturerId);
            }

            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course names for lecturer {LecturerId}", lecturerId);
            return 0;
        }
    }

    public async Task<int> UpdateCourseNamesForCourseCodeAsync(Guid courseCodeId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get course code
            var courseCode = await _context.CourseCodes
                .FirstOrDefaultAsync(cc => cc.Id == courseCodeId, cancellationToken);

            if (courseCode == null)
            {
                _logger.LogWarning("CourseCode with ID {CourseCodeId} not found for name update", courseCodeId);
                return 0;
            }

            // Get all courses using this course code
            var courses = await _context.Courses
                .Where(c => c.CourseCodeId == courseCodeId)
                .ToListAsync(cancellationToken);

            int updatedCount = 0;
            foreach (var course in courses)
            {
                // Get lecturer for each course
                var lecturer = await _kafkaUserService.GetUserByIdAsync(course.LecturerId, cancellationToken);
                if (lecturer == null)
                {
                    _logger.LogWarning("Lecturer with ID {LecturerId} not found for course {CourseId}", 
                        course.LecturerId, course.Id);
                    continue;
                }

                var lecturerFullName = GetLecturerFullName(lecturer);
                var newName = GenerateCourseName(courseCode.Code, course.UniqueCode, lecturerFullName);
                
                if (course.Name != newName)
                {
                    course.Name = newName;
                    course.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated course names for {UpdatedCount} courses using course code {CourseCodeId}", 
                    updatedCount, courseCodeId);
            }

            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course names for course code {CourseCodeId}", courseCodeId);
            return 0;
        }
    }

    private string GetLecturerFullName(dynamic lecturer)
    {
        try
        {
            // Handle different possible formats of lecturer data
            if (lecturer == null) return "Unknown Lecturer";

            // Try to get LastName and FirstName
            string lastName = lecturer.LastName?.ToString() ?? "";
            string firstName = lecturer.FirstName?.ToString() ?? "";

            if (!string.IsNullOrWhiteSpace(lastName) && !string.IsNullOrWhiteSpace(firstName))
            {
                return $"{lastName} {firstName}".Trim();
            }

            // Fallback to FullName if available
            string fullName = lecturer.FullName?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return fullName;
            }

            // Final fallback
            return "Unknown Lecturer";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting lecturer full name from lecturer data");
            return "Unknown Lecturer";
        }
    }
}
