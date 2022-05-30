using ReepoBot.Services.Telegram;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace ReepoBot.Services.GitHub;

public class PushGitHubWebhookEventForwarder : GitHubWebhookEventForwarder
{
    public PushGitHubWebhookEventForwarder(ILogger<PushGitHubWebhookEventForwarder> logger, TelegramBot bot)
        : base(logger, bot)
    {
    }

    public override async Task HandleAsync(JsonElement payload)
    {
        var repo = payload.GetProperty("repository");
        var sender = payload.GetProperty("sender");
        var diff = payload.GetProperty("compare");
        var commitMessages = GetCommitMessages(payload.GetProperty("commits"));

        var textBuilder = new StringBuilder();
        textBuilder.AppendLine($"{sender.GetProperty("login")} made {commitMessages.Count()} new push(es) to {repo.GetProperty("name")}:");
        foreach (var commitMessage in commitMessages)
        {
            textBuilder.AppendLine(commitMessage);
        }
        var text = textBuilder.ToString();

        var replyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithUrl("Diff", diff.ToString())
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithUrl("Repo", repo.GetProperty("html_url").ToString()),
                InlineKeyboardButton.WithUrl("User", sender.GetProperty("html_url").ToString())
            }
        });

        foreach (var subscriber in Bot.Subscriptions)
        {
            await Bot.SendTextMessageAsync(new(subscriber), text, replyMarkup: replyMarkup);
        }
    }

    static IEnumerable<string> GetCommitMessages(JsonElement commits)
    {
        return commits.EnumerateArray().Select(commit => commit.GetProperty("message").ToString());
    }
}
