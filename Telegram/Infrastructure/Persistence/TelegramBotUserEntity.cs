using System.Diagnostics.CodeAnalysis;
using Telegram.Bot.Types;

namespace Telegram.Infrastructure.Persistence;

/// <summary>
/// Principal entity with which other entities representing information related to this <see cref="ChatId"/> are associated.
/// </summary>
public record TelegramBotUserEntity(ChatId ChatId)
{
    [MemberNotNullWhen(true, nameof(MPlusIdentity))]
    internal bool IsAuthenticatedByMPlus => MPlusIdentity is not null;

    public MPlusIdentityEntity? MPlusIdentity { get; set; }
}
