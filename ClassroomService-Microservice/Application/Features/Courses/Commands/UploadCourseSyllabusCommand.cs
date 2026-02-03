using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ClassroomService.Application.Features.Courses.Commands;

/// <summary>
/// Command to upload a syllabus file for a course
/// Only the course lecturer can upload the syllabus
/// </summary>
public class UploadCourseSyllabusCommand : IRequest<UploadCourseSyllabusResponse>
{
    /// <summary>
    /// Course ID (from route parameter)
    /// </summary>
    [JsonIgnore]
    public Guid CourseId { get; set; }
    
    /// <summary>
    /// The syllabus file to upload (PDF, DOCX, PPTX, ZIP)
    /// </summary>
    [JsonIgnore]
    public IFormFile File { get; set; } = null!;
}

/// <summary>
/// Response for course syllabus upload
/// </summary>
public class UploadCourseSyllabusResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
}
