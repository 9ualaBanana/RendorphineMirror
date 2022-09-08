using System.Text;
using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands.Offline;

public class OfflineCommand : AuthenticatedCommand
{
    readonly UserNodes _userNodes;



    public OfflineCommand(ILogger<OfflineCommand> logger, TelegramBot bot, ChatAuthenticator authenticator, UserNodes userNodes)
        : base(logger, bot, authenticator)
    {
        _userNodes = userNodes;
    }



    public override string Value => "offline";

    protected override async Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(authenticationToken, out var userNodesSupervisor, Bot, authenticationToken.ChatId))
            return;

        var messageBuilder = new StringBuilder().AppendHeader(Logic.Header);

        messageBuilder.AppendLine(Logic.ListOfflineNodes(userNodesSupervisor).ToString());

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, messageBuilder.ToString());
    }
}
