using ReepoBot.Services;
using ReepoBot.Services.Telegram;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ReepoBot.Services.GitHub;

public abstract class GitHubWebhookEventForwarder : WebhookEventHandler<JsonElement>
{
    protected GitHubWebhookEventForwarder(ILogger<GitHubWebhookEventForwarder> logger, TelegramBot bot)
        : base(logger, bot)
    {
    }

    internal bool HasMatchingSignature(JsonElement payload, string signature)
    {
        var secret = Environment.GetEnvironmentVariable("GITHUB_SECRET", EnvironmentVariableTarget.User)!;
        using var hmac256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        // GitHub advises to use UTF-8 for payload deserializing.
        var ourSignature = "sha256=" + BitConverter.ToString(
            hmac256.ComputeHash(Encoding.UTF8.GetBytes(payload.GetRawText()))
            )
            .Replace("-", "").ToLower();

        var matched = ourSignature == signature;
        if (!matched)
        {
            Logger.LogError(
                "Signatures didn't match:\n\t" +
                "Received: {received}\n\t" +
                "Calculated: {calculated}", signature, ourSignature);
        }
        return matched;
    }
}
