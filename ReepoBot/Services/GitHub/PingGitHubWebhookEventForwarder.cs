using ReepoBot.Models;

namespace ReepoBot.Services.GitHub;

public class PingGitHubWebhookEventForwarder : IGitHubWebhookEventHandler
{
    readonly ILogger _logger;

    public PingGitHubWebhookEventForwarder(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PingGitHubWebhookEventForwarder>();
    }

    public Task HandleAsync(GitHubWebhookEvent gitHubEvent)
    {
        var eventSourceRepo = gitHubEvent.Payload.GetProperty("repository").GetProperty("name");
        _logger.LogInformation(
            "'ping' event is received from '{Repo}' repository.", eventSourceRepo);

        return Task.CompletedTask;
    }
}
