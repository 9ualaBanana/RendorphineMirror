using Telegram.MPlus;

namespace Telegram.Persistence;

/// <summary>
/// Entity representing <see cref="MPlusIdentity"/> of the user represented by <see cref="TelegramBotUserEntity"/>.
/// </summary>
public record MPlusIdentityEntity : MPlusIdentity
{
    public TelegramBotUserEntity TelegramBotUser { get; set; } = null!;

    internal MPlusIdentityEntity(MPlusIdentity mPlusIdentity)
        : base(mPlusIdentity)
    {
    }

    internal MPlusIdentityEntity(string userId, string sessionId, AccessLevel accessLevel)
        : base(userId, sessionId, accessLevel)
    {
    }
}
