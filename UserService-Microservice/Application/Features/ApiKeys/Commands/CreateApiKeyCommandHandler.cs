using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Events;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.ApiKeys.Commands;

public class CreateApiKeyCommandHandler : IRequestHandler<CreateApiKeyCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateApiKeyCommandHandler> _logger;

    public CreateApiKeyCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateApiKeyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("API key creation attempted for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        // Check if user has premium subscription (required for API access)
        // Tier levels: Free=0, Basic=1, Premium=2, Enterprise=3
        if (user.SubscriptionTier?.Level <= 1) // Free or Basic
        {
            _logger.LogWarning("API key creation attempted by user {UserId} without premium subscription", request.UserId);
            throw new ValidationException("API key creation requires Premium or Enterprise subscription");
        }

        // Check existing API key count limits
        var existingKeys = await _unitOfWork.UserApiKeys.GetManyAsync(k => k.UserId == user.Id && k.IsActive, cancellationToken);
        var maxKeys = user.SubscriptionTier?.Level == 2 ? 3 : 10; // Premium (level 2): 3, Enterprise (level 3): 10

        if (existingKeys.Count() >= maxKeys)
        {
            _logger.LogWarning("API key limit exceeded for user {UserId}: {Count}/{Max}", 
                request.UserId, existingKeys.Count(), maxKeys);
            throw new ValidationException($"Maximum number of API keys ({maxKeys}) reached");
        }

        return await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            // Generate secure API key
            var apiKeyValue = GenerateApiKey();
            var hashedKey = HashApiKey(apiKeyValue);

            // Set default scopes if not provided
            var scopes = request.Scopes ?? GetDefaultScopes(user.SubscriptionTier?.Level ?? 0);

            var apiKey = new UserApiKey
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Name = request.Name,
                Description = request.Description,
                KeyHash = hashedKey,
                KeyPrefix = apiKeyValue.Substring(0, 8), // Store first 8 chars for identification
                Scopes = string.Join(",", scopes),
                ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddYears(1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.UserApiKeys.AddAsync(apiKey, cancellationToken);

            // Add domain event
            var scopeEnums = new List<ApiKeyScope>();
            foreach (var scope in scopes)
            {
                if (Enum.TryParse<ApiKeyScope>(scope, true, out var scopeEnum))
                {
                    scopeEnums.Add(scopeEnum);
                }
                else
                {
                    _logger.LogWarning("Invalid API key scope '{Scope}' provided for user {UserId}", scope, user.Id);
                    throw new ValidationException($"Invalid scope: {scope}");
                }
            }
            user.AddDomainEvent(new UserApiKeyCreatedEvent(user.Id, apiKey.Id, apiKey.Name, scopeEnums.ToArray()));
            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("API key created for user {UserId}: {KeyName} ({KeyId})",
                user.Id, request.Name, apiKey.Id);

            var data = new
            {
                apiKeyId = apiKey.Id,
                apiKey = apiKeyValue, // Only returned once during creation
                name = apiKey.Name,
                createdAt = apiKey.CreatedAt,
                expiresAt = apiKey.ExpiresAt,
                scopes = scopes
            };

            return new ResponseModel(HttpStatusCode.OK, "API key created successfully", data);
        }, cancellationToken);
    }

    private static string GenerateApiKey()
    {
        // Generate a cryptographically secure API key
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        
        var apiKey = "usk_" + Convert.ToBase64String(bytes).Replace("/", "_").Replace("+", "-").TrimEnd('=');
        return apiKey;
    }

    private static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashedBytes);
    }

    private static List<string> GetDefaultScopes(int tierLevel)
    {
        return tierLevel switch
        {
            2 => new List<string> { "crawl:basic", "data:read" }, // Premium
            3 => new List<string> { "crawl:basic", "crawl:advanced", "data:read", "data:write", "analytics:read" }, // Enterprise
            _ => new List<string> { "crawl:basic" }
        };
    }
}