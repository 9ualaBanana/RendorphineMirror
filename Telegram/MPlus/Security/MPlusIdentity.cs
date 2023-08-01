using Newtonsoft.Json;
using System.Security.Claims;
using Telegram.Security.Authentication;

namespace Telegram.MPlus.Security;

/// <summary>
/// Helper for working with M+ <see cref="ClaimsIdentity"/>.
/// </summary>
public partial record MPlusIdentity(
    [JsonProperty(PropertyName = MPlusIdentity.EmailClaimType)] string Email,
    [JsonProperty(PropertyName = MPlusIdentity.UserIdClaimType)] string UserId,
    [JsonProperty(PropertyName = MPlusIdentity.SessionIdClaimType)] string SessionId,
    [JsonProperty(PropertyName = MPlusIdentity.AccessLevelClaimType)] AccessLevel AccessLevel)
{
    internal bool IsAdmin => AccessLevel > AccessLevel.User;

    #region ClaimAccessors

    internal static string EmailOf(ClaimsPrincipal claimsPrincipal) => claimsPrincipal.FindFirstValue(EmailClaimType)!;

    internal static string UserIdOf(ClaimsPrincipal claimsPrincipal) => claimsPrincipal.FindFirstValue(UserIdClaimType)!;

    internal static string SessionIdOf(ClaimsPrincipal claimsPrincipal) => claimsPrincipal.FindFirstValue(SessionIdClaimType)!;

    internal static AccessLevel AccessLevelOf(ClaimsPrincipal claimsPrincipal)
        => Enum.Parse<AccessLevel>(claimsPrincipal.FindFirstValue(AccessLevelClaimType)!, ignoreCase: true);

    #endregion

    internal ClaimsIdentity ToClaimsIdentity(string? issuer = null) => new(new Claim[]
    {
        new(EmailClaimType, Email, ClaimValueTypes.Email, issuer),
        new(UserIdClaimType, UserId, default, issuer),
        new(SessionIdClaimType, SessionId, default, issuer),
        new(AccessLevelClaimType, ((int)AccessLevel).ToString(), ClaimValueTypes.Integer, issuer)
    }, MPlusAuthenticationDefaults.AuthenticationScheme);

    #region ClaimTypes

    const string EmailClaimType = "email";
    const string UserIdClaimType = "userid";
    const string SessionIdClaimType = "sessionid";
    const string AccessLevelClaimType = "accesslevel";

    #endregion
}
