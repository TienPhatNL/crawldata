using System.Text.Json.Serialization;
using MediatR;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

/// <summary>
/// Command to remove a specific image attachment from a support request
/// </summary>
public class RemoveSupportRequestImageCommand : IRequest<RemoveSupportRequestImageResponse>
{
    /// <summary>
    /// Support Request ID (from route parameter)
    /// </summary>
    [JsonIgnore]
    public Guid SupportRequestId { get; set; }
    
    /// <summary>
    /// The image URL to remove (from request body)
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response for support request image removal
/// </summary>
public class RemoveSupportRequestImageResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
