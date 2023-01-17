using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;

namespace Telegram.Telegram.Updates.Commands.Ping;

public class AdminPingCommand : AdminAuthenticatedCommand
{
    readonly UserNodes _userNodes;


    public AdminPingCommand(
        ILogger<AdminPingCommand> logger,
        TelegramBot bot,
        ChatAuthenticator authenticator,
        UserNodes userNodes) : base(logger, bot, authenticator)
    {
        _userNodes = userNodes;
    }


    public override string Value => "adminping";

    protected override async Task HandleAsyncCore(Update update, ChatAuthenticationToken authenticationToken)
    {
        var messageBuilder = new StringBuilder().AppendHeader(Logic.Header);

        var nodeNames = update.Message!.Text!.QuotedArguments().Select(nodeName => nodeName.CaseInsensitive()).ToHashSet();
        foreach (var theUserNodes in _userNodes)
            messageBuilder.AppendLine(Logic.ListOnlineNodes(theUserNodes.Value, nodeNames).ToString());

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, messageBuilder.ToString());
    }
}
