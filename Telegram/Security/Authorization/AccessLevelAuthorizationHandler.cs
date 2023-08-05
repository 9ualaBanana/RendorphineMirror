using Microsoft.AspNetCore.Authorization;
using Telegram.MPlus.Security;
using static Telegram.Security.Authorization.MPlusAuthorizationPolicyBuilder;

namespace Telegram.Security.Authorization;

public class AccessLevelAuthorizationHandler : AuthorizationHandler<AccessLevelRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AccessLevelRequirement requirement)
    {
        if (!context.HasFailed)
        {
            var userAccessLevel = MPlusIdentity.AccessLevelOf(context.User);
            if (userAccessLevel >= requirement.AccessLevel)
                context.Succeed(requirement);
            else context.Fail(new(handler: this,
                $"User doesn't have required {nameof(AccessLevel)} ({userAccessLevel}:{(int)userAccessLevel}).")
                );
        }

        return Task.CompletedTask;
    }
}
