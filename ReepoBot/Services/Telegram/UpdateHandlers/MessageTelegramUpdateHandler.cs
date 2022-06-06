using ReepoBot.Services.Node;
using ReepoBot.Services.Telegram.UpdateTypeHandlers;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.UpdateHandlers;

internal class MessageTelegramUpdateHandler : ITelegramUpdateHandler
{
    readonly ILoggerFactory _loggerFactory;
    readonly ILogger _logger;
    readonly TelegramBot _bot;
    readonly NodeSupervisor _nodeSupervisor;

    internal MessageTelegramUpdateHandler(ILoggerFactory loggerFactory, TelegramBot bot, NodeSupervisor nodeSupervisor)
    {
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<MessageTelegramUpdateHandler>();
        _bot = bot;
        _nodeSupervisor = nodeSupervisor;
    }

    public async Task HandleAsync(Update update)
    {
        _logger.LogDebug("Dispatching text message...");
        var message = update.Message!;
        if (IsCommand(message))
        {
            await new CommandTelegramUpdateHandler(_loggerFactory, _bot, _nodeSupervisor).HandleAsync(update);
            return;
        }
        if (IsSystem(message))
        {
            _logger.LogDebug("System messages are handled by {Handler}", nameof(MyChatMemberTelegramUpdateHandler));
            return;    // Bot adding and removal are handled via `UpdateType.MyChatMember` updates.
        }
        _logger.LogWarning("The following message couldn't be handled:\n{Message}", message.Text);
    }

    static bool IsCommand(Message message)
    {
        var messageText = message.Text;
        return messageText is not null && messageText.StartsWith('/') && messageText.Length > 1;
    }

    bool IsSystem(Message message)
    {
        return message.LeftChatMember?.Id == _bot.BotId || message.NewChatMembers?.First().Id == _bot.BotId;
    }
}
