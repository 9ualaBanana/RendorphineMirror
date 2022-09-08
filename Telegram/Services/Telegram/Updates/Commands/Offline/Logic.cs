using System.Text;
using Telegram.Services.Node;

namespace Telegram.Services.Telegram.Updates.Commands.Offline;

internal static class Logic
{
    internal const string Header = "*Offline Nodes*";
    internal static StringBuilder ListOfflineNodes(NodeSupervisor userNodesSupervisor)
    {
        var messageBuilder = new StringBuilder();

        foreach (var nodeInfo in userNodesSupervisor.NodesOffline.OrderBy(node => node.NodeName))
            messageBuilder.AppendLine(nodeInfo.BriefInfoMDv2);

        return messageBuilder;
    }
}
