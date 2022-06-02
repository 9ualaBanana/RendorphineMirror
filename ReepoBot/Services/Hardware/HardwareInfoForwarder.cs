using ReepoBot.Services.Telegram;
using System.Runtime.Versioning;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ReepoBot.Services.Hardware;

public class HardwareInfoForwarder : WebhookEventHandler<string>
{
    public HardwareInfoForwarder(ILogger<HardwareInfoForwarder> logger, TelegramBot bot)
        : base(logger, bot)
    {
    }

    [SupportedOSPlatform("windows")]
    public override async Task HandleAsync(string hardwareInfoMessage)
    {
        Logger.LogDebug("Forwarding the hardware info message to Telegram subsribers...");
        foreach (var subscriber in Bot.Subscriptions)
        {
            await Bot.SendTextMessageAsync(
                subscriber, hardwareInfoMessage,
                parseMode: ParseMode.MarkdownV2);
        }
        Logger.LogDebug("Hardware info message is forwarded.");
    }
}
