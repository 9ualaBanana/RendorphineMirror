using System.Text.Json;

namespace ReepoBot.Models;

public readonly record struct GitHubWebhookEvent(string EventType, string Signature, JsonElement Payload)
{
}
