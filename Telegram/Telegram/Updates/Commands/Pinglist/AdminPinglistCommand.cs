using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;

namespace Telegram.Telegram.Updates.Commands.Pinglist;

public class AdminPinglistCommand : AdminAuthenticatedCommand
{
    readonly UserNodes _userNodes;


    public AdminPinglistCommand(
        ILogger<AuthenticatedCommand> logger,
        TelegramBot bot,
        ChatAuthenticator authenticator,
        UserNodes userNodes) : base(logger, bot, authenticator)
    {
        _userNodes = userNodes;
    }


    public override string Value => "adminpinglist";

    protected override async Task HandleAsyncCore(Update update, ChatAuthenticationToken authenticationToken)
    {
        var messageBuilder = new StringBuilder().AppendHeader(Logic.Header);

        foreach (var theUserNodes in _userNodes)
            messageBuilder.AppendLine(Logic.ListNodesOrderedByName(theUserNodes.Value).ToString());

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, messageBuilder.ToString());
    }
}
