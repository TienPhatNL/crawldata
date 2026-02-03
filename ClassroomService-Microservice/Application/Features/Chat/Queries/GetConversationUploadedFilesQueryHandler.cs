using System.Text.Json;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Chat.Queries;

public class GetConversationUploadedFilesQueryHandler : IRequestHandler<GetConversationUploadedFilesQuery, GetConversationUploadedFilesResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConversationUploadedFilesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetConversationUploadedFilesResponse> Handle(GetConversationUploadedFilesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await _unitOfWork.Conversations.GetByIdAsync(request.ConversationId, cancellationToken);
            if (conversation == null)
            {
                return new GetConversationUploadedFilesResponse
                {
                    Success = false,
                    Message = "Conversation not found",
                    Files = new List<ConversationUploadedFileDto>()
                };
            }

            if (conversation.User1Id != request.UserId && conversation.User2Id != request.UserId)
            {
                return new GetConversationUploadedFilesResponse
                {
                    Success = false,
                    Message = "Access denied to this conversation",
                    Files = new List<ConversationUploadedFileDto>()
                };
            }

            var uploadedFiles = await _unitOfWork.ConversationUploadedFiles
                .GetByConversationIdOrderedAsync(request.ConversationId, cancellationToken);

            var result = new List<ConversationUploadedFileDto>(uploadedFiles.Count);

            foreach (var file in uploadedFiles)
            {
                var columnNames = new List<string>();
                if (!string.IsNullOrWhiteSpace(file.ColumnNamesJson))
                {
                    try
                    {
                        columnNames = JsonSerializer.Deserialize<List<string>>(file.ColumnNamesJson)
                                      ?? new List<string>();
                    }
                    catch (JsonException)
                    {
                        columnNames = new List<string>();
                    }
                }

                result.Add(new ConversationUploadedFileDto
                {
                    Id = file.Id,
                    ConversationId = file.ConversationId,
                    FileName = file.FileName,
                    FileUrl = file.FileUrl,
                    FileSize = file.FileSize,
                    RowCount = file.RowCount,
                    ColumnNames = columnNames,
                    UploadedAt = file.UploadedAt,
                    UploadedBy = file.UploadedBy
                });
            }

            return new GetConversationUploadedFilesResponse
            {
                Success = true,
                Message = "Files retrieved successfully",
                Files = result
            };
        }
        catch (Exception ex)
        {
            return new GetConversationUploadedFilesResponse
            {
                Success = false,
                Message = $"Error retrieving uploaded files: {ex.Message}",
                Files = new List<ConversationUploadedFileDto>()
            };
        }
    }
}
