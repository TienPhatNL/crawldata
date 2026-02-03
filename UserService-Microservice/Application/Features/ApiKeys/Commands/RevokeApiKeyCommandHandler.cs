using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Domain.Events;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.ApiKeys.Commands;

public class RevokeApiKeyCommandHandler : IRequestHandler<RevokeApiKeyCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevokeApiKeyCommandHandler> _logger;

    public RevokeApiKeyCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RevokeApiKeyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("API key revocation attempted for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        var apiKey = await _unitOfWork.UserApiKeys.GetByIdAsync(request.ApiKeyId, cancellationToken);
        if (apiKey == null)
        {
            _logger.LogWarning("API key revocation attempted for non-existent key {ApiKeyId}", request.ApiKeyId);
            throw new ValidationException("API key not found");
        }

        // Verify the API key belongs to the user
        if (apiKey.UserId != request.UserId)
        {
            _logger.LogWarning("User {UserId} attempted to revoke API key {ApiKeyId} belonging to another user", 
                request.UserId, request.ApiKeyId);
            throw new ValidationException("API key not found");
        }

        // Check if already revoked
        if (!apiKey.IsActive)
        {
            _logger.LogWarning("Attempted to revoke already inactive API key {ApiKeyId}", request.ApiKeyId);
            throw new ValidationException("API key is already revoked");
        }

        // Revoke the API key
        var revokedAt = DateTime.UtcNow;
        apiKey.IsActive = false;
        apiKey.RevokedAt = revokedAt;
        apiKey.UpdatedAt = revokedAt;

        await _unitOfWork.UserApiKeys.UpdateAsync(apiKey, cancellationToken);

        // Add domain event
        user.AddDomainEvent(new UserApiKeyRevokedEvent(user.Id, user.Email, apiKey.Id, apiKey.Name));
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("API key revoked for user {UserId}: {KeyName} ({KeyId})",
            user.Id, apiKey.Name, apiKey.Id);

        var data = new
        {
            keyName = apiKey.Name,
            revokedAt = revokedAt
        };

        return new ResponseModel(HttpStatusCode.OK, "API key revoked successfully", data);
    }
}