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

public partial class PingCommand : CommandHandler, IAuthorizationRequirementsProvider
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

    public IEnumerable<IAuthorizationRequirement> Requirements { get; }
        = IAuthorizationRequirementsProvider.Provide(MPlusAuthenticationRequirement.Instance);

    internal override Command Target => "ping";

    protected override async Task HandleAsync(ParsedCommand receivedCommand, HttpContext context)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(MPlusIdentity.UserIdOf(context.User), out var userNodesSupervisor, Bot, Update.Message!.Chat.Id))
            return;

        var messageBuilder = new StringBuilder().AppendHeader(Header);

        var nodeNames = receivedCommand.QuotedArguments.Select(nodeName => nodeName.CaseInsensitive()).ToHashSet();
        messageBuilder.AppendLine(ListOnlineNodes(userNodesSupervisor, nodeNames).ToString());

        await Bot.SendMessageAsync_(Update.Message.Chat.Id, messageBuilder.ToString());
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
