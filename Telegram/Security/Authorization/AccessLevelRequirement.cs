using Microsoft.AspNetCore.Authorization;
using Telegram.MPlus;

namespace Telegram.Security.Authorization;

public class AccessLevelRequirement : IAuthorizationRequirement
{
    internal readonly AccessLevel AccessLevel;

    internal static AccessLevelRequirement Admin = new(AccessLevel.Admin);

    internal AccessLevelRequirement(AccessLevel accessLevel)
    {
        AccessLevel = accessLevel;
    }
}
