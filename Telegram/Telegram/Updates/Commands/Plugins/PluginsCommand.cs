using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Commands;
using Telegram.Models;
using Telegram.Services.Node;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;

namespace Telegram.Telegram.Updates.Commands.Plugins;

public class PluginsCommand : AuthenticatedCommand
{
    readonly UserNodes _userNodes;


    public PluginsCommand(ILogger<PluginsCommand> logger, TelegramBot bot, ChatAuthenticator authenticator, UserNodes userNodes)
        : base(logger, bot, authenticator)
    {
        _userNodes = userNodes;
    }


    public override string Value => "plugins";

    protected override async Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(authenticationToken, out var userNodesSupervisor, Bot, authenticationToken.ChatId))
            return;

        var nodeNamesWhosePluginsToShow = update.Message!.Text!.QuotedArguments();

        var message = Logic.ListInstalledPluginsFor(nodeNamesWhosePluginsToShow, userNodesSupervisor).ToString();

        await Bot.SendMessageAsync_(update.Message!.Chat.Id, message);
    }
}
