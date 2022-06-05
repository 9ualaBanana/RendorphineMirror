using ReepoBot.Models;

namespace ReepoBot.Services.GitHub;

public class PingGitHubEventForwarder : IGitHubEventHandler
{
    readonly ILogger _logger;

    public PingGitHubEventForwarder(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PingGitHubEventForwarder>();
    }

    public Task HandleAsync(GitHubEvent gitHubEvent)
    {
        var eventSourceRepo = gitHubEvent.Payload.GetProperty("repository").GetProperty("name");
        _logger.LogInformation(
            "'ping' event is received from '{Repo}' repository.", eventSourceRepo);

        return Task.CompletedTask;
    }
}
