using System.Text;
using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Services.Telegram;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public class OfflineCommand : AuthenticatedCommand
{
    readonly NodeSupervisor _nodeSupervisor;

    public OfflineCommand(ILogger<OfflineCommand> logger, TelegramBot bot, TelegramChatIdAuthentication authentication, NodeSupervisor nodeSupervisor)
        : base(logger, bot, authentication)
    {
        _nodeSupervisor = nodeSupervisor;
    }

    public override string Value => "offline";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken _)
    {
        Logger.LogDebug("Listing offline nodes...");

        var message = ListOfflineNodes();

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, message);
    }

    string ListOfflineNodes()
    {
        var messageBuilder = new StringBuilder();

        messageBuilder.AppendHeader("*Offline Nodes*");
        foreach (var nodeInfo in _nodeSupervisor.NodesOffline.OrderBy(node => node.NodeName))
            messageBuilder.AppendLine(nodeInfo.BriefInfoMDv2);

        return messageBuilder.ToString();
    }
}
