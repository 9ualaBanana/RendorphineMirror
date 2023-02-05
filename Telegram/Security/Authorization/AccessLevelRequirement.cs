using Microsoft.AspNetCore.Authorization;

namespace Telegram.Security.Authorization;

public class AccessLevelRequirement : IAuthorizationRequirement
{
    internal readonly AccessLevel AccessLevel;

    internal AccessLevelRequirement(AccessLevel accessLevel)
    {
        AccessLevel = accessLevel;
    }
}
