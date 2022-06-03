using ReepoBot.Services.Telegram;
using System.Text.Json;

namespace ReepoBot.Services.GitHub;

public class PingGitHubWebhookEventForwarder : GitHubWebhookEventForwarder
{
    public PingGitHubWebhookEventForwarder(ILogger<PingGitHubWebhookEventForwarder> logger, TelegramBot bot)
        : base(logger, bot)
    {
    }

    public override Task HandleAsync(JsonElement payload)
    {
        var eventSourceRepo = payload.GetProperty("repository").GetProperty("name");
        var hookId = payload.GetProperty("hook").GetProperty("url").ToString();
        Logger.LogInformation(
            "'ping' event is received from '{Repo}' repository.", eventSourceRepo);

        return Task.CompletedTask;
    }
}
