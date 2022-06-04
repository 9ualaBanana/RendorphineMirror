using ReepoBot.Models;
using ReepoBot.Services.Telegram;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramHelper;

namespace ReepoBot.Services.GitHub;

public class PushGitHubWebhookEventForwarder : IGitHubWebhookEventHandler
{
    readonly ILogger _logger;
    readonly TelegramBot _bot;

    public PushGitHubWebhookEventForwarder(ILoggerFactory loggerFactory, TelegramBot bot)
    {
        _logger = loggerFactory.CreateLogger<PushGitHubWebhookEventForwarder>();
        _bot = bot;
    }

    public async Task HandleAsync(GitHubWebhookEvent githubEvent)
    {
        var payload = githubEvent.Payload;
        var repo = payload.GetProperty("repository");
        var sender = payload.GetProperty("sender");
        var diff = payload.GetProperty("compare");
        var commitMessages = GetCommitMessages(payload.GetProperty("commits"));
        string[] markers = new string[] { "🟠", "🟡", "🔴", "🟢", "🔵", "🟣" };
        var random = new Random();
        var previousMarker = markers.First();
        var randomMarker = () =>
        {
            string result;
            while ((result = markers[random.Next(markers.Length)]) == previousMarker)
            {
                continue;
            }
            return result;
        };

        var textBuilder = new StringBuilder();
        textBuilder.AppendLine($"*{sender.GetProperty("login")}* made *{commitMessages.Count()}* new push(es) to *{repo.GetProperty("name")}*:".Sanitize());
        foreach (var commitMessage in commitMessages)
        {
            textBuilder.AppendLine($"   {randomMarker()} {commitMessage}");
        }
        var text = textBuilder.ToString();

        var replyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithUrl($"{repo.GetProperty("name")}".Sanitize(), repo.GetProperty("html_url").ToString()),
                InlineKeyboardButton.WithUrl($"{sender.GetProperty("login")}".Sanitize(), sender.GetProperty("html_url").ToString())
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithUrl("Diff", diff.ToString())
            }
        });

        foreach (var subscriber in _bot.Subscriptions)
        {
            await _bot.SendTextMessageAsync(
                new(subscriber),
                text,
                replyMarkup: replyMarkup,
                parseMode: ParseMode.MarkdownV2);
        }
    }

    static IEnumerable<string> GetCommitMessages(JsonElement commits)
    {
        return commits.EnumerateArray().Select(commit => commit.GetProperty("message").ToString().Sanitize());
    }
}
