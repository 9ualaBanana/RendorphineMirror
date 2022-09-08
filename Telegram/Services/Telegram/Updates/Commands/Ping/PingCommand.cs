using System.Text;
using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands.Ping;

public class PingCommand : AuthenticatedCommand
{
    readonly UserNodes _userNodes;


    public PingCommand(ILogger<PingCommand> logger, TelegramBot bot, ChatAuthenticator authenticator, UserNodes userNodes)
        : base(logger, bot, authenticator)
    {
        _userNodes = userNodes;
    }


    public override string Value => "ping";

    protected override async Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(authenticationToken, out var userNodesSupervisor, Bot, authenticationToken.ChatId))
            return;

        var messageBuilder = new StringBuilder().AppendHeader(Logic.Header);

        var nodeNames = update.Message!.Text!.QuotedArguments().Select(nodeName => nodeName.CaseInsensitive()).ToHashSet();
        messageBuilder.AppendLine(Logic.ListOnlineNodes(userNodesSupervisor, nodeNames).ToString());

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, messageBuilder.ToString());
    }
}
