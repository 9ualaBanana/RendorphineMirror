using System.Text.Json;

namespace ReepoBot.Models;

public readonly record struct GitHubEvent(string EventType, string Signature, JsonElement Payload)
{
}
