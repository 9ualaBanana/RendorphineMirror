using ReepoBot.Services.Node;
using ReepoBot.Services.Telegram.UpdateTypeHandlers;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ReepoBot.Services.Telegram.UpdateHandlers;

public class TelegramUpdateHandler : ITelegramUpdateHandler
{
    readonly ILoggerFactory _loggerFactory;
    readonly ILogger _logger;
    readonly TelegramBot _bot;
    readonly NodeSupervisor _nodeSupervisor;

    public TelegramUpdateHandler(
        ILoggerFactory loggerFactory,
        TelegramBot bot,
        NodeSupervisor nodeSupervisor)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<TelegramUpdateHandler>();
        _nodeSupervisor = nodeSupervisor;
        _bot = bot;
    }

    public async Task HandleAsync(Update update)
    {
        switch (update.Type)
        {
            case UpdateType.MyChatMember:
                await new MyChatMemberTelegramUpdateHandler(_loggerFactory, _bot).HandleAsync(update);
                break;
            case UpdateType.Message:
                await new MessageTelegramUpdateHandler(_loggerFactory, _bot, _nodeSupervisor).HandleAsync(update);
                break;
            default:
                _logger.LogWarning("{type} update couldn't be handled by {handler}", update.Type, nameof(TelegramUpdateHandler));
                break;
        }
    }
}
