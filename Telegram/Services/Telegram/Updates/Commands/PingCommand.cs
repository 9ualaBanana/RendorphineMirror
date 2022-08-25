using System.Text;
using Telegram.Bot.Types;
using Telegram.Models;
using Telegram.Services.Node;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public class PingCommand : AuthenticatedCommand
{
    readonly NodeSupervisor _nodeSupervisor;



    public PingCommand(ILogger<PingCommand> logger, TelegramBot bot, TelegramChatIdAuthenticator authenticator, NodeSupervisor nodeSupervisor)
        : base(logger, bot, authenticator)
    {
        _nodeSupervisor = nodeSupervisor;
    }

    public override string Value => "ping";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken _)
    {
        Logger.LogDebug("Listing online nodes...");

        IEnumerable<KeyValuePair<MachineInfo, TimerPlus>> onlineNodesToList = _nodeSupervisor.NodesOnline;
        var nodeNames = update.Message!.Text!.QuotedArguments().Select(nodeName => nodeName.CaseInsensitive()).ToHashSet();

        if (nodeNames.Any())
            onlineNodesToList = onlineNodesToList.Where(
                nodeInfo => nodeNames.Any(nodeName => nodeInfo.Key.NodeName.CaseInsensitive().Contains(nodeName))
                );
        var message = ListOnlineNodes(onlineNodesToList);

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, message);
    }

    string ListOnlineNodes(IEnumerable<KeyValuePair<MachineInfo, TimerPlus>> onlineNodesToList)
    {
        var messageBuilder = new StringBuilder();

        messageBuilder.AppendHeader("*Node* | *Uptime*");
        foreach (var (nodeInfo, _) in onlineNodesToList.OrderBy(nodeOnline => nodeOnline.Key.NodeName))
        {
            var uptime = _nodeSupervisor.GetUptimeFor(nodeInfo); if (uptime is null) continue;  // Went offline.

            var escapedUptime = $"{uptime:d\\.hh\\:mm}";
            messageBuilder.AppendLine($"{nodeInfo.BriefInfoMDv2} | {escapedUptime}");
        }

        return messageBuilder.ToString();
    }
}
