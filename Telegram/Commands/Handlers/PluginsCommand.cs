using GIBS.Authorization;
using GIBS.Commands;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Models;
using Telegram.MPlus.Security;
using Telegram.Security.Authorization;
using Telegram.Services.Node;

namespace Telegram.Commands.Handlers;

public partial class PluginsCommand : CommandHandler, IAuthorizationPolicyProtected
{
    readonly UserNodes _userNodes;

    public PluginsCommand(
        UserNodes userNodes,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PluginsCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _userNodes = userNodes;
    }

    public override Command Target => CommandFactory.Create("plugins");

    public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder().Build();

    protected override async Task HandleAsync(Command receivedCommand)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(MPlusIdentity.UserIdOf(User), out var userNodesSupervisor, Bot, ChatId))
            return;

        var nodeNamesWhosePluginsToShow = receivedCommand.QuotedArguments;

        var message = ListInstalledPluginsFor(nodeNamesWhosePluginsToShow, userNodesSupervisor).ToString();

        await Bot.SendMessageAsync_(ChatId, message);
    }

    static StringBuilder ListInstalledPluginsFor(IEnumerable<string> nodeNames, NodeSupervisor nodeSupervisor)
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
