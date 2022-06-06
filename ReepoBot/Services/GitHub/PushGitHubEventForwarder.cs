﻿using Newtonsoft.Json.Linq;
using ReepoBot.Models;
using ReepoBot.Services.Telegram;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramHelper;

namespace ReepoBot.Services.GitHub;

public class PushGitHubEventForwarder : IGitHubEventHandler
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
        textBuilder.AppendLine($"*{sender["login"]}* made *{commitMessages.Count()}* new push(es) to *{repo["name"]}*:".Sanitize());
        foreach (var commitMessage in commitMessages)
        {
            textBuilder.AppendLine($"   {randomMarker()} {commitMessage}");
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

        foreach (var subscriber in _bot.Subscriptions)
        {
            await _bot.SendTextMessageAsync(
                new(subscriber),
                text,
                replyMarkup: replyMarkup,
                parseMode: ParseMode.MarkdownV2);
        }
    }

    static IEnumerable<string> GetCommitMessages(JToken commits)
    {
        return commits.ToArray().Select(commit => commit["message"]!.ToString().Sanitize());
    }
}
