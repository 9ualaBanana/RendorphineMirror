using ReepoBot.Models;
using ReepoBot.Services.Telegram;
using System.Timers;

namespace ReepoBot.Services.Node;

public class NodeSupervisor : WebhookEventHandler<NodeInfo>
{
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
                Logger.LogWarning(ex, "\"{ConfigKey}\" config key is not defined.", configKey);
            }
            catch (FormatException ex)
            {
                Logger.LogWarning(ex, "Value of \"{ConfigKey}\" can't be parsed as double.", configKey);
            }
            catch (OverflowException ex)
            {
                Logger.LogWarning(ex, "Value of \"{ConfigKey}\" overflows.", configKey);
            }
            return _timeBeforeGoingOffline = result;
        }
    }
    readonly IConfiguration _configuration;

    public NodeSupervisor(ILogger<NodeSupervisor> logger, TelegramBot _, IConfiguration configuration)
        : base(logger, _)
    {
        _configuration = configuration;
    }

    void UpdateNodeStatus(NodeInfo nodeInfo)
    {
        if (!NodesOnline.ContainsKey(nodeInfo))
        {
            Logger.LogDebug("New node is online: {Node} (v.{Version}).", nodeInfo.Name, nodeInfo.Version);
            NodesOnline.Add(nodeInfo, Timer);
        }

        NodesOnline[nodeInfo].Stop();
        NodesOnline[nodeInfo].Start();
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
        Logger.LogError("{Name} (v.{Version}) went offline after {Time} ms since the last ping.",
            offlineNodeInfo.Name, offlineNodeInfo.Version, TimeBeforeNodeGoesOffline);
        NodesOnline.Remove(offlineNodeInfo);
    }

    /// <returns>
    /// <see cref="TimeSpan"/> representing the last time ping was received from <paramref name="node"/>;
    /// <c>null</c> if <paramref name="node"/> is offline.
    /// </returns>
    internal TimeSpan? ElapsedSinceLastPingFrom(NodeInfo node)
    {
        if (!NodesOnline.ContainsKey(node)) return null;

        return NodesOnline[node].ElapsedTime;
    }

    public override Task HandleAsync(NodeInfo nodeInfo)
    {
        Logger.LogDebug("Updating node status...");
        UpdateNodeStatus(nodeInfo);
        Logger.LogDebug("Node status is updated.");
        return Task.CompletedTask;
    }
}
