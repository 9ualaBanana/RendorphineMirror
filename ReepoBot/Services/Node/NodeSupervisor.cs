using ReepoBot.Models;
using System.Timers;

namespace ReepoBot.Services.Node;

public class NodeSupervisor : IEventHandler<NodeInfo>
{
    readonly ILogger<NodeSupervisor> _logger;
    internal readonly Dictionary<NodeInfo, TimerPlus> NodesOnline = new();
    double _timeBeforeGoingOffline = -1;
    double TimeBeforeNodeGoesOffline
    {
        get
        {
            if (_timeBeforeGoingOffline != -1) return _timeBeforeGoingOffline;

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
            return _timeBeforeGoingOffline = result;
        }
    }
    readonly IConfiguration _configuration;

    public NodeSupervisor(ILogger<NodeSupervisor> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task HandleAsync(NodeInfo nodeInfo)
    {
        _logger.LogDebug("Updating node status...");
        UpdateNodeStatus(nodeInfo);
        _logger.LogDebug("Node status is updated.");
        return Task.CompletedTask;
    }

    void UpdateNodeStatus(NodeInfo nodeInfo)
    {
        if (!NodesOnline.ContainsKey(nodeInfo))
        {
            var previousVersionOfNode = GetPreviousVersionIfOnline(nodeInfo);
            if (previousVersionOfNode is null)
            {
                NodesOnline.Add(nodeInfo, Timer);
                _logger.LogDebug("New node is online: {Name} {PCName} | v.{Version} | {IP}", nodeInfo.UserName, nodeInfo.PCName, nodeInfo.Version, nodeInfo.IP);
            }
            else
            {
                NodesOnline.Remove((NodeInfo)previousVersionOfNode);
                NodesOnline.Add(nodeInfo, Timer);
            }
        }

        NodesOnline[nodeInfo].Stop();
        NodesOnline[nodeInfo].Start();
    }

    NodeInfo? GetPreviousVersionIfOnline(NodeInfo nodeInfo)
    {
        try
        {
            return NodesOnline.Single(nodeOnline => nodeOnline.Key.IP == nodeInfo.IP).Key;
        }
        catch (Exception) { return null; }
    }

    TimerPlus Timer
    {
        get
        {
            var timer = new TimerPlus(TimeBeforeNodeGoesOffline) { AutoReset = false };
            timer.Elapsed += OnNodeWentOffline;
            return timer;
        }
    }

    void OnNodeWentOffline(object? sender, ElapsedEventArgs e)
    {
        var offlineNode = NodesOnline.Single(node => node.Value == sender);
        var offlineNodeInfo = offlineNode.Key;
        _logger.LogError("{Name} {PCName} | v.{Version} | {IP} went offline after {Time} ms since the last ping.",
            offlineNodeInfo.UserName, offlineNodeInfo.PCName, offlineNodeInfo.Version, offlineNodeInfo.IP, TimeBeforeNodeGoesOffline);
        NodesOnline.Remove(offlineNodeInfo);
    }

    /// <returns>
    /// <see cref="TimeSpan"/> representing the last time ping was received from <paramref name="node"/>;
    /// <c>null</c> if <paramref name="node"/> is offline.
    /// </returns>
    internal TimeSpan? GetUptimeFor(NodeInfo node)
    {
        if (!NodesOnline.ContainsKey(node)) return null;

        return NodesOnline[node].ElapsedTime;
    }
}
