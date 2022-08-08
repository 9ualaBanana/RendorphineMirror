using Common;
using Machine.Plugins.Deployment;
using Node.UserSettings;
using ReepoBot.Models;
using ReepoBot.Services.Node;
using ReepoBot.Services.Telegram.Authentication;
using ReepoBot.Services.Telegram.Helpers;
using System.Text;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Updates;

public class TelegramCommandHandler
{
    readonly ILogger _logger;

    readonly TelegramBot _bot;
    readonly HttpClient _httpClient;
    readonly TelegramChatIdAuthentication _authentication;
    readonly NodeSupervisor _nodeSupervisor;

    public TelegramCommandHandler(
        ILogger<TelegramMessageHandler> logger,
        TelegramBot bot,
        IHttpClientFactory httpClientFactory,
        TelegramChatIdAuthentication authentication,
        NodeSupervisor nodeSupervisor)
    {
        _logger = logger;
        _bot = bot;
        _httpClient = httpClientFactory.CreateClient();
        _authentication = authentication;
        _nodeSupervisor = nodeSupervisor;
    }

    public async Task HandleAsync(Update update)
    {
        var command = update.Message!.Text!;
        _logger.LogDebug("Dispatching {Command} command...", command);

        ChatId id = update.Message.Chat.Id;
        var unprefixedCommand = command[1..];

        if (unprefixedCommand.StartsWith("pinglist"))
        { _authentication.Required(_ => HandlePingList(update), id); return; }
        else if (unprefixedCommand.StartsWith("ping"))
        { _authentication.Required(_ => HandlePing(update), id); return; }
        else if (unprefixedCommand.StartsWith("plugins"))
        { _authentication.Required(_ => HandlePlugins(update), id); return; }
        else if (unprefixedCommand.StartsWith("online"))
        { _authentication.Required(_ => HandleOnline(update), id); return; }
        else if (unprefixedCommand.StartsWith("offline"))
        { _authentication.Required(_ => HandleOffline(update), id); return; }
        else if (unprefixedCommand.StartsWith("remove"))
        { _authentication.Required(_ => HandleRemove(update), id); return; }
        else if (unprefixedCommand.StartsWith("deploy"))
        { _authentication.Required(sessionId => HandleDeploy(update, sessionId), id); return; }
        else if (unprefixedCommand.StartsWith("login"))
        { await HandleLoginAsync(update); return; }
        else if (unprefixedCommand.StartsWith("logout"))
        { _authentication.Required(_ => HandleLogout(update), id); return; }

        _logger.LogWarning("No handler for {Command} command is found", command);
    }

    void HandlePingList(Update update)
    {
        _logger.LogDebug("Listing all nodes...");

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("*All Nodes*");
        messageBuilder.AppendLine(TelegramHelperExtensions.HorizontalDelimeter);
        foreach (var nodeInfo in _nodeSupervisor.AllNodes.OrderBy(node => node.NodeName))
        {
            messageBuilder.Append(nodeInfo.BriefInfoMDv2);
            if (_nodeSupervisor.NodesOffline.Contains(nodeInfo)) messageBuilder.AppendLine(" *--OFFLINE--*");
            else messageBuilder.AppendLine();
        }
        var message = messageBuilder.ToString();

        _ = _bot.TrySendMessageAsync(update.Message!.Chat.Id, message, _logger);
    }

