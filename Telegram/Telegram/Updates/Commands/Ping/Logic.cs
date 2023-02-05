using System.Text;
using Telegram.Commands;
using Telegram.Models;
using Telegram.Services.Node;

namespace Telegram.Telegram.Updates.Commands.Ping;

internal static class Logic
{
    internal const string Header = "*Node* | *Uptime*";

    internal static StringBuilder ListOnlineNodes(NodeSupervisor nodeSupervisor, HashSet<string> nodeNames)
    {
        IEnumerable<MachineInfo> onlineNodesToList = nodeSupervisor.NodesOnline;

        if (nodeNames.Any())
            onlineNodesToList = onlineNodesToList.Where(
                nodeInfo => nodeNames.Any(nodeName => nodeInfo.NodeName.CaseInsensitive().Contains(nodeName))
                );

        return ListSpecifiedOnlineNodes(nodeSupervisor, onlineNodesToList);
    }

    static StringBuilder ListSpecifiedOnlineNodes(NodeSupervisor userNodesSupervisor, IEnumerable<MachineInfo> onlineNodesToList)
    {
        var messageBuilder = new StringBuilder();

        foreach (var nodeInfo in onlineNodesToList.OrderBy(nodeOnline => nodeOnline.NodeName))
        {
            if (userNodesSupervisor.UptimeOf(nodeInfo, out var uptime))
                messageBuilder.AppendLine($"{nodeInfo.BriefInfoMDv2} | {uptime:d\\.hh\\:mm}");
        }

        return messageBuilder;
    }
}
