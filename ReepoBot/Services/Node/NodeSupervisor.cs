using ReepoBot.Models;
using ReepoBot.Services.Telegram;
using System.Collections.Concurrent;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramHelper;

namespace ReepoBot.Services.Node;

public class NodeSupervisor : IEventHandler<NodeInfo>
{
    internal readonly HashSet<NodeInfo> AllNodes = new();
    internal readonly ConcurrentDictionary<NodeInfo, TimerPlus> NodesOnline = new();
    internal HashSet<NodeInfo> NodesOffline => AllNodes.ToHashSet().Except(NodesOnline.Keys).ToHashSet();

    readonly object _allNodesLock = new();
    readonly ILogger<NodeSupervisor> _logger;
    readonly TelegramBot _bot;
    double _idleTimeBeforeGoingOffline = -1;
    double IdleTimeBeforeNodeGoesOffline
    {
        get
        {
            if (_idleTimeBeforeGoingOffline != -1) return _idleTimeBeforeGoingOffline;

            const string configKey = "TimeBeforeNodeGoesOffline";
            double result = TimeSpan.FromMinutes(10).TotalMilliseconds;
            try
            {
                result = double.Parse(_configuration[configKey]);
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "\"{ConfigKey}\" config key is not defined.", configKey);
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Value of \"{ConfigKey}\" can't be parsed as double.", configKey);
            }
            catch (OverflowException ex)
            {
                _logger.LogWarning(ex, "Value of \"{ConfigKey}\" overflows.", configKey);
            }
            return _idleTimeBeforeGoingOffline = result;
        }
    }
    readonly IConfiguration _configuration;

    public NodeSupervisor(ILogger<NodeSupervisor> logger, IConfiguration configuration, TelegramBot bot)
    {
        _logger = logger;
        _configuration = configuration;
        _bot = bot;
    }

    public async Task HandleAsync(NodeInfo nodeInfo)
    {
        _logger.LogDebug("Updating node status...");
        await UpdateNodeStatus(nodeInfo);
        _logger.LogDebug("Node status is updated.");
    }

    async Task UpdateNodeStatus(NodeInfo nodeInfo)
    {
        if (!NodesOnline.ContainsKey(nodeInfo))
        {
            var previousVersionOfNode = GetPreviousVersionIfOnline(nodeInfo);
            if (previousVersionOfNode is null)
            {
                await AddNewNode(nodeInfo);
            }
            else
            {
                await UpdateNodeVersion(previousVersionOfNode.Value, nodeInfo);
            }
        }

        if (NodesOnline.TryGetValue(nodeInfo, out var nodeUptimeTimer))
        {
            nodeUptimeTimer.Stop();
            nodeUptimeTimer.Start();
        }
    }

    NodeInfo? GetPreviousVersionIfOnline(NodeInfo nodeInfo)
    {
        try
        {
            return NodesOnline.Single(nodeOnline => nodeOnline.Key.PCName == nodeInfo.PCName).Key;
        }
        catch (Exception) { return null; }
    }

    async Task AddNewNode(NodeInfo nodeInfo)
    {
        AllNodes.Add(nodeInfo);
        if (!NodesOnline.TryAdd(nodeInfo, Timer)) return;
        
        foreach (var subscriber in _bot.Subscriptions)
        {
            try
            {
                var message = $"{nodeInfo.BriefInfoMDv2} is online".Sanitize();
                await _bot.SendTextMessageAsync(
                    subscriber,
                    message,
                    parseMode: ParseMode.MarkdownV2
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't add new node.");
            }
        }
        _logger.LogDebug("New node is online: {PCName} {UserName} (v.{Version}) {IP}", nodeInfo.PCName, nodeInfo.UserName, nodeInfo.Version, nodeInfo.IP);
    }

    async Task UpdateNodeVersion(NodeInfo nodeOnline, NodeInfo updatedNode)
    {
        if (!NodesOnline.TryRemove(nodeOnline, out var uptimeTimer)) return;
        NodesOnline.TryAdd(updatedNode, uptimeTimer);

        lock (_allNodesLock)
        {
            if (AllNodes.Contains(nodeOnline)) AllNodes.Remove(nodeOnline);
        }
        AllNodes.Add(updatedNode);

        foreach (var subscriber in _bot.Subscriptions)
        {
            try
            {
                var message = $"{updatedNode.BriefInfoMDv2} was updated: v.*{nodeOnline.Version}* *->* v.*{updatedNode.Version}*.".Sanitize();
                await _bot.SendTextMessageAsync(
                    subscriber,
                    message,
                    parseMode: ParseMode.MarkdownV2
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't send node version update message.");
            }
        }
    }

    TimerPlus Timer
    {
        get
        {
            var timer = new TimerPlus(IdleTimeBeforeNodeGoesOffline) { AutoReset = false };
            timer.Elapsed += OnNodeWentOffline;
            return timer;
        }
    }

    void OnNodeWentOffline(object? sender, ElapsedEventArgs e)
    {
        var offlineNode = NodesOnline.Single(node => node.Value == sender);
        var offlineNodeInfo = offlineNode.Key;
        _logger.LogError("{PCName} {UserName} (v.{Version}) {IP} went offline after {Time} ms since the last ping.",
            offlineNodeInfo.PCName, offlineNodeInfo.UserName, offlineNodeInfo.Version, offlineNodeInfo.IP, IdleTimeBeforeNodeGoesOffline);
        NodesOnline.TryRemove(offlineNodeInfo, out var _);
    }

    /// <returns>
    /// <see cref="TimeSpan"/> representing the last time ping was received from <paramref name="nodeInfo" />;
    /// <c>null</c> if <paramref name="nodeInfo"/> is offline.
    /// </returns>
    internal TimeSpan? GetUptimeFor(NodeInfo nodeInfo)
    {
        NodesOnline.TryGetValue(nodeInfo, out var uptime);
        return uptime?.ElapsedTime;
    }

    internal int TryRemoveNodesWithNames(params string[] nodeNames)
    {
        var names = nodeNames.ToHashSet();
        lock (_allNodesLock)
        {
            return AllNodes.RemoveWhere(nodeInfo => names.Contains(nodeInfo.PCName));
        }
    }
}
