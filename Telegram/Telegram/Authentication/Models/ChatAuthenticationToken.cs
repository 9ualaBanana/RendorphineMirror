using Telegram.Bot.Types;

namespace Telegram.Telegram.Authentication.Models;

public class ChatAuthenticationToken : IEquatable<ChatAuthenticationToken>
{
    public ChatId ChatId { get; private set; } = default!;
    public MPlusAuthenticationToken MPlus { get; private set; } = default!;


    private ChatAuthenticationToken()
    {
    }

    public ChatAuthenticationToken (ChatId chatId, MPlusAuthenticationToken mPlus)
    {
        ChatId = chatId;
        MPlus = mPlus;
    }


    #region Equality
    public override bool Equals(object? obj) => Equals(obj as ChatAuthenticationToken);

    public bool Equals(ChatAuthenticationToken? other) => ChatId == other?.ChatId;

    public override int GetHashCode() => ChatId.GetHashCode();
    #endregion
}
