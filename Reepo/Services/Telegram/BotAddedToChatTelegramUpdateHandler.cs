using Telegram.Bot;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram;

public class BotAddedToChatTelegramUpdateHandler : TelegramUpdateHandler
{
    public BotAddedToChatTelegramUpdateHandler(ILogger<BotAddedToChatTelegramUpdateHandler> logger, TelegramBot bot)
        : base(logger, bot)
    {
    }

    public override async Task HandleAsync(Update update)
    {
        var chatId = update.MyChatMember!.Chat.Id;
        Bot.Subscriptions.Add(chatId);
        await Bot.SendTextMessageAsync(chatId, "You are subscribed to events now. Remove me from the chat to unsubscribe.");
        Logger.LogInformation("Subscriber added: {id}", chatId);
    }
}
