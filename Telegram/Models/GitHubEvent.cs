using Newtonsoft.Json.Linq;

namespace Telegram.Models;

public readonly record struct GitHubEvent(string EventType, string Signature, JObject Payload)
{
}
