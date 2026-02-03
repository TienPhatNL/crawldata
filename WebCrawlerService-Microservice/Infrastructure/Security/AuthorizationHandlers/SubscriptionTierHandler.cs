using Microsoft.AspNetCore.Authorization;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Infrastructure.Security.AuthorizationHandlers;

public class SubscriptionTierHandler : AuthorizationHandler<SubscriptionTierRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SubscriptionTierRequirement requirement)
    {
        var tierClaim = context.User.FindFirst("subscription_tier")?.Value;
        
        if (tierClaim == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        if (Enum.TryParse<SubscriptionTier>(tierClaim, out var userTier))
        {
            if ((int)userTier >= (int)requirement.RequiredTier)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

public class SubscriptionTierRequirement : IAuthorizationRequirement
{
    public SubscriptionTier RequiredTier { get; }

    public SubscriptionTierRequirement(SubscriptionTier requiredTier)
    {
        RequiredTier = requiredTier;
    }
}