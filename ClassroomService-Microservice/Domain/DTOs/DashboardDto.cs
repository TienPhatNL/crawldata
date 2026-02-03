namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Base dashboard response wrapper
/// </summary>
public class DashboardResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string? Message { get; set; }
}

// ============= STUDENT DASHBOARD DTOs =============

/// <summary>
/// Student's overall grade overview across all courses
/// </summary>
public class StudentGradesOverviewDto
{
    public decimal? CurrentTermGpa { get; set; }
    public decimal? OverallGpa { get; set; }
    public decimal AverageGrade { get; set; }
    public int TotalAssignmentsCompleted { get; set; }
    public int TotalAssignments { get; set; }
    public List<CourseGradeSummaryDto> Courses { get; set; } = new();
    public GradeDistributionDto GradeDistribution { get; set; } = new();
}

/// <summary>
/// Grade summary for a single course
/// </summary>
public class CourseGradeSummaryDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string TermName { get; set; } = string.Empty;
    public decimal? CurrentGrade { get; set; }
    public string? LetterGrade { get; set; }
    public int CompletedAssignments { get; set; }
    public int TotalAssignments { get; set; }
    public decimal CompletionRate { get; set; }
}

/// <summary>
/// Grade distribution statistics
/// </summary>
public class GradeDistributionDto
{
    public int ACount { get; set; }
    public int BCount { get; set; }
    public int CCount { get; set; }
    public int DCount { get; set; }
    public int FCount { get; set; }
    public int UngradeCount { get; set; }
}

/// <summary>
/// Detailed course grades with all assignments
/// </summary>
public class CourseGradesDetailDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public decimal? AverageGrade { get; set; }
    public string? LetterGrade { get; set; }
    public List<DashboardAssignmentGradeDto> Assignments { get; set; } = new();
    public Dictionary<string, decimal> TopicBreakdown { get; set; } = new();
    public List<GradeTrendPoint> GradeTrend { get; set; } = new();
    public decimal? ClassAverage { get; set; }
}

/// <summary>
/// Detailed grade breakdown showing weighted contributions
/// </summary>
public class StudentGradeBreakdownDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public List<AssignmentGradeDetailDto> AssignmentBreakdown { get; set; } = new();
    public decimal WeightedCourseGrade { get; set; }
    public string LetterGrade { get; set; } = string.Empty;
    public decimal TotalWeightUsed { get; set; }
    public decimal RemainingWeight { get; set; }
}

