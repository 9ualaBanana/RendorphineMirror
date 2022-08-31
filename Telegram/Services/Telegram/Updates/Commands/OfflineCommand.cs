using System.Text;
using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Services.Telegram.Authentication;
namespace Telegram.Services.Telegram.Updates.Commands;

public class OfflineCommand : AuthenticatedCommand
{
    readonly UserNodes _userNodes;



    public OfflineCommand(ILogger<OfflineCommand> logger, TelegramBot bot, TelegramChatIdAuthenticator authenticator, UserNodes userNodes)
        : base(logger, bot, authenticator)
    {
        _userNodes = userNodes;
    }



    public override string Value => "offline";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken authenticationToken)
    {
        Logger.LogDebug("Listing offline nodes...");

        if (!_userNodes.TryGetUserNodeSupervisor(authenticationToken, out var userNodesSupervisor, Bot, authenticationToken.ChatId))
            return;

        var message = ListOfflineNodes(userNodesSupervisor);

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, message);
    }

    string ListOfflineNodes(NodeSupervisor userNodesSupervisor)
    {
        var messageBuilder = new StringBuilder();

        messageBuilder.AppendHeader("*Offline Nodes*");
        foreach (var nodeInfo in userNodesSupervisor.NodesOffline.OrderBy(node => node.NodeName))
            messageBuilder.AppendLine(nodeInfo.BriefInfoMDv2);

        return messageBuilder.ToString();
    }
}
