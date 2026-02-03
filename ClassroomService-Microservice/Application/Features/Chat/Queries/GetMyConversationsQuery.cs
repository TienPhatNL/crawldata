using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.Chat.Queries;

public class GetMyConversationsQuery : IRequest<GetMyConversationsResponse>
{
    public Guid UserId { get; set; }
    public Guid? CourseId { get; set; }
}

public class GetMyConversationsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ConversationDto> Conversations { get; set; } = new();
}
