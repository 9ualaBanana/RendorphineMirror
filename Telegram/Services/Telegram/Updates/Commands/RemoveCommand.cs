using System.Text;
using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public class RemoveCommand : AuthenticatedCommand
{
    readonly UserNodes _userNodes;



    public RemoveCommand(ILogger<RemoveCommand> logger, TelegramBot bot, ChatAuthenticator authenticator, UserNodes userNodes)
        : base(logger, bot, authenticator)
    {
        _userNodes = userNodes;
    }



    public override string Value => "remove";

    protected override async Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken)
    {
        var nodeNames = update.Message!.Text!.QuotedArguments().ToArray();
        if (!_userNodes.TryGetUserNodeSupervisor(authenticationToken, out var userNodesSupervisor, Bot, authenticationToken.ChatId))
            return;
        int nodesRemoved = userNodesSupervisor.TryRemoveNodesWithNames(nodeNames);

        var message = new StringBuilder().Append($"{nodesRemoved} nodes were removed.");
        if (nodesRemoved > 0) message.Append("Nodes with specified names are either online or not found.");

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, message.ToString());
    }
}
