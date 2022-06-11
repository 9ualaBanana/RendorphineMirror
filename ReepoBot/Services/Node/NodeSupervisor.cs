using ReepoBot.Models;
using ReepoBot.Services.Telegram;
using System.Collections.Concurrent;
using System.Timers;

namespace ReepoBot.Services.Node;

public class NodeSupervisor
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

    internal void UpdateNodeStatus(NodeInfo nodeInfo)
    {
        _logger.LogDebug("Updating node status...");

        if (!NodesOnline.ContainsKey(nodeInfo))
        {
            var previousVersionOfNode = GetPreviousVersionIfOnline(nodeInfo);

            if (previousVersionOfNode is null)
                AddNewNode(nodeInfo);
            else
                UpdateNodeVersion(previousVersionOfNode.Value, nodeInfo);
        }

        if (NodesOnline.TryGetValue(nodeInfo, out var nodeUptimeTimer))
        {
            nodeUptimeTimer.Stop();
            nodeUptimeTimer.Start();
        }

        _logger.LogDebug("Node status is updated");
    }

    NodeInfo? GetPreviousVersionIfOnline(NodeInfo nodeInfo)
    {
        try
        {
            return NodesOnline.Single(nodeOnline => nodeOnline.Key.PCName == nodeInfo.PCName).Key;
        }
        catch (Exception) { return null; }
    }

    void AddNewNode(NodeInfo nodeInfo)
    {
        AllNodes.Add(nodeInfo);
        if (!NodesOnline.TryAdd(nodeInfo, Timer)) return;

        _bot.TryNotifySubscribers($"{nodeInfo.BriefInfoMDv2} is online", _logger);
        _logger.LogDebug("New node is online: {Node}", nodeInfo.BriefInfoMDv2);
    }

    void UpdateNodeVersion(NodeInfo nodeOnline, NodeInfo updatedNode)
    {
        if (!NodesOnline.TryRemove(nodeOnline, out var uptimeTimer)) return;
        NodesOnline.TryAdd(updatedNode, uptimeTimer);

        lock (_allNodesLock)
        {
            if (AllNodes.Contains(nodeOnline)) AllNodes.Remove(nodeOnline);
        }
        AllNodes.Add(updatedNode);

        _bot.TryNotifySubscribers(
            $"{updatedNode.BriefInfoMDv2} was updated: v.*{nodeOnline.Version}* *=>* v.*{updatedNode.Version}*.",
            _logger);
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
        var offlineNodeInfo = NodesOnline.Single(node => node.Value == sender).Key;
        NodesOnline.TryRemove(offlineNodeInfo, out var _);

        _logger.LogError("{Node} went offline after {Time} ms since the last ping.",
            offlineNodeInfo.BriefInfoMDv2, IdleTimeBeforeNodeGoesOffline);
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
