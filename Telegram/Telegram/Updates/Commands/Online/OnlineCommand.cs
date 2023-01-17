using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates.Commands;

namespace Telegram.Telegram.Updates.Commands.Online;

public class OnlineCommand : AuthenticatedCommand
{
    readonly UserNodes _userNodes;


    public OnlineCommand(ILogger<OnlineCommand> logger, TelegramBot bot, ChatAuthenticator authenticator, UserNodes userNodes)
        : base(logger, bot, authenticator)
    {
        _userNodes = userNodes;
    }


    public override string Value => "online";

    protected override async Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(authenticationToken, out var userNodesSupervisor, Bot, update.Message!.Chat.Id))
            return;

        var message = Logic.BuildMessage(userNodesSupervisor.NodesOnline.Count, userNodesSupervisor.NodesOffline.Count);

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, message);
    }
}
