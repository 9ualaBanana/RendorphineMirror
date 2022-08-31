using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public class OnlineCommand : AuthenticatedCommand
{
    readonly UserNodes _userNodes;

    public OnlineCommand(ILogger<OnlineCommand> logger, TelegramBot bot, TelegramChatIdAuthenticator authenticator, UserNodes userNodes)
        : base(logger, bot, authenticator)
    {
        _userNodes = userNodes;
    }



    public override string Value => "online";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken authenticationToken)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(authenticationToken, out var userNodesSupervisor, Bot, update.Message!.Chat.Id))
            return;
        var message = $"Online: *{userNodesSupervisor.NodesOnline.Count}*\nOffline: {userNodesSupervisor.NodesOffline.Count}";
        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, message);
    }
}
