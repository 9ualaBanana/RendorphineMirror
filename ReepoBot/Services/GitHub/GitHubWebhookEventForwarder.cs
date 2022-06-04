using ReepoBot.Models;
using ReepoBot.Services.Telegram;
using System.Security.Cryptography;
using System.Text;

namespace ReepoBot.Services.GitHub;

public class GitHubWebhookEventForwarder : IGitHubWebhookEventHandler
{
    readonly ILoggerFactory _loggerFactory;
    readonly ILogger<GitHubWebhookEventForwarder> _logger;
    readonly TelegramBot _bot;

    public GitHubWebhookEventForwarder(ILoggerFactory loggerFactory, TelegramBot bot)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<GitHubWebhookEventForwarder>();
        _bot = bot;
    }

    public async Task HandleAsync(GitHubWebhookEvent githubEvent)
    {
        switch (githubEvent.EventType)
        {
            case "ping":
                await new PingGitHubWebhookEventForwarder(_loggerFactory).HandleAsync(githubEvent);
                break;
            case "push":
                await new PushGitHubWebhookEventForwarder(_loggerFactory, _bot).HandleAsync(githubEvent);
                break;
        }
    }

    internal bool SignaturesMatch(GitHubWebhookEvent gitHubEvent, string secret)
    {
        using var hmac256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        // GitHubGitHub advises to use UTF-8 for payload deserializing.
        var ourSignature = "sha256=" + BitConverter.ToString(
            hmac256.ComputeHash(Encoding.UTF8.GetBytes(gitHubEvent.Payload.GetRawText()))
            )
            .Replace("-", "").ToLower();

        var matched = ourSignature == gitHubEvent.Signature;
        if (!matched)
        {
            _logger.LogError(
               @"Signatures didn't match:\n\t
                 Received: {Received}\n\t
                 Calculated: {Calculated}", gitHubEvent.Signature, ourSignature);
        }
        return matched;
    }
}
