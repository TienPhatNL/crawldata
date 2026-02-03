using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.ApiKeys.Queries;

public class GetApiKeysQueryHandler : IRequestHandler<GetApiKeysQuery, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetApiKeysQueryHandler> _logger;

    public GetApiKeysQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetApiKeysQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(GetApiKeysQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("API keys requested for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        // Check if user has premium subscription (required for API access)
        // Tier levels: Free=0, Basic=1, Premium=2, Enterprise=3
        if (user.SubscriptionTier?.Level <= 1) // Free or Basic
        {
            _logger.LogWarning("API keys requested by user {UserId} without premium subscription", request.UserId);
            throw new ValidationException("API key access requires Premium or Enterprise subscription");
        }

        // Get user's API keys
        var apiKeys = await _unitOfWork.UserApiKeys.GetManyAsync(k => k.UserId == user.Id, cancellationToken);

        var maxKeys = user.SubscriptionTier?.Level == 2 ? 3 : 10; // Premium (level 2): 3, Enterprise (level 3): 10
        var activeKeys = apiKeys.Where(k => k.IsActive).Count();

        var apiKeyDtos = apiKeys.OrderByDescending(k => k.CreatedAt).Select(k => new
        {
            id = k.Id,
            name = k.Name,
            description = k.Description,
            keyPrefix = k.KeyPrefix + "...",
            scopes = k.Scopes?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>(),
            createdAt = k.CreatedAt,
            lastUsedAt = k.LastUsedAt,
            expiresAt = k.ExpiresAt,
            isActive = k.IsActive,
            isExpired = k.ExpiresAt.HasValue && k.ExpiresAt.Value < DateTime.UtcNow
        }).ToList();

        _logger.LogInformation("Retrieved {KeyCount} API keys for user {UserId}", apiKeyDtos.Count, request.UserId);

        var data = new
        {
            apiKeys = apiKeyDtos,
            maxKeysAllowed = maxKeys,
            keysRemaining = maxKeys - activeKeys
        };

        return new ResponseModel(HttpStatusCode.OK, "API keys retrieved successfully", data);
    }
}