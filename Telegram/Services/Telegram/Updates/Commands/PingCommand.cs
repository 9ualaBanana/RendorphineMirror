using System.Text;
using Telegram.Bot.Types;
using Telegram.Models;
using Telegram.Services.Node;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public class PingCommand : AuthenticatedCommand
{
    readonly UserNodes _userNodes;


    public PingCommand(ILogger<PingCommand> logger, TelegramBot bot, TelegramChatIdAuthenticator authenticator, UserNodes userNodes)
        : base(logger, bot, authenticator)
    {
        _userNodes = userNodes;
    }

    public override string Value => "ping";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken authenticationToken)
    {
        Logger.LogDebug("Listing online nodes...");

        if (!_userNodes.TryGetUserNodeSupervisor(authenticationToken, out var userNodesSupervisor, Bot, authenticationToken.ChatId))
            return;
        IEnumerable<MachineInfo> onlineNodesToList = userNodesSupervisor.NodesOnline;
        var nodeNames = update.Message!.Text!.QuotedArguments().Select(nodeName => nodeName.CaseInsensitive()).ToHashSet();

        if (nodeNames.Any())
            onlineNodesToList = onlineNodesToList.Where(
                nodeInfo => nodeNames.Any(nodeName => nodeInfo.NodeName.CaseInsensitive().Contains(nodeName))
                );
        var message = ListOnlineNodes(userNodesSupervisor, onlineNodesToList);

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, message);
    }

    string ListOnlineNodes(NodeSupervisor userNodesSupervisor, IEnumerable<MachineInfo> onlineNodesToList)
    {
        var messageBuilder = new StringBuilder();

        messageBuilder.AppendHeader("*Node* | *Uptime*");
        foreach (var nodeInfo in onlineNodesToList.OrderBy(nodeOnline => nodeOnline.NodeName))
        {
            var uptime = userNodesSupervisor.UptimeOf(nodeInfo);

            var escapedUptime = $"{uptime:d\\.hh\\:mm}";
            messageBuilder.AppendLine($"{nodeInfo.BriefInfoMDv2} | {escapedUptime}");
        }

        return messageBuilder.ToString();
    }
}
