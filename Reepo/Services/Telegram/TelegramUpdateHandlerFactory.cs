using ReepoBot.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ReepoBot.Services.Telegram;

public class TelegramUpdateHandlerFactory : WebhookEventHandlerFactory<TelegramUpdateHandler, Update>
{
    public TelegramUpdateHandlerFactory(
        ILogger<TelegramUpdateHandlerFactory> logger,
        ILoggerFactory loggerFactory,
        TelegramBot bot) : base(logger, loggerFactory, bot)
    {
    }

    public override TelegramUpdateHandler? Resolve(Update update)
    {
        switch (update.Type)
        {
            case UpdateType.MyChatMember:
                return ResolveMyChatMember(update);
            case UpdateType.Message:
                return ResolveMessage(update);
            default:
                throw new ArgumentException("Unknown update type.", update.Type.ToString());
        }
    }

    TelegramUpdateHandler? ResolveMyChatMember(Update update)
    {
        if (BotIsAddedToChat(update))
        {
            return new BotAddedToChatTelegramUpdateHandler(
                LoggerFactory.CreateLogger<BotAddedToChatTelegramUpdateHandler>(), Bot);
        }
        if (BotIsRemovedFromChat(update))
        {
            return new BotIsRemovedFromChatTelegramHandler(
                LoggerFactory.CreateLogger<BotIsRemovedFromChatTelegramHandler>(), Bot);
        }
        return null;
    }

    TelegramUpdateHandler? ResolveMessage(Update update)
    {
        if (update.Message!.LeftChatMember?.Id == Bot.BotId || update.Message!.NewChatMembers?.First().Id == Bot.BotId)
            return null;    // Bot adding and removal are handled via `UpdateType.MyChatMember` updates.
        return null;
    }

    bool IsServiceMessage(Update update)
    {
        if (update.Type is not UpdateType.Message) return false;

        return false;
    }

    private bool BotIsRemovedFromChat(Update update)
    {
        if (update.Type is not UpdateType.MyChatMember) return false;

        var newChatMember = update.MyChatMember!.NewChatMember;
        return newChatMember.User.Id == Bot.BotId && !IsAddedToChat(newChatMember);
    }

    bool BotIsAddedToChat(Update update)
    {
        if (update.Type is not UpdateType.MyChatMember) return false;

        var newChatMember = update.MyChatMember!.NewChatMember;
        var oldChatMember = update.MyChatMember!.OldChatMember;
        // Doesn't match when the bot is being promoted.
        return newChatMember.User.Id == Bot.BotId && IsAddedToChat(newChatMember) && !IsAddedToChat(oldChatMember);
    }

    static bool IsAddedToChat(ChatMember chatMember)
    {
        return chatMember.Status is not ChatMemberStatus.Left && chatMember.Status is not ChatMemberStatus.Kicked;
    }
}
