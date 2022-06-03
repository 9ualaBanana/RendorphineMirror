using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram;

internal class BotIsRemovedFromChatTelegramUpdateHandler : TelegramUpdateHandler
{
    public BotIsRemovedFromChatTelegramUpdateHandler(
        ILogger<BotIsRemovedFromChatTelegramUpdateHandler> logger,
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
