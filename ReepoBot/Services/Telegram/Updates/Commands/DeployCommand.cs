using Common;
using Machine.Plugins.Deployment;
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

        var plugins = pluginTypes
            .Where(type => type.IsPlugin())
            .Select(type => new PluginToDeploy() { Type = type, Version = string.Empty })
            .ToList();

        foreach (var plugin in plugins)
        {
            plugin.SubPlugins = pluginTypes
                .Where(type => type.IsChildOf(plugin.Type))
                .Select(type => new PluginToDeploy() { Type = type, Version = string.Empty });
            foreach (var subPlugin in plugin.SubPlugins)
            {
                subPlugin.SubPlugins = pluginTypes
                    .Where(type => type.IsChildOf(plugin.Type))
                    .Select(type => new PluginToDeploy() { Type = type, Version = string.Empty });
            }
        }

        var userSettingsManager = new UserSettingsManager(_httpClient);
        if (nodeNames.Any())
        {
            nodeNames
                .SelectMany(_nodeSupervisor.GetNodesByName).ToList()
                .ForEach(node => _ = userSettingsManager.TrySetAsync(new UserSettings(node.Guid) { NodeInstallSoftware = plugins }, authenticationToken.SessionId));
        }
        else
            _ = userSettingsManager.TrySetAsync(new UserSettings() { InstallSoftware = plugins }, authenticationToken.SessionId);
    }

    static IEnumerable<PluginType> ParsePluginTypesFrom(string receivedCommand) =>
        receivedCommand.UnquotedArguments().Select(type => Enum.Parse<PluginType>(type, true));
}
