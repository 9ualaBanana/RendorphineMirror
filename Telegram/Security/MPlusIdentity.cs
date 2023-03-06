using Newtonsoft.Json;
using System.Security.Claims;
using Telegram.Security.Authentication;

namespace Telegram.Security;

/// <summary>
/// Helper for working with M+ <see cref="ClaimsIdentity"/>.
/// </summary>
public record MPlusIdentity(
    [JsonProperty(PropertyName = "userid")] string UserId,
    [JsonProperty(PropertyName = "sessionid")] string SessionId,
    [JsonProperty(PropertyName = "accesslevel")] AccessLevel AccessLevel)
{
    internal const string UserIdClaimType = "UserId";
    internal const string SessionIdClaimType = "SessionId";
    internal const string AccessLevelClaimType = "AccessLevel";

    internal bool IsAdmin => AccessLevel > AccessLevel.User;

    internal static MPlusIdentity CreateFrom(ClaimsPrincipal claimsPrincipal)
        => new(UserIdOf(claimsPrincipal), SessionIdOf(claimsPrincipal), AccessLevelOf(claimsPrincipal));
    internal static MPlusIdentity Create(string userId, string sessionId, int accessLevel)
        => Create(userId, sessionId, (AccessLevel)accessLevel);

    internal static MPlusIdentity Create(string userId, string sessionId, AccessLevel accessLevel)
        => new(userId, sessionId, accessLevel);

    internal static string UserIdOf(ClaimsPrincipal claimsPrincipal) => claimsPrincipal.FindFirstValue(UserIdClaimType);

    internal static string SessionIdOf(ClaimsPrincipal claimsPrincipal) => claimsPrincipal.FindFirstValue(SessionIdClaimType);

    internal static AccessLevel AccessLevelOf(ClaimsPrincipal claimsPrincipal)
        => Enum.Parse<AccessLevel>(claimsPrincipal.FindFirstValue(AccessLevelClaimType), ignoreCase: true);

    internal ClaimsIdentity ToClaimsIdentity() => new(new Claim[]
    {
            new(UserIdClaimType, UserId),
            new(SessionIdClaimType, SessionId),
            new(AccessLevelClaimType, ((int)AccessLevel.User).ToString(), ClaimValueTypes.Integer)
    }, MPlusViaTelegramChatDefaults.AuthenticationScheme);
}
