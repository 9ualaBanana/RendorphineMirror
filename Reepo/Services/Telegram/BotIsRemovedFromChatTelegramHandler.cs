using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram;

internal class BotIsRemovedFromChatTelegramHandler : TelegramUpdateHandler
{
    public BotIsRemovedFromChatTelegramHandler(
        ILogger<BotIsRemovedFromChatTelegramHandler> logger,
        TelegramBot bot)
        : base(logger, bot)
    {
    }

    public override Task HandleAsync(Update update)
    {
        var chatId = update.MyChatMember!.Chat.Id;
        Bot.Subscriptions.Remove(chatId);
        Logger.LogInformation("Subscriber removed: {id}", chatId);
        return Task.CompletedTask;
    }
}
