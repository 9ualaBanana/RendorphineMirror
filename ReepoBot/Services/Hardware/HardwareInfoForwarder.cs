using ReepoBot.Services.Telegram;
using System.Runtime.Versioning;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ReepoBot.Services.Hardware;

// Implement strategies for different verbosity output.
// This forwarder will act as the dispatcher to outputter for  different OS with different verbosity level.

public class HardwareInfoForwarder : WebhookEventHandler<string>
{
    public HardwareInfoForwarder(ILogger<HardwareInfoForwarder> logger, TelegramBot bot)
        : base(logger, bot)
    {
    }

    [SupportedOSPlatform("windows")]
    public override async Task HandleAsync(string hardwareInfoMessage)
    {
        foreach (var subscriber in Bot.Subscriptions)
        {
            await Bot.SendTextMessageAsync(
                subscriber, hardwareInfoMessage,
                parseMode: ParseMode.MarkdownV2);
        }
    }
}
