using Newtonsoft.Json;

namespace Telegram.MPlus.Security;

internal record MPlusPublicSessionInfo(
    [JsonProperty(PropertyName = "sid")] string SessionId,
    [JsonProperty(PropertyName = "guid")] string Guid,
    [JsonProperty(PropertyName = "userid")] string UserId,
    [JsonProperty(PropertyName = "email")] string Email,
    [JsonProperty(PropertyName = "adminlevel")] AccessLevel AccessLevel)
{
}

static class MPlusPublicSessionInfoExtensions
{
    internal static MPlusIdentity ToMPlusIdentity(this MPlusPublicSessionInfo publicSessionInfo)
        => new(publicSessionInfo.UserId, publicSessionInfo.SessionId, publicSessionInfo.AccessLevel);
}
