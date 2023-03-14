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

public partial class PingListCommand : CommandHandler, IAuthorizationRequirementsProvider
{
    readonly UserNodes _userNodes;

    public PingListCommand(
        UserNodes userNodes,
        CommandParser parser,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PingListCommand> logger)
        : base(parser, bot, httpContextAccessor, logger)
    {
        _userNodes = userNodes;
    }

    public IEnumerable<IAuthorizationRequirement> Requirements { get; }
        = IAuthorizationRequirementsProvider.Provide(MPlusAuthenticationRequirement.Instance);

    internal override Command Target => "pinglist";

    protected override async Task HandleAsync(ParsedCommand receivedCommand, HttpContext context)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(MPlusIdentity.UserIdOf(context.User), out var userNodesSupervisor, Bot, Update.Message!.Chat.Id))
            return;
        var messageBuilder = new StringBuilder().AppendHeader(Header);

        messageBuilder.AppendLine(ListNodesOrderedByName(userNodesSupervisor).ToString());

        await Bot.SendMessageAsync_(Update.Message.Chat.Id, messageBuilder.ToString());
    }

    const string Header = "*All Nodes*";

    static StringBuilder ListNodesOrderedByName(NodeSupervisor userNodesSupervisor)
    {
        var messageBuilder = new StringBuilder();

        foreach (var nodeInfo in userNodesSupervisor.AllNodes.OrderBy(node => node.NodeName))
            messageBuilder.Append(nodeInfo.BriefInfoMDv2).AppendLine(GetStatusFor(nodeInfo, userNodesSupervisor));

        return messageBuilder;
    }

    static string? GetStatusFor(MachineInfo nodeInfo, NodeSupervisor userNodesSupervisor)
        => userNodesSupervisor.NodesOffline.Contains(nodeInfo) ? " *--OFFLINE--*" : null;
}
