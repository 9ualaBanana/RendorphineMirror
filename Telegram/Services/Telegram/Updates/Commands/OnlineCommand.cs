using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public class OnlineCommand : AuthenticatedCommand
{
    readonly NodeSupervisor _nodeSupervisor;

    public OnlineCommand(ILogger<OnlineCommand> logger, TelegramBot bot, TelegramChatIdAuthentication authentication, NodeSupervisor nodeSupervisor)
        : base(logger, bot, authentication)
    {
        _nodeSupervisor = nodeSupervisor;
    }

    public override string Value => "online";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken _)
    {
        var message = $"Online: *{_nodeSupervisor.NodesOnline.Count}*\nOffline: {_nodeSupervisor.NodesOffline.Count}";
        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, message);
    }
}
