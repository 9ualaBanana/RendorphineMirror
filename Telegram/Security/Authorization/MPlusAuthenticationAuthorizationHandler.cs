using Microsoft.AspNetCore.Authorization;
using Telegram.Security.Authentication;

namespace Telegram.Security.Authorization;

public class MPlusAuthenticationAuthorizationHandler : AuthorizationHandler<MPlusAuthenticationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MPlusAuthenticationRequirement requirement)
    {
        if (context.User.Identity?.AuthenticationType == MPlusAuthenticationViaTelegramChatDefaults.AuthenticationScheme)
            context.Succeed(requirement);
        else context.Fail(new(handler: this, $"Current request is not authenticated by M+."));

        return Task.CompletedTask;
    }
}
