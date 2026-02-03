using ClassroomService.Application.Common.Helpers;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ClassroomService.Application.Features.Chat.Commands;

/// <summary>
/// Handler for uploading CSV files to conversations
/// Parses CSV data and stores it in ConversationUploadedFiles table
/// </summary>
public class UploadConversationCsvCommandHandler : IRequestHandler<UploadConversationCsvCommand, UploadConversationCsvResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ILogger<UploadConversationCsvCommandHandler> _logger;

    public UploadConversationCsvCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ILogger<UploadConversationCsvCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _logger = logger;
    }

    public async Task<UploadConversationCsvResponse> Handle(UploadConversationCsvCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user is authenticated
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new UploadConversationCsvResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            var userId = currentUserId.Value;

            // Validate file
            if (!FileValidationHelper.ValidateCsvFile(request.File, out var validationError))
            {
                return new UploadConversationCsvResponse
                {
                    Success = false,
                    Message = validationError
                };
            }

            // Get conversation and create if it doesn't exist (for new conversations)
            var conversation = await _unitOfWork.Conversations.GetByIdAsync(request.ConversationId, cancellationToken);
            if (conversation == null)
            {
                // Conversation doesn't exist - this can happen for new conversations before any messages are sent
                // Try to find AssignmentId from existing CrawlerChatMessages with this ConversationId
                var existingMessages = await _unitOfWork.CrawlerChatMessages.GetByConversationIdAsync(
                    request.ConversationId, 
                    cancellationToken);
                
                var assignmentId = existingMessages.FirstOrDefault()?.AssignmentId ?? Guid.Empty;
                var courseId = Guid.Empty;
                
                // Get CourseId from Assignment if we found one
                if (assignmentId != Guid.Empty)
                {
                    var assignment = await _unitOfWork.Assignments.GetByIdAsync(assignmentId, cancellationToken);
                    courseId = assignment?.CourseId ?? Guid.Empty;
                }
                
                // Create the conversation (similar to CrawlerEventConsumer pattern)
                conversation = new Conversation
                {
                    Id = request.ConversationId,
                    CourseId = courseId,
                    User1Id = userId,
                    User2Id = Guid.Empty, // System/bot conversation for crawler chat
                    IsCrawler = true,
                    Name = null, // Will be set later when first crawl completes
                    LastMessageAt = DateTime.UtcNow,
                    LastMessagePreview = string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };
                
                await _unitOfWork.Conversations.AddAsync(conversation, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation(
                    "Created conversation {ConversationId} for CSV upload (Assignment: {AssignmentId}, Course: {CourseId})",
                    request.ConversationId, assignmentId, courseId);
            }

            // Check if user has access to the conversation
            // For crawler chat conversations (User2Id == Guid.Empty), only User1Id needs to match
            if (conversation.User2Id == Guid.Empty)
            {
                // System/bot conversation - only User1Id needs to match
                if (conversation.User1Id != userId)
                {
                    return new UploadConversationCsvResponse
                    {
                        Success = false,
                        Message = "Access denied to this conversation"
                    };
                }
            }
            else
            {
                // Regular conversation - check both users
                if (conversation.User1Id != userId && conversation.User2Id != userId)
                {
                    return new UploadConversationCsvResponse
                    {
                        Success = false,
                        Message = "Access denied to this conversation"
                    };
                }
            }

            // Parse CSV file first (before upload to validate the file)
            CsvParseResult parseResult;
            try
            {
                using var stream = request.File.OpenReadStream();
                parseResult = CsvParserHelper.ParseCsv(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse CSV file {FileName}", request.File.FileName);
                return new UploadConversationCsvResponse
                {
                    Success = false,
                    Message = $"Failed to parse CSV file: {ex.Message}"
                };
            }

            // Upload file to storage (IFormFile.OpenReadStream() creates a new stream each time)
            string fileUrl;
            try
            {
                fileUrl = await _uploadService.UploadFileAsync(request.File);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload CSV file {FileName} to storage", request.File.FileName);
                return new UploadConversationCsvResponse
                {
                    Success = false,
                    Message = $"Failed to upload file: {ex.Message}"
                };
            }

            // Convert to JSON
            var (dataJson, columnNamesJson) = CsvParserHelper.ToJson(parseResult);

            // Create entity
            var uploadedFile = new ConversationUploadedFile
            {
                Id = Guid.NewGuid(),
                ConversationId = request.ConversationId,
                FileName = request.File.FileName,
                FileUrl = fileUrl,
                FileSize = request.File.Length,
                DataJson = dataJson,
                ColumnNamesJson = columnNamesJson,
                RowCount = parseResult.RowCount,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = userId,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            // Save to database
            await _unitOfWork.ConversationUploadedFiles.AddAsync(uploadedFile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Parse column names from JSON for response
            var columnNames = JsonSerializer.Deserialize<List<string>>(columnNamesJson) ?? new List<string>();

            _logger.LogInformation(
                "CSV file uploaded successfully to Conversation {ConversationId} by User {UserId}. File: {FileName}, Rows: {RowCount}",
                request.ConversationId,
                userId,
                request.File.FileName,
                parseResult.RowCount);

            return new UploadConversationCsvResponse
            {
                Success = true,
                Message = "CSV file uploaded successfully",
                FileId = uploadedFile.Id,
                FileName = uploadedFile.FileName,
                RowCount = uploadedFile.RowCount,
                ColumnNames = columnNames
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading CSV file to conversation {ConversationId}", request.ConversationId);
            return new UploadConversationCsvResponse
            {
                Success = false,
                Message = $"Error uploading CSV file: {ex.Message}"
            };
        }
    }
}
