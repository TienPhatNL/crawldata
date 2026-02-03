using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.CourseRequests.DTOs;

/// <summary>
/// Data transfer object for CourseRequest
/// </summary>
public class CourseRequestDto
{
    public Guid Id { get; set; }
    public Guid CourseCodeId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseCodeTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public Guid LecturerId { get; set; }
    public string LecturerName { get; set; } = string.Empty;
    public CourseRequestStatus Status { get; set; }
    public string? RequestReason { get; set; }
    public Guid? ProcessedBy { get; set; }
    public string? ProcessedByName { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingComments { get; set; }
    public Guid? CreatedCourseId { get; set; }
    public string? Announcement { get; set; }
    public string? SyllabusFile { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Department { get; set; } = string.Empty;
}

/// <summary>
/// Filter DTO for course requests
/// </summary>
public class CourseRequestFilterDto
{
    public CourseRequestStatus? Status { get; set; }
    public string? LecturerName { get; set; }
    public string? CourseCode { get; set; }
    public string? Term { get; set; }
    public string? Department { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}