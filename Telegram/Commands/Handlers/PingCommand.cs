using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.Models;
using Telegram.MPlus;
using Telegram.Security.Authorization;
using Telegram.Services.Node;

namespace Telegram.Commands.Handlers;

public partial class PingCommand : CommandHandler, IAuthorizationPolicyProtected
{
    readonly UserNodes _userNodes;

    public PingCommand(
        UserNodes userNodes,
        CommandParser parser,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PingCommand> logger)
        : base(parser, bot, httpContextAccessor, logger)
    {
        _userNodes = userNodes;
    }

    internal override Command Target => "ping";

    public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder().Build();

    protected override async Task HandleAsync(ParsedCommand receivedCommand)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(MPlusIdentity.UserIdOf(User), out var userNodesSupervisor, Bot, ChatId))
            return;

        var messageBuilder = new StringBuilder().AppendHeader(Header);

        var nodeNames = receivedCommand.QuotedArguments.Select(nodeName => nodeName.CaseInsensitive()).ToHashSet();
        messageBuilder.AppendLine(ListOnlineNodes(userNodesSupervisor, nodeNames).ToString());

        await Bot.SendMessageAsync_(ChatId, messageBuilder.ToString());
    }

    const string Header = "*Node* | *Uptime*";

    static StringBuilder ListOnlineNodes(NodeSupervisor nodeSupervisor, HashSet<string> nodeNames)
    {
        IEnumerable<MachineInfo> onlineNodesToList = nodeSupervisor.NodesOnline;

        if (nodeNames.Any())
            onlineNodesToList = onlineNodesToList.Where(
                nodeInfo => nodeNames.Any(nodeName => nodeInfo.NodeName.CaseInsensitive().Contains(nodeName))
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
