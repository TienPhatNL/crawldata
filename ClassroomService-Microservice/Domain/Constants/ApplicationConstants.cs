using System.Net;

namespace ClassroomService.Domain.Constants;

/// <summary>
/// Contains HTTP status codes and related constants
/// </summary>
public static class StatusCodes
{
    public const int Ok = 200;
    public const int Created = 201;
    public const int BadRequest = 400;
    public const int Unauthorized = 401;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int TooManyRequests = 429;
    public const int InternalServerError = 500;
}

/// <summary>
/// Contains validation and business rule constants
/// </summary>
public static class ValidationConstants
{
    // CourseCode validation
    public const int MaxCourseCodeLength = 20;
    public const int MinCourseCodeLength = 2;
    public const int MaxCourseCodeTitleLength = 200;
    public const int MinCourseCodeTitleLength = 3;
    public const int MaxCourseCodeDescriptionLength = 1000;
    public const int MaxDepartmentLength = 100;
    public const int MinDepartmentLength = 2;
    public const int MinCreditHours = 1;
    public const int MaxCreditHours = 12;

    // Course validation (updated)
    public const int MaxCourseNameLength = 300; // Increased for auto-generated names
    public const int MinCourseNameLength = 3; // Added missing constant
    public const int MaxCourseDescriptionLength = 1000; // Renamed from course name
    public const int MinCourseDescriptionLength = 3;
    public const int MaxTermLength = 100; // Updated for flexible term names
    public const int MinTermLength = 1;
    public const int MaxSectionLength = 10;

    // Access code validation (existing)
    public const int MinNumericCodeLength = 4;
    public const int MaxNumericCodeLength = 8;
    public const int MinAlphaNumericCodeLength = 4;
    public const int MaxAlphaNumericCodeLength = 12;
    public const int MinWordBasedCodeLength = 5;
    public const int MaxWordBasedCodeLength = 20;
    public const int MinCustomCodeLength = 4;
    public const int MaxCustomCodeLength = 50;

    // Rate limiting
    public const int MaxAccessCodeAttemptsPerHour = 5;
    public const int RateLimitWindowMinutes = 60;

    // Pagination
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;
    public const int MinPageSize = 1;
    public const int DefaultPage = 1;
}

/// <summary>
/// Contains sorting and filtering constants
/// </summary>
public static class SortingConstants
{
    // Sort fields
    public const string Name = "name";
    public const string CourseCode = "coursecode";
    public const string CreatedAt = "createdat";
    public const string EnrollmentCount = "enrollmentcount";
    public const string JoinedAt = "joinedat";

    // Sort directions
    public const string Ascending = "asc";
    public const string Descending = "desc";

    // Default values
    public const string DefaultSortField = CreatedAt;
    public const string DefaultSortDirection = Descending;
}

/// <summary>
/// Contains role and permission constants
/// </summary>
public static class RoleConstants
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Lecturer = "Lecturer";
    public const string Student = "Student";
}

/// <summary>
/// Contains enrollment status constants
/// </summary>
public static class EnrollmentConstants
{
    public const string Active = "Active";
    public const string Withdrawn = "Withdrawn";
    public const string Suspended = "Suspended";
}

/// <summary>
/// Contains access code generation constants
/// </summary>
public static class AccessCodeConstants
{
    // Character sets for code generation
    public const string NumericChars = "0123456789";
    public const string AlphaNumericChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excludes confusing chars
    public const string CustomAllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*-_=+";

    // Word list for word-based codes
    public static readonly string[] WordList = { 
        "happy", "bright", "quick", "smart", "cool", "fast", "blue", "green", 
        "star", "moon", "sun", "wave", "fire", "wind", "storm", "peace",
        "strong", "light", "deep", "wide", "sharp", "soft", "warm", "cold"
    };

    // Default code lengths
    public const int DefaultNumericLength = 6;
    public const int DefaultAlphaNumericLength = 6;
    public const int DefaultWordBasedMinNumber = 10;
    public const int DefaultWordBasedMaxNumber = 99;
}

/// <summary>
/// Contains logging and monitoring constants
/// </summary>
public static class LoggingConstants
{
    // Log levels
    public const string Information = "Information";
    public const string Warning = "Warning";
    public const string Error = "Error";
    public const string Debug = "Debug";

    // Event IDs for structured logging
    public const int CourseCreated = 1001;
    public const int CourseUpdated = 1002;
    public const int CourseDeleted = 1003;
    public const int StudentEnrolled = 2001;
    public const int StudentUnenrolled = 2002;
    public const int AccessCodeValidated = 3001;
    public const int AccessCodeFailed = 3002;
    public const int RateLimitExceeded = 3003;
    public const int DatabaseError = 9001;
    public const int ValidationError = 9002;
    public const int AuthorizationError = 9003;
}

/// <summary>
/// Contains cache key constants
/// </summary>
public static class CacheKeys
{
    public const string CoursePrefix = "course:";
    public const string UserPrefix = "user:";
    public const string EnrollmentPrefix = "enrollment:";
    public const string AccessCodePrefix = "accesscode:";
    
    // Cache durations (in minutes)
    public const int ShortCacheDuration = 5;
    public const int MediumCacheDuration = 30;
    public const int LongCacheDuration = 120;
}

/// <summary>
/// Contains API endpoint constants
/// </summary>
public static class ApiEndpoints
{
    public const string Courses = "api/courses";
    public const string MyCourses = "api/courses/my-courses";
    public const string AllCourses = "api/courses/all";
    public const string AvailableCourses = "api/courses/available";
    public const string CourseEnrollments = "api/courses/{id}/enrollments";
    public const string CourseStatistics = "api/courses/{id}/statistics";
    public const string CourseAccessCode = "api/courses/{id}/access-code";
    public const string CourseJoinInfo = "api/courses/{id}/join-info";
    public const string Enrollments = "api/enrollments";
    public const string JoinCourse = "api/enrollments/join";
    public const string LeaveCourse = "api/enrollments/leave";
}

/// <summary>
/// Contains notification type constants
/// </summary>
public static class NotificationTypes
{
    public const string Assignment = "Assignment";
    public const string Announcement = "Announcement";
    public const string Enrollment = "Enrollment";
    public const string AccessCode = "AccessCode";
    public const string General = "General";
}

/// <summary>
/// Contains database configuration constants
/// </summary>
public static class DatabaseConstants
{
    // Connection string names
    public const string DefaultConnection = "DefaultConnection";
    public const string ReadOnlyConnection = "ReadOnlyConnection";

    // Schema names
    public const string DefaultSchema = "dbo";
    public const string AuditSchema = "audit";

    // Table names
    public const string CoursesTable = "Courses";
    public const string EnrollmentsTable = "CourseEnrollments";
    public const string AssignmentsTable = "Assignments";
    public const string NotificationsTable = "Notifications";
}

/// <summary>
/// Contains configuration section names
/// </summary>
public static class ConfigurationSections
{
    public const string ConnectionStrings = "ConnectionStrings";
    public const string Logging = "Logging";
    public const string Authentication = "Authentication";
    public const string Authorization = "Authorization";
    public const string AccessCodeSettings = "AccessCodeSettings";
    public const string PaginationSettings = "PaginationSettings";
    public const string CacheSettings = "CacheSettings";
    public const string NotificationSettings = "NotificationSettings";
}