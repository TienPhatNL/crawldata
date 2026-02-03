using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

public class CreateSupportRequestCommand : IRequest<CreateSupportRequestResponse>
{
    public Guid CourseId { get; set; }
    [JsonIgnore]
    public Guid RequesterId { get; set; }
    public int Priority { get; set; } = 1; // Medium
    public int Category { get; set; } = 0; // Technical
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [JsonIgnore]
    public List<IFormFile>? Images { get; set; }
}

public class CreateSupportRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? SupportRequestId { get; set; }
}
