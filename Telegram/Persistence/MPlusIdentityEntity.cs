using Telegram.Bot.Types;
using Telegram.MPlus.Security;

namespace Telegram.Persistence;

/// <summary>
/// Entity representing <see cref="MPlusIdentity"/> of the user represented by <see cref="TelegramBotUserEntity"/>.
/// </summary>
public record MPlusIdentityEntity : MPlusIdentity
{
    public TelegramBotUserEntity TelegramBotUser { get; set; } = null!;
    /// <remarks>
    /// Expicitly defined Foreign Key which is also Primary Key
    /// because one can be logged in only as one user at a time,
    /// there will always be only one record of <see cref="MPlusIdentityEntity"/>
    /// in the database with a given <see cref="TelegramBotUserChatId"/> as records
    /// of logged out users are removed.
    /// </remarks>
    public ChatId TelegramBotUserChatId { set; get; } = null!;

    internal MPlusIdentityEntity(MPlusIdentity mPlusIdentity)
        : base(mPlusIdentity)
    {
    }

    internal MPlusIdentityEntity(string userId, string sessionId, AccessLevel accessLevel)
        : base(userId, sessionId, accessLevel)
    {
    }
}
