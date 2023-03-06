using Microsoft.AspNetCore.Authorization;

namespace Telegram.Security.Authorization;

public class AccessLevelAuthorizationHandler : AuthorizationHandler<AccessLevelRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AccessLevelRequirement requirement)
    {
        var userAccessLevel = MPlusIdentity.AccessLevelOf(context.User);
        if (userAccessLevel >= requirement.AccessLevel)
            context.Succeed(requirement);
        else context.Fail(new(handler: this, $"Current user doesn't have required {nameof(AccessLevel)}."));

        return Task.CompletedTask;
    }
}
