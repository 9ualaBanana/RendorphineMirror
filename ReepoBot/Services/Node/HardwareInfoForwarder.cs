using ReepoBot.Services.Telegram;
using System.Runtime.Versioning;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ReepoBot.Services.Node;

public class HardwareInfoForwarder : IEventHandler<string>
{
    readonly ILogger<HardwareInfoForwarder> _logger;
    readonly TelegramBot _bot;

    public HardwareInfoForwarder(ILogger<HardwareInfoForwarder> logger, TelegramBot bot)
    {
        _logger = logger;
        _bot = bot;
    }

    [SupportedOSPlatform("windows")]
    public async Task HandleAsync(string hardwareInfoMessage)
    {
        _logger.LogDebug("Forwarding the hardware info message to Telegram subscribers...");
        foreach (var subscriber in _bot.Subscriptions)
        {
            try
            {
                await _bot.SendTextMessageAsync(
                subscriber,
                hardwareInfoMessage,
                parseMode: ParseMode.MarkdownV2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The message couldn't be sent to subscriber ({Subscriber}):\n{Message}", subscriber, hardwareInfoMessage);
            }
        }
        _logger.LogDebug("Hardware info message is forwarded.");
    }
}
