using Newtonsoft.Json.Linq;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure.Bot;
using Telegram.Models;

namespace Telegram.Services.GitHub;

public class PushGitHubEventForwarder
{
    readonly ILogger _logger;
    readonly TelegramBot _bot;

    public PushGitHubEventForwarder(ILoggerFactory loggerFactory, TelegramBot bot)
    {
        _logger = loggerFactory.CreateLogger<PushGitHubEventForwarder>();
        _bot = bot;
    }

    public async Task HandleAsync(GitHubEvent githubEvent)
    {
        var payload = githubEvent.Payload;
        var repo = payload["repository"]!;
        var sender = payload["sender"]!;
        var diff = payload["compare"]!;
        var commitMessages = GetCommitMessages(payload["commits"]!);
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
        textBuilder.AppendLine($"*{sender["login"]}* made *{commitMessages.Count()}* new push(es) to *{repo["name"]}*:");
        foreach (var commitMessage in commitMessages)
        {
            textBuilder.AppendLine($"   {randomMarker()} {commitMessage.Replace("*", @"\*")}");
        }
        var text = textBuilder.ToString();

        var replyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithUrl($"{repo["name"]}".Sanitize(), repo["html_url"]!.ToString()),
                InlineKeyboardButton.WithUrl($"{sender["login"]}".Sanitize(), sender["html_url"]!.ToString())
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithUrl("Diff", diff.ToString())
            }
        });

        foreach (var subscriber in Subscriptions._)
            await _bot.SendMessageAsync_(subscriber, text, replyMarkup: replyMarkup);
    }

    static IEnumerable<string> GetCommitMessages(JToken commits)
    {
        return commits.ToArray().Select(commit => commit["message"]!.ToString());
    }
}
