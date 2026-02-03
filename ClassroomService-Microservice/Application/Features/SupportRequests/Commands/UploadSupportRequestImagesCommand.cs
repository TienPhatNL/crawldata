using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

/// <summary>
/// Command to upload multiple image attachments to a support request
/// </summary>
public class UploadSupportRequestImagesCommand : IRequest<UploadSupportRequestImagesResponse>
{
    /// <summary>
    /// Support Request ID (from route parameter)
    /// </summary>
    [JsonIgnore]
    public Guid SupportRequestId { get; set; }
    
    /// <summary>
    /// The image files to upload
    /// </summary>
    [JsonIgnore]
    public List<IFormFile> Images { get; set; } = new List<IFormFile>();
}

/// <summary>
/// Response for support request images upload
/// </summary>
public class UploadSupportRequestImagesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// List of uploaded image URLs
    /// </summary>
    public List<string>? UploadedImageUrls { get; set; }
    
    /// <summary>
    /// Count of successfully uploaded images
    /// </summary>
    public int UploadedCount { get; set; }
}
