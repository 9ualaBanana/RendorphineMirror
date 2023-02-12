using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates.Commands;

namespace Telegram.Telegram.Updates.Commands.Offline;

public class AdminOfflineCommand : AdminAuthenticatedCommand
{
    readonly UserNodes _userNodes;


    public AdminOfflineCommand(
        ILogger<AuthenticatedCommand> logger,
        TelegramBot bot,
        ChatAuthenticator authenticator,
        UserNodes userNodes) : base(logger, bot, authenticator)
    {
        _userNodes = userNodes;
    }


    public override string Value => "adminoffline";

    protected override async Task HandleAsyncCore(Update update, ChatAuthenticationToken authenticationToken)
    {
        var messageBuilder = new StringBuilder().AppendHeader(Logic.Header);

        foreach (var theUserNodes in _userNodes)
            messageBuilder.AppendLine(Logic.ListOfflineNodes(theUserNodes.Value).ToString());

        await Bot.SendMessageAsync_(update.Message!.Chat.Id, messageBuilder.ToString());
    }
}
