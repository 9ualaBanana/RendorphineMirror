using System.Text;
using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Telegram;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;

namespace Telegram.Services.Telegram.Updates.Commands.Pinglist;

public class PingListCommand : AuthenticatedCommand
{
    readonly ILogger _logger;
    readonly UserNodes _userNodes;


    public PingListCommand(ILogger<PingListCommand> logger, TelegramBot bot, ChatAuthenticator authenticator, UserNodes userNodes)
        : base(logger, bot, authenticator)
    {
        _logger = logger;
        _userNodes = userNodes;
    }


    public override string Value => "pinglist";

    protected override async Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(authenticationToken, out var userNodesSupervisor, Bot, authenticationToken.ChatId))
            return;

        var messageBuilder = new StringBuilder().AppendHeader(Logic.Header);

        messageBuilder.AppendLine(Logic.ListNodesOrderedByName(userNodesSupervisor).ToString());

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, messageBuilder.ToString());
    }
}
