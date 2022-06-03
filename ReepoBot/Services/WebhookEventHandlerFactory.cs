using ReepoBot.Services.Telegram;

namespace ReepoBot.Services;

public abstract class WebhookEventHandlerFactory<THandler, TEvent>
{
    protected readonly ILogger<WebhookEventHandlerFactory<THandler, TEvent>> Logger;
    protected readonly ILoggerFactory LoggerFactory;
    protected readonly TelegramBot Bot;

    public WebhookEventHandlerFactory(
        ILogger<WebhookEventHandlerFactory<THandler, TEvent>> logger,
        ILoggerFactory loggerFactory,
        TelegramBot bot)
    {
        Logger = logger;
        LoggerFactory = loggerFactory;
        Bot = bot;
    }

    public abstract THandler? Resolve(TEvent eventType);

    protected void LogUnresolvedEvent(object eventType)
    {
        Logger.LogError("'{EventType}' event can't be handled by any {Handler}.", eventType, typeof(THandler).Name);
    }
}
