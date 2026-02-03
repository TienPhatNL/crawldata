using System.Net;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Authentication.Commands;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<LogoutCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (request.LogoutAllDevices)
        {
            // Logout from all devices - deactivate all user sessions
            var allUserSessions = await _unitOfWork.UserSessions.GetManyAsync(
                s => s.UserId == request.UserId && s.IsActive,
                cancellationToken);

            foreach (var session in allUserSessions)
            {
                session.IsActive = false;
                session.LoggedOutAt = DateTime.UtcNow;
                session.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.UserSessions.UpdateAsync(session, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} logged out from all devices ({SessionCount} sessions)",
                request.UserId, allUserSessions.Count());

            return new ResponseModel(HttpStatusCode.OK, "Successfully logged out from all devices");
        }
        else
        {
            // Logout from current session only
            UserSession? currentSession = null;
            
            if (!string.IsNullOrEmpty(request.AccessToken))
            {
                // Find session by access token
                currentSession = await _unitOfWork.UserSessions.GetAsync(
                    s => s.SessionToken == request.AccessToken && s.UserId == request.UserId && s.IsActive,
                    cancellationToken);
            }
            
            if (currentSession == null)
            {
                // If no specific session found, deactivate the most recent active session
                var userSessions = await _unitOfWork.UserSessions.GetManyAsync(
                    s => s.UserId == request.UserId && s.IsActive,
                    cancellationToken);
                
                currentSession = userSessions
                    .OrderByDescending(s => s.LastActivityAt)
                    .FirstOrDefault();
            }

            if (currentSession != null)
            {
                currentSession.IsActive = false;
                currentSession.LoggedOutAt = DateTime.UtcNow;
                currentSession.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.UserSessions.UpdateAsync(currentSession, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User {UserId} logged out from session {SessionId}", 
                    request.UserId, currentSession.Id);
            }
            else
            {
                _logger.LogWarning("No active session found for user {UserId} during logout", request.UserId);
            }

            return new ResponseModel(HttpStatusCode.OK, "Successfully logged out");
        }
    }
}