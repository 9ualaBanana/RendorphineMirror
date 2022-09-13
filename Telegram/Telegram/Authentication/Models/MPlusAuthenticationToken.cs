namespace Telegram.Telegram.Authentication.Models;

public record MPlusAuthenticationToken(string UserId, string SessionId, AccessLevel AccessLevel)
{
    internal bool IsAdmin => AccessLevel > AccessLevel.User;
}
