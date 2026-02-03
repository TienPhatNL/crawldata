using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

/// <summary>
/// Command to upload a syllabus file for a course request
/// Only the requesting lecturer can upload the syllabus
/// </summary>
public class UploadCourseRequestSyllabusCommand : IRequest<UploadCourseRequestSyllabusResponse>
{
    /// <summary>
    /// CourseRequest ID (from route parameter)
    /// </summary>
    [JsonIgnore]
    public Guid CourseRequestId { get; set; }
    
    /// <summary>
    /// The syllabus file to upload (PDF, DOCX, PPTX, ZIP)
    /// </summary>
    [JsonIgnore]
    public IFormFile File { get; set; } = null!;
}

/// <summary>
/// Response for course request syllabus upload
/// </summary>
public class UploadCourseRequestSyllabusResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
}
