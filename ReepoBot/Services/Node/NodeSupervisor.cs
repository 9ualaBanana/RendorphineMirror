﻿using ReepoBot.Models;
using ReepoBot.Services.Telegram;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ReepoBot.Services.Node;

public class NodeSupervisor : WebhookEventHandler<NodeInfo>
{
    readonly Dictionary<NodeInfo, Timer> _nodesOnline = new();
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
        if (!_nodesOnline.ContainsKey(nodeInfo)) _nodesOnline.Add(nodeInfo, Timer);

        _nodesOnline[nodeInfo].Stop();
        _nodesOnline[nodeInfo].Start();
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
        var offlineNode = _nodesOnline.Single(node => node.Value == sender);
        var offlineNodeInfo = offlineNode.Key;
        Logger.LogWarning("{name} ({version}) went offline after {time} since the last ping.",
            offlineNodeInfo.Name, offlineNodeInfo.Version, TimeBeforeNodeGoesOffline);
        _nodesOnline.Remove(offlineNodeInfo);
    }


    public override Task HandleAsync(NodeInfo nodeInfo)
    {
        Logger.LogDebug("Updating node status...");
        UpdateNodeStatus(nodeInfo);
        Logger.LogDebug("Node status is updated.");
        return Task.CompletedTask;
    }
}
