using Newtonsoft.Json.Linq;

namespace ReepoBot.Models;

public readonly record struct GitHubEvent(string EventType, string Signature, JToken Payload)
{
}
