using ReepoBot.Models;

namespace ReepoBot.Services.GitHub;

public class PingGitHubEventForwarder
{
    readonly ILogger _logger;

    public PingGitHubEventForwarder(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PingGitHubEventForwarder>();
    }

    public void Handle(GitHubEvent gitHubEvent)
    {
        var eventSourceRepo = gitHubEvent.Payload["repository"]!["name"]!.ToString();
        _logger.LogInformation(
            "'ping' event is received from '{Repo}' repository.", eventSourceRepo);
    }
}
