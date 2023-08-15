using GIBS.Authorization;
using GIBS.Commands;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Models;
using Telegram.MPlus.Security;
using Telegram.Security.Authorization;
using Telegram.Services.Node;

namespace Telegram.Commands.Handlers;

public partial class PingCommand : CommandHandler, IAuthorizationPolicyProtected
{
    readonly UserNodes _userNodes;

    public PingCommand(
        UserNodes userNodes,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PingCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _userNodes = userNodes;
    }

    public override Command Target => CommandFactory.Create("ping");

    public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder().Build();

    protected override async Task HandleAsync(Command receivedCommand)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(MPlusIdentity.UserIdOf(User), out var userNodesSupervisor, Bot, ChatId))
            return;

        var messageBuilder = new StringBuilder().AppendHeader(Header);

        var nodeNames = receivedCommand.QuotedArguments.Select(nodeName => nodeName.ToLowerInvariant()).ToHashSet();
        messageBuilder.AppendLine(ListOnlineNodes(userNodesSupervisor, nodeNames).ToString());

        await Bot.SendMessageAsync_(ChatId, messageBuilder.ToString());
    }

    const string Header = "*Node* | *Uptime*";

    static StringBuilder ListOnlineNodes(NodeSupervisor nodeSupervisor, HashSet<string> nodeNames)
    {
        IEnumerable<MachineInfo> onlineNodesToList = nodeSupervisor.NodesOnline;

        if (nodeNames.Any())
            onlineNodesToList = onlineNodesToList.Where(
                nodeInfo => nodeNames.Any(nodeName => nodeInfo.NodeName.ToLowerInvariant().Contains(nodeName))
                );

        return ListSpecifiedOnlineNodes(nodeSupervisor, onlineNodesToList);
    }

    static StringBuilder ListSpecifiedOnlineNodes(NodeSupervisor userNodesSupervisor, IEnumerable<MachineInfo> onlineNodesToList)
    {
        var messageBuilder = new StringBuilder();

        foreach (var nodeInfo in onlineNodesToList.OrderBy(nodeOnline => nodeOnline.NodeName))
        {
            if (userNodesSupervisor.UptimeOf(nodeInfo, out var uptime))
                messageBuilder.AppendLine($"{nodeInfo.BriefInfoMDv2} | {uptime:d\\.hh\\:mm}");
        }

        return messageBuilder;
    }
}
