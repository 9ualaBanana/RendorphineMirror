using ReepoBot.Models;

namespace ReepoBot.Services.GitHub;

public interface IGitHubWebhookEventHandler : IEventHandler<GitHubWebhookEvent>
{
}
