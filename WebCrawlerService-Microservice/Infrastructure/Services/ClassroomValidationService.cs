using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Domain.DTOs;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Infrastructure.Services;

public class ClassroomValidationService : IClassroomValidationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClassroomValidationService> _logger;
    private readonly string _classroomServiceBaseUrl;

    public ClassroomValidationService(
        HttpClient httpClient,
        ILogger<ClassroomValidationService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _classroomServiceBaseUrl = configuration["Services:ClassroomService:BaseUrl"]
            ?? throw new InvalidOperationException("Services:ClassroomService:BaseUrl not configured");

        _httpClient.BaseAddress = new Uri(_classroomServiceBaseUrl);
    }

    public async Task<AssignmentValidationResponse> ValidateAssignmentAccessAsync(
        Guid assignmentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Validating assignment access: Assignment {AssignmentId}, User {UserId}",
                assignmentId, userId);

            var response = await _httpClient.GetAsync(
                $"/api/public/assignments/{assignmentId}/validate-access?userId={userId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Assignment validation failed with status {StatusCode}",
                    response.StatusCode);

                return new AssignmentValidationResponse
                {
                    IsValid = false,
                    HasAccess = false,
                    Message = $"Failed to validate assignment access: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<AssignmentValidationResponse>(cancellationToken);

            return result ?? new AssignmentValidationResponse
            {
                IsValid = false,
                HasAccess = false,
                Message = "Failed to deserialize validation response"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while validating assignment access");
            return new AssignmentValidationResponse
            {
                IsValid = false,
                HasAccess = false,
                Message = "Failed to communicate with ClassroomService"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while validating assignment access");
            throw;
        }
    }

    public async Task<GroupValidationResponse> ValidateGroupMembershipAsync(
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Validating group membership: Group {GroupId}, User {UserId}",
                groupId, userId);

            var response = await _httpClient.GetAsync(
                $"/api/public/groups/{groupId}/validate-membership?userId={userId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Group validation failed with status {StatusCode}",
                    response.StatusCode);

                return new GroupValidationResponse
                {
                    IsValid = false,
                    IsMember = false,
                    Message = $"Failed to validate group membership: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<GroupValidationResponse>(cancellationToken);

            return result ?? new GroupValidationResponse
            {
                IsValid = false,
                IsMember = false,
                Message = "Failed to deserialize validation response"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while validating group membership");
            return new GroupValidationResponse
            {
                IsValid = false,
                IsMember = false,
                Message = "Failed to communicate with ClassroomService"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while validating group membership");
            throw;
        }
    }

    public async Task<GroupInfo?> GetGroupInfoAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting group info for {GroupId}", groupId);

            var response = await _httpClient.GetFromJsonAsync<GroupInfo>(
                $"/api/public/groups/{groupId}",
                cancellationToken);

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while getting group info for {GroupId}", groupId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting group info");
            return null;
        }
    }

    public async Task<AssignmentInfo?> GetAssignmentInfoAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting assignment info for {AssignmentId}", assignmentId);

            var response = await _httpClient.GetFromJsonAsync<AssignmentInfo>(
                $"/api/public/assignments/{assignmentId}",
                cancellationToken);

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while getting assignment info for {AssignmentId}", assignmentId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting assignment info");
            return null;
        }
    }
}
