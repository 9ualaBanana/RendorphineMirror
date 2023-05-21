using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.MPlus.Security;
using Telegram.Security.Authorization;
using Telegram.Services.Node;

namespace Telegram.Commands.Handlers;

public class RemoveCommand : CommandHandler, IAuthorizationPolicyProtected
{
    readonly UserNodes _userNodes;

    public RemoveCommand(
        UserNodes userNodes,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RemoveCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _userNodes = userNodes;
    }

    internal override Command Target => CommandFactory.Create("remove");

    public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder().Build();

    protected override async Task HandleAsync(Command receivedCommand)
    {
        var nodeNames = receivedCommand.QuotedArguments.ToArray();
        if (!_userNodes.TryGetUserNodeSupervisor(MPlusIdentity.UserIdOf(User), out var userNodesSupervisor, Bot, ChatId))
            return;
        int nodesRemoved = userNodesSupervisor.TryRemoveNodesWithNames(nodeNames);

        var message = new StringBuilder().Append($"{nodesRemoved} nodes were removed.");
        if (nodesRemoved > 0) message.Append("Nodes with specified names are either online or not found.");

        await Bot.SendMessageAsync_(ChatId, message.ToString());
    }
}
