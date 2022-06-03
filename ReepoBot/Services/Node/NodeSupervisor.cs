using ReepoBot.Models;
using ReepoBot.Services.Telegram;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ReepoBot.Services.Node;

public class NodeSupervisor : WebhookEventHandler<NodeInfo>
{
    internal readonly Dictionary<NodeInfo, Timer> NodesOnline = new();
    double _timeBeforeGoingOffline = -1;
    internal double TimeBeforeNodeGoesOffline
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
                Logger.LogError(ex, "\"{configKey}\" config key is not defined.", configKey);
            }
            catch (FormatException ex)
            {
                Logger.LogError(ex, "Value of \"{configKey}\" can't be parsed as double.", configKey);
            }
            catch (OverflowException ex)
            {
                Logger.LogError(ex, "Value of \"{configKey}\" overflows.", configKey);
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
        if (!NodesOnline.ContainsKey(nodeInfo)) NodesOnline.Add(nodeInfo, Timer);

        NodesOnline[nodeInfo].Stop();
        NodesOnline[nodeInfo].Start();
    }

    Timer Timer
    {
        get
        {
            var timer = new Timer(TimeBeforeNodeGoesOffline) { AutoReset = false };
            timer.Elapsed += OnNodeWentOffline;
            return timer;
        }
    }

    void OnNodeWentOffline(object? sender, ElapsedEventArgs e)
    {
        var offlineNode = NodesOnline.Single(node => node.Value == sender);
        var offlineNodeInfo = offlineNode.Key;
        Logger.LogError("{name} (v.{version}) went offline after {time} ms since the last ping.",
            offlineNodeInfo.Name, offlineNodeInfo.Version, TimeBeforeNodeGoesOffline);
        NodesOnline.Remove(offlineNodeInfo);
    }


    public override Task HandleAsync(NodeInfo nodeInfo)
    {
        Logger.LogDebug("Updating node status...");
        UpdateNodeStatus(nodeInfo);
        Logger.LogDebug("Node status is updated.");
        return Task.CompletedTask;
    }
}
