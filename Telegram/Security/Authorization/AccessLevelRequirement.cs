using Microsoft.AspNetCore.Authorization;
using Telegram.MPlus;

namespace Telegram.Security.Authorization;

public partial class MPlusAuthorizationPolicyBuilder
{
    public class AccessLevelRequirement : IAuthorizationRequirement
    {
        internal readonly AccessLevel AccessLevel;

        internal AccessLevelRequirement(AccessLevel accessLevel)
        {
            AccessLevel = accessLevel;
        }

        internal static AccessLevelRequirement Admin = new(AccessLevel.Admin);
    }
}
