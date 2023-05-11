using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Models;
using Telegram.MPlus;
using Telegram.Security.Authorization;
using Telegram.Services.Node;

namespace Telegram.Commands.Handlers;

public partial class PingListCommand : CommandHandler, IAuthorizationPolicyProtected
{
    readonly UserNodes _userNodes;

    public PingListCommand(
        UserNodes userNodes,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PingListCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _userNodes = userNodes;
    }

    internal override Command Target => CommandFactory.Create("pinglist");

    public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder().Build();

    protected override async Task HandleAsync(Command receivedCommand)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(MPlusIdentity.UserIdOf(User), out var userNodesSupervisor, Bot, ChatId))
            return;
        var messageBuilder = new StringBuilder().AppendHeader(Header);

        messageBuilder.AppendLine(ListNodesOrderedByName(userNodesSupervisor).ToString());

        await Bot.SendMessageAsync_(ChatId, messageBuilder.ToString());
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
