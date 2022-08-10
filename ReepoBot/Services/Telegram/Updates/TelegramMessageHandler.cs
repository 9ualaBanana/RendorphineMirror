using ReepoBot.Services.Telegram.Updates.Commands;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Updates;

public class TelegramMessageHandler
{
    readonly ILogger _logger;

    readonly TelegramBot _bot;
    readonly TelegramCommandHandler _commandHandler;

    public TelegramMessageHandler(
        ILogger<TelegramMessageHandler> logger,
        TelegramBot bot,
        TelegramCommandHandler commandHandler)
    {
        _logger = logger;
        _bot = bot;
        _commandHandler = commandHandler;
    }

    public async Task HandleAsync(Update update)
    {
        var message = update.Message!;
        _logger.LogDebug("Dispatching {Message}...", nameof(Message));
        if (IsCommand(message))
        {
            await _commandHandler.HandleAsync(update);
            return;
        }
        else if (IsSystemMessage(message))
        {
            _logger.LogTrace("System messages are handled by {Handler}", nameof(TelegramChatMemberUpdatedHandler));
            return; // Bot adding and removal are handled via `UpdateType.MyChatMember` updates.
        }
        _logger.LogWarning("The following message couldn't be handled:\n{Message}", message.Text);
    }

    static bool IsCommand(Message message)
    {
        var messageText = message.Text;
        return messageText is not null && messageText.StartsWith('/') && messageText.Length > 1;
    }

    bool IsSystemMessage(Message message)
    {
        return message.LeftChatMember?.Id == _bot.BotId || message.NewChatMembers?.First().Id == _bot.BotId;
    }
}