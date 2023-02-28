using Telegram.Bot.Types;

namespace Telegram.Security.Authentication.Persistence;

/// <summary>
/// Principal entity with which other entities representing information related to this <see cref="ChatId"/> are associated.
/// </summary>
public record TelegramBotUserEntity(ChatId ChatId)
{
    public MPlusIdentityEntity? MPlusIdentity { get; set; }
}
