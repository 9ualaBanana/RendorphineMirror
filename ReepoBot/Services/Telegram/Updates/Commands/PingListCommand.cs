using ReepoBot.Models;
using ReepoBot.Services.Node;
using ReepoBot.Services.Telegram.Authentication;
using System.Text;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Updates.Commands;

public class PingListCommand : Command
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

    public override bool RequiresAuthentication => true;

    protected override async Task HandleAsyncCore(Update update)
    {
        _logger.LogDebug("Listing all nodes...");
        var message = ListNodesOrderedByName();
        _ = Bot.TrySendMessageAsync(update.Message!.Chat.Id, message, _logger);
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
