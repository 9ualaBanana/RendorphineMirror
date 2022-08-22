using Common;
using Node.Plugins.Deployment;
using Node.UserSettings;
using ReepoBot.Services.Node;
using ReepoBot.Services.Telegram.Authentication;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Updates.Commands;

public class DeployCommand : AuthenticatedCommand
{
    readonly NodeSupervisor _nodeSupervisor;
    readonly HttpClient _httpClient;

    public DeployCommand(ILogger<DeployCommand> logger, TelegramBot bot, TelegramChatIdAuthentication authentication, NodeSupervisor nodeSupervisor, IHttpClientFactory httpClientFactory)
        : base(logger, bot, authentication)
    {
        _nodeSupervisor = nodeSupervisor;
        _httpClient = httpClientFactory.CreateClient();
    }

    public override string Value => "deploy";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken authenticationToken)
    {
        var pluginTypes = ParsePluginTypesFrom(update.Message!.Text!);
        var nodeNames = update.Message.Text!.QuotedArguments();

        var plugins = pluginTypes.Where(type => type.IsPlugin())
            .Select(type => new PluginToDeploy() { Type = type, Version = string.Empty }).ToHashSet();

        var userSettingsManager = new UserSettingsManager(_httpClient);
        var userSettings = await userSettingsManager.TryFetchAsync(authenticationToken.SessionId);
        if (userSettings is null)
        { await Bot.TrySendMessageAsync(update.Message.Chat.Id, "Plugins couldn't be deployed."); return; }

        PopulateWithChildPlugins(plugins, pluginTypes);

        if (nodeNames.Any())
        {
            foreach (var node in nodeNames.SelectMany(_nodeSupervisor.GetNodesByName))
            {
                var nodeSettings = new UserSettings(node.Guid) { InstallSoftware = userSettings.InstallSoftware, NodeInstallSoftware = userSettings.NodeInstallSoftware };
                nodeSettings.ThisNodeInstallSoftware.UnionEachWith(plugins);
                if (!await userSettingsManager.TrySetAsync(nodeSettings, authenticationToken.SessionId))
                { await Bot.TrySendMessageAsync(update.Message.Chat.Id, "Plugins couldn't be deployed."); return; }
            }
        }
        else
        {
            userSettings.InstallSoftware.UnionEachWith(plugins);
            if (!await userSettingsManager.TrySetAsync(new UserSettings() { InstallSoftware = userSettings.InstallSoftware, NodeInstallSoftware = userSettings.NodeInstallSoftware }, authenticationToken.SessionId))
            { await Bot.TrySendMessageAsync(update.Message.Chat.Id, "Plugins couldn't be deployed."); return; }
        }
        await Bot.TrySendMessageAsync(update.Message.Chat.Id, "Plugins successfully added to the deploy queue.");
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

    static IEnumerable<PluginType> ParsePluginTypesFrom(string receivedCommand) =>
        receivedCommand.UnquotedArguments().Select(type => Enum.Parse<PluginType>(type, true));
}
