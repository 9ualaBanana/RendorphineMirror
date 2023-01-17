using System.Text;
using Telegram.Models;
using Telegram.Services.Node;

namespace Telegram.Telegram.Updates.Commands.Pinglist;

internal static class Logic
{
    internal const string Header = "*All Nodes*";

    internal static StringBuilder ListNodesOrderedByName(NodeSupervisor userNodesSupervisor)
    {
        var messageBuilder = new StringBuilder();

        foreach (var nodeInfo in userNodesSupervisor.AllNodes.OrderBy(node => node.NodeName))
            messageBuilder.Append(nodeInfo.BriefInfoMDv2).AppendLine(GetStatusFor(nodeInfo, userNodesSupervisor));

        return messageBuilder;
    }

    static string? GetStatusFor(MachineInfo nodeInfo, NodeSupervisor userNodesSupervisor) =>
        userNodesSupervisor.NodesOffline.Contains(nodeInfo) ? " *--OFFLINE--*" : null;
}
