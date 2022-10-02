using System.Text;
using Telegram.Models;
using Telegram.Services.Node;
using Telegram.Telegram;

namespace Telegram.Services.Telegram.Updates.Commands.Plugins;

internal static class Logic
{
    internal static StringBuilder ListInstalledPluginsFor(IEnumerable<string> nodeNames, NodeSupervisor nodeSupervisor)
    {
        var messageBuilder = new StringBuilder();

        var nodesWhosePluginsToShow = nodeNames.Any() ? nodeSupervisor.AllNodes
            .Where(node => node.NameContainsAny(nodeNames)) : nodeSupervisor.AllNodes;
        foreach (var node in nodesWhosePluginsToShow)
        {
            messageBuilder.AppendLine(ListInstalledPluginsFor(node).ToString());
            messageBuilder.AppendLine();
        }

        return messageBuilder;
    }

    static StringBuilder ListInstalledPluginsFor(MachineInfo nodeInfo)
    {
        var messageBuilder = new StringBuilder();

        messageBuilder.AppendHeader($"Plugins installed on {nodeInfo.BriefInfoMDv2}");
        foreach (var groupedPlugins in nodeInfo.InstalledPlugins.GroupBy(nodePlugin => nodePlugin.Type))
        {
            messageBuilder.AppendLine($"{Enum.GetName(groupedPlugins.Key)}");
            foreach (var plugin in groupedPlugins)
            {
                messageBuilder
                    .AppendLine($"\tVersion: {plugin.Version}")
                    // Directory root is determined based on the OS where the executable is running
                    // so Windows path is broken when running on linux and vice versa.
                    .AppendLine($"\tPath: {plugin.Path[..3].Replace(@"\", @"\\")}");
            }
            messageBuilder.AppendLine();
        }

        return messageBuilder;
    }
}