/// <summary>
/// Detailed grade for single assignment with weight contribution
/// </summary>
public class AssignmentGradeDetailDto
{
    public Guid AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public decimal? Grade { get; set; }
    public int MaxPoints { get; set; }
    public decimal Weight { get; set; }
    public decimal WeightedContribution { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Grade for a single assignment (Dashboard)
/// </summary>
public class DashboardAssignmentGradeDto
{
    public Guid AssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public decimal? Grade { get; set; }
    public int? MaxPoints { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Feedback { get; set; }
}

/// <summary>
/// Grade trend over time
/// </summary>
public class GradeTrendPoint
{
    public DateTime Date { get; set; }
    public decimal? Grade { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
}

/// <summary>
/// Pending assignments summary
/// </summary>
public class PendingAssignmentsDto
{
    public List<PendingAssignmentDto> UpcomingAssignments { get; set; } = new();
    public List<PendingAssignmentDto> DraftReports { get; set; } = new();
    public List<PendingAssignmentDto> RevisionRequests { get; set; } = new();
    public int TotalPending { get; set; }
}

/// <summary>
/// Single pending assignment
/// </summary>
public class PendingAssignmentDto
{
    public Guid AssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime? ExtendedDueDate { get; set; }
    public int HoursUntilDue { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsGroupAssignment { get; set; }
    public string? GroupName { get; set; }
    public string? ReportStatus { get; set; }
    public Guid? ReportId { get; set; }
}

/// <summary>
/// Current enrolled courses
/// </summary>
public class CurrentCoursesDto
{
    public List<CurrentCourseDto> Courses { get; set; } = new();
    public int TotalEnrolled { get; set; }
    public string CurrentTermName { get; set; } = string.Empty;
}

/// <summary>
/// Single current course
/// </summary>
public class CurrentCourseDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string LecturerName { get; set; } = string.Empty;
    public int PendingAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int TotalAssignments { get; set; }
    public decimal? CurrentGrade { get; set; }
    public decimal ProgressPercentage { get; set; }
    public string? CourseImage { get; set; }
}

/// <summary>
/// Student performance analytics
/// </summary>
public class StudentPerformanceAnalyticsDto
{
    public decimal OnTimeSubmissionRate { get; set; }
    public decimal LateSubmissionRate { get; set; }
    public int TotalSubmissions { get; set; }
    public int OnTimeSubmissions { get; set; }
    public int LateSubmissions { get; set; }
    public decimal AverageGrade { get; set; }
    public List<CoursePerformanceDto> CoursePerformance { get; set; } = new();
    public List<TopicPerformanceDto> TopicPerformance { get; set; } = new();
    public decimal ResubmissionRate { get; set; }
    public int TotalResubmissions { get; set; }
}

/// <summary>
/// Performance in a single course
/// </summary>
public class CoursePerformanceDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public decimal? AverageGrade { get; set; }
    public int AssignmentsCompleted { get; set; }
    public int AssignmentsTotal { get; set; }
    public decimal CompletionRate { get; set; }
}

/// <summary>
/// Performance by topic/category
/// </summary>
public class TopicPerformanceDto
{
    public string TopicName { get; set; } = string.Empty;
    public decimal? AverageGrade { get; set; }
    public int AssignmentsCount { get; set; }
    public string PerformanceLevel { get; set; } = string.Empty; // Excellent, Good, Average, NeedsImprovement
}

// ============= LECTURER DASHBOARD DTOs =============

/// <summary>
/// Lecturer's courses overview
/// </summary>
public class LecturerCoursesOverviewDto
{
    public List<LecturerCourseDto> Courses { get; set; } = new();
    public int TotalStudentsEnrolled { get; set; }
    public int TotalReportsPendingGrading { get; set; }
    public int TotalActiveAssignments { get; set; }
}

/// <summary>
/// Single course taught by lecturer
/// </summary>
public class LecturerCourseDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string TermName { get; set; } = string.Empty;
    public int EnrollmentCount { get; set; }
    public int PendingGradingCount { get; set; }
    public int ActiveAssignmentsCount { get; set; }
    public decimal? AverageCourseGrade { get; set; }
    public DateTime? LastSubmissionDate { get; set; }
}

/// <summary>
/// Grading queue for lecturer
/// </summary>
public class GradingQueueDto
{
    public List<PendingGradingReportDto> PendingReports { get; set; } = new();
    public int TotalPending { get; set; }
    public int SubmittedCount { get; set; }
    public int ResubmittedCount { get; set; }
}

/// <summary>
/// Single report pending grading
/// </summary>
public class PendingGradingReportDto
{
    public Guid ReportId { get; set; }
    public Guid AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int DaysSinceSubmission { get; set; }
    public bool IsGroupSubmission { get; set; }
    public string? GroupName { get; set; }
    public string SubmitterName { get; set; } = string.Empty;
    public int Version { get; set; }
}

/// <summary>
/// Student performance in a course (lecturer view)
/// </summary>
public class CourseStudentPerformanceDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public GradeDistributionDto GradeDistribution { get; set; } = new();
    public List<AssignmentPerformanceDto> AssignmentPerformance { get; set; } = new();
    public List<StudentSummaryDto> TopPerformers { get; set; } = new();
    public List<StudentSummaryDto> AtRiskStudents { get; set; } = new();
    public decimal AverageCourseGrade { get; set; }
    public decimal SubmissionRate { get; set; }
    public int TotalStudents { get; set; }
}

/// <summary>
/// Performance statistics for an assignment
/// </summary>
public class AssignmentPerformanceDto
{
    public Guid AssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal? AverageGrade { get; set; }
    public int SubmissionCount { get; set; }
    public int TotalStudents { get; set; }
    public decimal SubmissionRate { get; set; }
}

/// <summary>
/// Student summary for performance views
/// </summary>
public class StudentSummaryDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal? AverageGrade { get; set; }
    public int AssignmentsCompleted { get; set; }
    public int AssignmentsTotal { get; set; }
    public int LateSubmissions { get; set; }
    public string RiskLevel { get; set; } = string.Empty; // High, Medium, Low
}

/// <summary>
/// Assignment statistics for lecturer
/// </summary>
public class AssignmentStatisticsDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public List<AssignmentStatsDto> Assignments { get; set; } = new();
    public decimal OverallSubmissionRate { get; set; }
    public decimal OverallAverageGrade { get; set; }
}

/// <summary>
/// Detailed statistics for a single assignment
/// </summary>
public class AssignmentStatsDto
{
    public Guid AssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public int TotalSubmissions { get; set; }
    public int ExpectedSubmissions { get; set; }
    public decimal SubmissionRate { get; set; }
    public int OnTimeSubmissions { get; set; }
    public int LateSubmissions { get; set; }
    public decimal? AverageGrade { get; set; }
    public decimal? LowestGrade { get; set; }
    public decimal? HighestGrade { get; set; }
    public string DifficultyLevel { get; set; } = string.Empty; // Easy, Medium, Hard
}
