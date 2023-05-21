using Microsoft.AspNetCore.Authorization;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.MPlus.Security;
using Telegram.Security.Authorization;
using Telegram.Services.Node;

namespace Telegram.Commands.Handlers;

public class DeployCommand : CommandHandler, IAuthorizationPolicyProtected
{
    readonly UserNodes _userNodes;
    readonly HttpClient _httpClient;

    public DeployCommand(
        UserNodes userNodes,
        IHttpClientFactory httpClientFactory,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DeployCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _userNodes = userNodes;
        _httpClient = httpClientFactory.CreateClient();
    }

    internal override Command Target => CommandFactory.Create("deploy");

    public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder().Build();

    protected override async Task HandleAsync(Command receivedCommand)
    {
        var pluginTypes = receivedCommand.UnquotedArguments.Select(type => Enum.Parse<PluginType>(type, true));
        var nodeNames = receivedCommand.QuotedArguments;

        var plugins = pluginTypes.Where(type => type.IsPlugin())
            .Select(type => new PluginToDeploy() { Type = type, Version = string.Empty }).ToHashSet();
        
        var api = Apis.DefaultWithSessionId(MPlusIdentity.SessionIdOf(User));

        var userSettingsManager = new UserSettingsManager(_httpClient);
        var userSettings = await userSettingsManager.TryFetchAsync(MPlusIdentity.SessionIdOf(User));
        if (userSettings is null)
        { await Bot.SendMessageAsync_(ChatId, "Plugins couldn't be deployed."); return; }
        PopulateWithChildPlugins(plugins, pluginTypes);
        if (nodeNames.Any())
        {
            if (!_userNodes.TryGetUserNodeSupervisor(MPlusIdentity.UserIdOf(User), out var userNodesSupervisor, Bot, ChatId))
                return;

            foreach (var node in nodeNames.SelectMany(userNodesSupervisor.GetNodesByName))
            {
                var nodeSettings = new UserSettings(node.Guid) { InstallSoftware = userSettings.InstallSoftware, NodeInstallSoftware = userSettings.NodeInstallSoftware };
                nodeSettings.ThisNodeInstallSoftware.UnionEachWith(plugins);
                if (!await userSettingsManager.TrySetAsync(nodeSettings, MPlusIdentity.SessionIdOf(User)))
                { await Bot.SendMessageAsync_(ChatId, "Plugins couldn't be deployed."); return; }
            }
        }
        else
        {
            userSettings.InstallSoftware.UnionEachWith(plugins);
            if (!await userSettingsManager.TrySetAsync(new UserSettings() { InstallSoftware = userSettings.InstallSoftware, NodeInstallSoftware = userSettings.NodeInstallSoftware }, MPlusIdentity.SessionIdOf(User)))
            { await Bot.SendMessageAsync_(ChatId, "Plugins couldn't be deployed."); return; }
        }
        await Bot.SendMessageAsync_(ChatId, "Plugins successfully added to the deploy queue.");
    }

    static void PopulateWithChildPlugins(IEnumerable<PluginToDeploy> plugins, IEnumerable<PluginType> pluginTypes)
    {
        foreach (var plugin in plugins)
        {
            plugin.SubPlugins = pluginTypes
                .Where(type => type.IsChildOf(plugin.Type))
                .Select(type => new PluginToDeploy() { Type = type, Version = string.Empty })
                .ToHashSet();
            foreach (var subPlugin in plugin.SubPlugins)
            {
                subPlugin.SubPlugins = pluginTypes
                    .Where(type => type.IsChildOf(subPlugin.Type))
                    .Select(type => new PluginToDeploy() { Type = type, Version = string.Empty })
                    .ToHashSet();
            }
        }
    }
}
