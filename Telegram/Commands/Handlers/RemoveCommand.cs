using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.MPlus;
using Telegram.Security.Authorization;
using Telegram.Services.Node;

namespace Telegram.Commands.Handlers;

public class RemoveCommand : CommandHandler, IAuthorizationRequirementsProvider
{
    readonly UserNodes _userNodes;

    public RemoveCommand(
        UserNodes userNodes,
        CommandParser parser,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RemoveCommand> logger)
        : base(parser, bot, httpContextAccessor, logger)
    {
        _userNodes = userNodes;
    }

    public IEnumerable<IAuthorizationRequirement> Requirements { get; }
        = IAuthorizationRequirementsProvider.Provide(MPlusAuthenticationRequirement.Instance);

    internal override Command Target => "remove";

    protected override async Task HandleAsync(ParsedCommand receivedCommand, HttpContext context)
    {
        var nodeNames = receivedCommand.QuotedArguments.ToArray();
        if (!_userNodes.TryGetUserNodeSupervisor(MPlusIdentity.UserIdOf(context.User), out var userNodesSupervisor, Bot, Update.Message!.Chat.Id))
            return;
        int nodesRemoved = userNodesSupervisor.TryRemoveNodesWithNames(nodeNames);

        var message = new StringBuilder().Append($"{nodesRemoved} nodes were removed.");
        if (nodesRemoved > 0) message.Append("Nodes with specified names are either online or not found.");

        await Bot.SendMessageAsync_(Update.Message.Chat.Id, message.ToString());
    }
}
