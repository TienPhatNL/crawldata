using MediatR;

namespace ClassroomService.Application.Features.Chat.Commands;

public class DeleteMessageCommand : IRequest<DeleteMessageResponse>
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
}

public class DeleteMessageResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? ReceiverId { get; set; }
}
