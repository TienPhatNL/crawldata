using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.CrawlerChat.Queries;

public class GetAssignmentConversationsQuery : IRequest<List<ConversationSummaryDto>>
{
    public Guid AssignmentId { get; set; }
    public Guid? UserId { get; set; } // Optional: filter by user participation
}
