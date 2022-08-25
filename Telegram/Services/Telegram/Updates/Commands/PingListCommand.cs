using System.Text;
using Telegram.Bot.Types;
using Telegram.Models;
using Telegram.Services.Node;
using Telegram.Services.Telegram;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public class PingListCommand : AuthenticatedCommand
{
    readonly ILogger _logger;
    readonly NodeSupervisor _nodeSupervisor;

    public PingListCommand(ILogger<PingListCommand> logger, TelegramBot bot, TelegramChatIdAuthentication authentication, NodeSupervisor nodeSupervisor)
        : base(logger, bot, authentication)
    {
        _logger = logger;
        _nodeSupervisor = nodeSupervisor;
    }

    public override string Value => "pinglist";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken _)
    {
        _logger.LogDebug("Listing all nodes...");
        var message = ListNodesOrderedByName();
        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, message);
    }

    string ListNodesOrderedByName()
    {
        var messageBuilder = new StringBuilder();

        messageBuilder.AppendHeader("*All Nodes*");
        foreach (var nodeInfo in _nodeSupervisor.AllNodes.OrderBy(node => node.NodeName))
            messageBuilder.Append(nodeInfo.BriefInfoMDv2).AppendLine(GetStatusFor(nodeInfo));

        return messageBuilder.ToString();
    }

    string? GetStatusFor(MachineInfo nodeInfo) =>
        _nodeSupervisor.NodesOffline.Contains(nodeInfo) ? " *--OFFLINE--*" : null;
}
