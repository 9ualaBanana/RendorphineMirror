namespace Telegram.Services.Telegram.Authentication;

public record MPlusAuthenticationToken(string UserId, string SessionId, AccessLevel AccessLevel)
{
    internal bool IsAdmin => AccessLevel > AccessLevel.User;
}