    void HandlePing(Update update)
    {
        _logger.LogDebug("Listing online nodes...");

        IEnumerable<KeyValuePair<MachineInfo, TimerPlus>> nodesOnlineToList;
        var splitCommand = update.Message!.Text!.Split();

        if (splitCommand.Length > 1)
        {
            var lcNodeNamesToList = splitCommand[1..]
                .Select(nodeName => nodeName.ToLower())
                .ToHashSet();

            nodesOnlineToList = _nodeSupervisor.NodesOnline.Where(
                nodeInfo => lcNodeNamesToList.Any(
                    lcNodeNameToList => nodeInfo.Key.NodeName.ToLower().Contains(lcNodeNameToList)
                    )
                );
        }
        else
        {
            nodesOnlineToList = _nodeSupervisor.NodesOnline;
        }

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("*Node* | *Uptime*");
        messageBuilder.AppendLine(TelegramHelperExtensions.HorizontalDelimeter);
        foreach (var (nodeInfo, _) in nodesOnlineToList.OrderBy(nodeOnline => nodeOnline.Key.NodeName))
        {
            var uptime = _nodeSupervisor.GetUptimeFor(nodeInfo);
            // It could already go offline.
            if (uptime is null) continue;

            var escapedUptime = $"{uptime:d\\.hh\\:mm}";
            messageBuilder.AppendLine($"{nodeInfo.BriefInfoMDv2} | {escapedUptime}");
        }
        var message = messageBuilder.ToString();

        _ = _bot.TrySendMessageAsync(update.Message!.Chat.Id, message, _logger);
    }

    void HandlePlugins(Update update)
    {
        var nodesNamesWhosePluginsToShow = update.Message!.Text!.QuotedArguments();
        var nodesWhosePluginsToShow = _nodeSupervisor.AllNodes.Where(node => node.NameContainsAny(nodesNamesWhosePluginsToShow));

        var messageBuilder = new StringBuilder();
        foreach (var node in nodesWhosePluginsToShow)
        {
            messageBuilder.AppendLine(node.InstalledPluginsAsText);
            messageBuilder.AppendLine();
        }

        _ = _bot.TrySendMessageAsync(update.Message!.Chat.Id, messageBuilder.ToString(), _logger);
    }

    void HandleOnline(Update update)
    {
        var message = $"Online: *{_nodeSupervisor.NodesOnline.Count}*\nOffline: {_nodeSupervisor.NodesOffline.Count}";
        _ = _bot.TrySendMessageAsync(update.Message!.Chat.Id, message, _logger);
    }

    void HandleOffline(Update update)
    {
        _logger.LogDebug("Listing offline nodes...");

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("*Offline Nodes*");
        messageBuilder.AppendLine(TelegramHelperExtensions.HorizontalDelimeter);
        foreach (var nodeInfo in _nodeSupervisor.NodesOffline.OrderBy(node => node.NodeName))
        {
            messageBuilder.AppendLine(nodeInfo.BriefInfoMDv2);
        }
        var message = messageBuilder.ToString();

        _ = _bot.TrySendMessageAsync(update.Message!.Chat.Id, message, _logger);
    }

    void HandleRemove(Update update)
    {
        var nodeNames = update.Message!.Text!.QuotedArguments().ToArray();

        int nodesRemoved = _nodeSupervisor.TryRemoveNodesWithNames(nodeNames);

        var message = nodesRemoved == 0 ?
            $"Nodes with specified names are either online or not found." :
            $"{nodesRemoved} were removed.";

        _ = _bot.TrySendMessageAsync(update.Message!.Chat.Id, message, _logger);
    }

    void HandleDeploy(Update update, string sessionId)
    {
        var stringPluginTypes = update.Message!.Text!.UnquotedArguments().OrderBy(type => type);
        var nodeNames = update.Message.Text!.QuotedArguments();

        var pluginTypes = stringPluginTypes
            .Select(type => Enum.Parse<PluginType>(type, true));

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
                .ForEach(node => _ = userSettingsManager.TrySetAsync(new(node.Guid) { NodeInstallSoftware = plugins }, sessionId));
        }
        else
            _ = userSettingsManager.TrySetAsync(new() { InstallSoftware = plugins }, sessionId);
    }

    async Task HandleLoginAsync(Update update)
    {
        await _authentication.AuthenticateAsync(update.Message!);
    }

    void HandleLogout(Update update)
    {
        _authentication.LogOut(update.Message!.Chat.Id);
    }
}