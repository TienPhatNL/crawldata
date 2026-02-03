using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ClassroomService.Application.Features.Courses.Commands;

/// <summary>
/// Command to upload an image for a course
/// </summary>
public class UploadCourseImageCommand : IRequest<UploadCourseImageResponse>
{
    /// <summary>
    /// Course ID (from route parameter)
    /// </summary>
    [JsonIgnore]
    public Guid CourseId { get; set; }
    
    /// <summary>
    /// The image file to upload
    /// </summary>
    [JsonIgnore]
    public IFormFile Image { get; set; } = null!;
}

/// <summary>
/// Response for course image upload
/// </summary>
public class UploadCourseImageResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
