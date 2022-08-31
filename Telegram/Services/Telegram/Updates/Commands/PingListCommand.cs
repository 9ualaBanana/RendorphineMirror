using System.Text;
using Telegram.Bot.Types;
using Telegram.Models;
using Telegram.Services.Node;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public class PingListCommand : AuthenticatedCommand
{
    readonly ILogger _logger;
    readonly UserNodes _userNodes;



    public PingListCommand(ILogger<PingListCommand> logger, TelegramBot bot, TelegramChatIdAuthenticator authenticator, UserNodes userNodes)
        : base(logger, bot, authenticator)
    {
        _logger = logger;
        _userNodes = userNodes;
    }



    public override string Value => "pinglist";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken authenticationToken)
    {
        _logger.LogDebug("Listing all nodes...");

        if (!_userNodes.TryGetUserNodeSupervisor(authenticationToken, out var userNodesSupervisor, Bot, authenticationToken.ChatId))
            return;
        var message = ListNodesOrderedByName(userNodesSupervisor);
        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, message);
    }

    string ListNodesOrderedByName(NodeSupervisor userNodesSupervisor)
    {
        var messageBuilder = new StringBuilder();

        messageBuilder.AppendHeader("*All Nodes*");
        foreach (var nodeInfo in userNodesSupervisor.AllNodes.OrderBy(node => node.NodeName))
            messageBuilder.Append(nodeInfo.BriefInfoMDv2).AppendLine(GetStatusFor(nodeInfo, userNodesSupervisor));

        return messageBuilder.ToString();
    }

    string? GetStatusFor(MachineInfo nodeInfo, NodeSupervisor userNodesSupervisor) =>
        userNodesSupervisor.NodesOffline.Contains(nodeInfo) ? " *--OFFLINE--*" : null;
}
