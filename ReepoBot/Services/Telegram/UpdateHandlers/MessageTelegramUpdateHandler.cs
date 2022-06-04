using ReepoBot.Services.Node;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.UpdateHandlers;

internal class MessageTelegramUpdateHandler : ITelegramUpdateHandler
{
    readonly ILoggerFactory _loggerFactory;
    readonly TelegramBot _bot;
    readonly NodeSupervisor _nodeSupervisor;

    internal MessageTelegramUpdateHandler(ILoggerFactory loggerFactory, TelegramBot bot, NodeSupervisor nodeSupervisor)
    {
        _loggerFactory = loggerFactory;
        _bot = bot;
        _nodeSupervisor = nodeSupervisor;
    }

    public async Task HandleAsync(Update update)
    {
        var message = update.Message!;
        if (message.LeftChatMember?.Id == _bot.BotId || message.NewChatMembers?.First().Id == _bot.BotId)
        {
            return;    // Bot adding and removal are handled via `UpdateType.MyChatMember` updates.
        }
        if (message.Text is not null && message.Text.StartsWith('/'))
        {
            await new CommandTelegramUpdateHandler(_loggerFactory, _bot, _nodeSupervisor).HandleAsync(update);
        }
    }
}