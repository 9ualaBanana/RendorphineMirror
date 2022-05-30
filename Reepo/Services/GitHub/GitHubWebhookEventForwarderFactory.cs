using ReepoBot.Services.Telegram;

namespace ReepoBot.Services.GitHub;

public class GitHubWebhookEventForwarderFactory : WebhookEventHandlerFactory<GitHubWebhookEventForwarder, string>
{
    public GitHubWebhookEventForwarderFactory(
        ILogger<GitHubWebhookEventForwarderFactory> logger,
        ILoggerFactory loggerFactory,
        TelegramBot bot) : base(logger, loggerFactory, bot)
    {
    }

    public override GitHubWebhookEventForwarder? Resolve(string eventType)
    {
        switch (eventType)
        {
            case "ping":
                return new PingGitHubWebhookEventForwarder(
                    LoggerFactory.CreateLogger<PingGitHubWebhookEventForwarder>(), Bot);
            case "push":
                return new PushGitHubWebhookEventForwarder(
                    LoggerFactory.CreateLogger<PushGitHubWebhookEventForwarder>(), Bot);
            default:
                LogUnresolvedEvent(eventType);
                return null;
        }
    }
}
