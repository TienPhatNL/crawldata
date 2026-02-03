namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Course statistics data transfer object
/// </summary>
public class CourseStatisticsDto
{
    /// <summary>
    /// Basic course information
    /// </summary>
    public CourseDto Course { get; set; } = null!;

    /// <summary>
    /// Total number of students enrolled
    /// </summary>
    public int TotalEnrollments { get; set; }

    /// <summary>
    /// Number of assignments in the course
    /// </summary>
    public int TotalAssignments { get; set; }

    /// <summary>
    /// Number of groups in the course
    /// </summary>
    public int TotalGroups { get; set; }

    /// <summary>
    /// Number of chat messages in the course
    /// </summary>
    public int TotalChatMessages { get; set; }

    /// <summary>
    /// Recent enrollment activity (last 7 days)
    /// </summary>
    public int RecentEnrollments { get; set; }

    /// <summary>
    /// When the course was last updated
    /// </summary>
    public DateTime LastActivity { get; set; }

    /// <summary>
    /// Enrollment statistics by month (for charts)
    /// </summary>
    public Dictionary<string, int> EnrollmentsByMonth { get; set; } = new();
}

/// <summary>
/// Response for getting course statistics
/// </summary>
public class GetCourseStatisticsResponse
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Course statistics data
    /// </summary>
    public CourseStatisticsDto? Statistics { get; set; }
}