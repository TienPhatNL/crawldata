using System.Text.Json.Serialization;
using MediatR;

namespace ClassroomService.Application.Features.Topics.Commands;

/// <summary>
/// Command to update an existing topic
/// </summary>
public class UpdateTopicCommand : IRequest<UpdateTopicResponse>
{
    [JsonIgnore]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
