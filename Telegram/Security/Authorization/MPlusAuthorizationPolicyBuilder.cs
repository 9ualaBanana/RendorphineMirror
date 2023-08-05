using Microsoft.AspNetCore.Authorization;
using Telegram.Security.Authentication;

namespace Telegram.Security.Authorization;

public partial class MPlusAuthorizationPolicyBuilder : AuthorizationPolicyBuilder
{
    internal MPlusAuthorizationPolicyBuilder()
        : base(MPlusAuthenticationDefaults.AuthenticationScheme)
    { RequireAuthenticatedUser(); }

    internal MPlusAuthorizationPolicyBuilder Add(AccessLevelRequirement accessLevelRequirement)
    { AddRequirements(accessLevelRequirement); return this; }

    internal new MPlusAuthorizationPolicyBuilder AddRequirements(params IAuthorizationRequirement[] requirements)
    { base.AddRequirements(requirements); return this; }
}
