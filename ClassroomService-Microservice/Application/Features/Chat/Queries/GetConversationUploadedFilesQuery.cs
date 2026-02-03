using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.Chat.Queries;

public class GetConversationUploadedFilesQuery : IRequest<GetConversationUploadedFilesResponse>
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
}

public class GetConversationUploadedFilesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ConversationUploadedFileDto> Files { get; set; } = new();
}
