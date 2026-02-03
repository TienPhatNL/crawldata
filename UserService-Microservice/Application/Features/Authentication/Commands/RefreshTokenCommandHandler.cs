using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;

namespace UserService.Application.Features.Authentication.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Find session by refresh token
        var session = await _unitOfWork.UserSessions.GetAsync(
            s => s.RefreshToken == request.RefreshToken && s.IsActive,
            cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Invalid refresh token provided");
            throw new ValidationException("Invalid refresh token");
        }

        // Check if refresh token has expired
        if (session.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Expired refresh token used for session {SessionId}", session.Id);
            
            // Deactivate expired session
            session.IsActive = false;
            session.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.UserSessions.UpdateAsync(session, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            throw new ValidationException("Refresh token has expired");
        }

        // Get user
        var user = await _unitOfWork.Users.GetByIdAsync(session.UserId, cancellationToken);
        if (user == null || user.IsDeleted || !user.CanLogin)
        {
            _logger.LogWarning("Token refresh attempted for invalid user {UserId}", session.UserId);
            
            // Deactivate session for invalid user
            session.IsActive = false;
            session.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.UserSessions.UpdateAsync(session, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            throw new ValidationException("User account is no longer valid");
        }

        // Generate new tokens
        var claims = _jwtTokenService.GetUserClaims(user.Id, user.Email, user.Role.ToString(), user.SubscriptionTier?.Name ?? "Free");
        var newAccessToken = _jwtTokenService.GenerateAccessToken(claims);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var tokenExpires = DateTime.UtcNow.AddMinutes(60);

        // Update session with new tokens
        session.SessionToken = newAccessToken;
        session.RefreshToken = newRefreshToken;
        session.ExpiresAt = tokenExpires;
        session.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7); // Extend refresh token
        session.LastActivityAt = DateTime.UtcNow;
        session.IpAddress = request.IpAddress ?? session.IpAddress;
        session.UserAgent = request.UserAgent ?? session.UserAgent;
        session.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.UserSessions.UpdateAsync(session, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);

        var data = new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken,
            tokenExpires = tokenExpires,
            userId = user.Id,
            email = user.Email,
            role = user.Role.ToString(),
            subscriptionTier = user.SubscriptionTier?.Name ?? "Free"
        };

        return new ResponseModel(HttpStatusCode.OK, "Token refreshed successfully", data);
    }
}