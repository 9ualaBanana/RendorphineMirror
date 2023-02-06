using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;

namespace Telegram.Telegram.Updates.Commands.Plugins;

public class AdminPluginsCommand : AdminAuthenticatedCommand
{
    readonly UserNodes _userNodes;


    public AdminPluginsCommand(
        ILogger<AuthenticatedCommand> logger,
        TelegramBot bot,
        ChatAuthenticator authenticator,
        UserNodes userNodes) : base(logger, bot, authenticator)
    {
        _userNodes = userNodes;
    }


    public override string Value => "adminplugins";

    protected override async Task HandleAsyncCore(Update update, ChatAuthenticationToken authenticationToken)
    {
        var messageBuilder = new StringBuilder();

        var nodeNamesWhosePluginsToShow = update.Message!.Text!.QuotedArguments();
        foreach (var theUserNodes in _userNodes)
            messageBuilder.AppendLine(
                Logic.ListInstalledPluginsFor(nodeNamesWhosePluginsToShow, theUserNodes.Value).ToString());

        if (messageBuilder.Length == 0) messageBuilder.AppendLine("No plugins are installed on the specified nodes.");

        await Bot.SendMessageAsync_(update.Message!.Chat.Id, messageBuilder.ToString());
    }
}
