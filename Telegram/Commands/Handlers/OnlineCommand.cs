using Microsoft.AspNetCore.Authorization;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.MPlus;
using Telegram.Security.Authorization;
using Telegram.Services.Node;

namespace Telegram.Commands.Handlers;

public partial class OnlineCommand : CommandHandler, IAuthorizationPolicyProtected
{
    readonly UserNodes _userNodes;

    public OnlineCommand(
        UserNodes userNodes,
        CommandParser parser,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OnlineCommand> logger)
        : base(parser, bot, httpContextAccessor, logger)
    {
        _userNodes = userNodes;
    }

    internal override Command Target => "online";

    public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder().Build();

    protected override async Task HandleAsync(ParsedCommand receivedCommand)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(MPlusIdentity.UserIdOf(User), out var userNodesSupervisor, Bot, ChatId))
            return;

        var message = BuildMessage(userNodesSupervisor.NodesOnline.Count, userNodesSupervisor.NodesOffline.Count);

        await Bot.SendMessageAsync_(ChatId, message);
    }

    static string BuildMessage(int onlineNodes, int offlineNodes) =>
        $"Online: *{onlineNodes}*\nOffline: {offlineNodes}";
}
