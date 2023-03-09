using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Bot;
using Telegram.Commands.SyntacticAnalysis;
using Telegram.Models;
using Telegram.MPlus;
using Telegram.Security.Authorization;
using Telegram.Services.Node;

namespace Telegram.Commands.Handlers;

public partial class PluginsCommand : CommandHandler, IAuthorizationRequirementsProvider
{
    readonly UserNodes _userNodes;

    public PluginsCommand(
        UserNodes userNodes,
        CommandParser parser,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PluginsCommand> logger)
        : base(parser, bot, httpContextAccessor, logger)
    {
        _userNodes = userNodes;
    }

    public IEnumerable<IAuthorizationRequirement> Requirements { get; }
        = IAuthorizationRequirementsProvider.Provide(MPlusAuthenticationRequirement.Instance);

    internal override Command Target => "plugins";

    protected override async Task HandleAsync(ParsedCommand receivedCommand, HttpContext context)
    {
        if (!_userNodes.TryGetUserNodeSupervisor(MPlusIdentity.UserIdOf(context.User), out var userNodesSupervisor, Bot, Update.Message!.Chat.Id))
            return;

        var nodeNamesWhosePluginsToShow = receivedCommand.QuotedArguments;

        var message = ListInstalledPluginsFor(nodeNamesWhosePluginsToShow, userNodesSupervisor).ToString();

        await Bot.SendMessageAsync_(Update.Message.Chat.Id, message);
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
