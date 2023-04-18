﻿using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.MPlus;
using Telegram.Security.Authorization;
using Telegram.Services.Node;

namespace Telegram.Commands.Handlers;

public partial class OfflineCommand : CommandHandler, IAuthorizationRequirementsProvider
{
    readonly UserNodes _userNodes;

    public OfflineCommand(
        UserNodes userNodes,
        CommandParser parser,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OfflineCommand> logger)
        : base(parser, bot, httpContextAccessor, logger)
    {
        _userNodes = userNodes;
    }

    public IEnumerable<IAuthorizationRequirement> Requirements { get; }
        = IAuthorizationRequirementsProvider.Provide(MPlusAuthenticationRequirement.Instance);

    internal override Command Target => "offline";

    protected override async Task HandleAsync(ParsedCommand receivedCommand)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(MPlusIdentity.UserIdOf(User), out var userNodesSupervisor, Bot, ChatId))
            return;

        var messageBuilder = new StringBuilder().AppendHeader(Header);

        messageBuilder.AppendLine(ListOfflineNodes(userNodesSupervisor).ToString());

        await Bot.SendMessageAsync_(ChatId, messageBuilder.ToString());
    }

    const string Header = "*Offline Nodes*";

    static StringBuilder ListOfflineNodes(NodeSupervisor userNodesSupervisor)
    {
        var messageBuilder = new StringBuilder();

        foreach (var nodeInfo in userNodesSupervisor.NodesOffline.OrderBy(node => node.NodeName))
            messageBuilder.AppendLine(nodeInfo.BriefInfoMDv2);

        return messageBuilder;
    }
}
