using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace WebCrawlerService.Infrastructure.Security.AuthorizationHandlers;

public class UserDataAccessHandler : AuthorizationHandler<UserDataAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserDataAccessRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (userIdClaim == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Allow access if user is admin
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if user is trying to access their own data
        if (context.Resource is Guid requestedUserId)
        {
            if (Guid.TryParse(userIdClaim, out var currentUserId) && 
                currentUserId == requestedUserId)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

public class UserDataAccessRequirement : IAuthorizationRequirement
{
}