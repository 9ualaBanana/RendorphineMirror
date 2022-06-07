using ReepoBot.Models;
using ReepoBot.Services.Telegram;

namespace ReepoBot.Services.GitHub;

public class GitHubEventForwarder : IGitHubEventHandler
{
    readonly ILoggerFactory _loggerFactory;
    readonly ILogger<GitHubEventForwarder> _logger;
    readonly TelegramBot _bot;

    public GitHubEventForwarder(ILoggerFactory loggerFactory, TelegramBot bot)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<GitHubEventForwarder>();
        _bot = bot;
    }

    public async Task HandleAsync(GitHubEvent githubEvent)
    {
        _logger.LogDebug("Dispatching GitHub event with {Type} type...", githubEvent.EventType);
        switch (githubEvent.EventType)
        {
            case "ping":
                await new PingGitHubEventForwarder(_loggerFactory).HandleAsync(githubEvent);
                break;
            case "push":
                await new PushGitHubEventForwarder(_loggerFactory, _bot).HandleAsync(githubEvent);
                break;
            default:
                _logger.LogDebug("GitHub event with {Type} type couldn't be handled", githubEvent.EventType);
                break;
        }
    }

    // This piece of shit doesn't work on linux for some reason.
    //internal bool SignaturesMatch(GitHubEvent gitHubEvent, string secret)
    //{
    //    using var hmac256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    //    var rawJsonPayload = gitHubEvent.Payload.ToString(Formatting.None);
    //    // GitHubGitHub advises to use UTF-8 for payload deserializing.
    //    var ourSignature = "sha256=" + BitConverter.ToString(
    //        hmac256.ComputeHash(Encoding.UTF8.GetBytes(rawJsonPayload))
    //        )
    //        .Replace("-", "").ToLower();

    //    var matched = ourSignature == gitHubEvent.Signature;
    //    if (!matched)
    //    {
    //        _logger.LogError(
    //           "Signatures didn't match:\n\tReceived: {Received}\n\tCalculated: {Calculated}",
    //           gitHubEvent.Signature, ourSignature);
    //    }
    //    return matched;
    //}
}
