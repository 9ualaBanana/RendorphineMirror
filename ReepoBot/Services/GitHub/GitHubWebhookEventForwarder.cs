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

    internal bool SignaturesMatch(JsonElement payload, string signature, string secret)
    {
        using var hmac256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        // GitHubGitHub advises to use UTF-8 for payload deserializing.
        var ourSignature = "sha256=" + BitConverter.ToString(
            hmac256.ComputeHash(Encoding.UTF8.GetBytes(payload.GetRawText()))
            )
            .Replace("-", "").ToLower();

        var matched = ourSignature == signature;
        if (!matched)
        {
            Logger.LogError(
                "Signatures didn't match:\n\t" +
                "Received: {Received}\n\t" +
                "Calculated: {Calculated}", signature, ourSignature);
        }
        return matched;
    }
}
