using ReepoBot.Services.Telegram;

namespace ReepoBot.Services;

public abstract class WebhookEventHandler<TPayload>
{
    protected readonly ILogger<WebhookEventHandler<TPayload>> Logger;
    protected readonly TelegramBot Bot;

    public WebhookEventHandler(ILogger<WebhookEventHandler<TPayload>> logger, TelegramBot bot)
    {
        Logger = logger;
        Bot = bot;
    }

    public abstract Task HandleAsync(TPayload payload);

    protected void LogUnresolvedEvent(object eventType)
    {
        Logger.LogError("'{EventType}' event can't be handled by any handler.", eventType);
    }
}
