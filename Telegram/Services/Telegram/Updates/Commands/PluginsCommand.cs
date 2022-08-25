using System.Text;
using Telegram.Bot.Types;
using Telegram.Models;
using Telegram.Services.Node;
using Telegram.Services.Telegram;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public class PluginsCommand : AuthenticatedCommand
{
    readonly NodeSupervisor _nodeSupervisor;

    public PluginsCommand(ILogger<PluginsCommand> logger, TelegramBot bot, TelegramChatIdAuthentication authentication, NodeSupervisor nodeSupervisor)
        : base(logger, bot, authentication)
    {
        _nodeSupervisor = nodeSupervisor;
    }

    public override string Value => "plugins";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken _)
    {
        var nodeNamesWhosePluginsToShow = update.Message!.Text!.QuotedArguments();
        var nodesWhosePluginsToShow = _nodeSupervisor.AllNodes.Where(node => node.NameContainsAny(nodeNamesWhosePluginsToShow));

        var message = ListInstalledPluginsFor(nodesWhosePluginsToShow);

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, message);
    }

    string ListInstalledPluginsFor(IEnumerable<MachineInfo> nodesInfo)
    {
        var messageBuilder = new StringBuilder();

        foreach (var node in nodesInfo)
        {
            messageBuilder.AppendLine(ListInstalledPluginsFor(node));
            messageBuilder.AppendLine();
        }

        return messageBuilder.ToString();
    }

    static string ListInstalledPluginsFor(MachineInfo nodeInfo)
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

        return messageBuilder.ToString();
    }
}
