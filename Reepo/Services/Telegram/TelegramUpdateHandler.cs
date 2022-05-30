using ReepoBot.Services;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram;

public abstract class TelegramUpdateHandler : WebhookEventHandler<Update>
{
    public TelegramUpdateHandler(ILogger<TelegramUpdateHandler> logger, TelegramBot bot)
        : base(logger, bot)
    {
    }
}
