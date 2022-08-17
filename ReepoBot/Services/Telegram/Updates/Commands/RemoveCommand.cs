using ReepoBot.Services.Node;
using ReepoBot.Services.Telegram.Authentication;
using System.Text;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Updates.Commands;

public class RemoveCommand : AuthenticatedCommand
{
    readonly NodeSupervisor _nodeSupervisor;

    public RemoveCommand(ILogger<RemoveCommand> logger, TelegramBot bot, TelegramChatIdAuthentication authentication, NodeSupervisor nodeSupervisor)
        : base(logger, bot, authentication)
    {
        _nodeSupervisor = nodeSupervisor;
    }

    public override string Value => "remove";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken _)
    {
        var nodeNames = update.Message!.Text!.QuotedArguments().ToArray();
        int nodesRemoved = _nodeSupervisor.TryRemoveNodesWithNames(nodeNames);

        var message = new StringBuilder().Append($"{nodesRemoved} nodes were removed.");
        if (nodesRemoved > 0) message.Append("Nodes with specified names are either online or not found.");

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, message.ToString());
    }
}
