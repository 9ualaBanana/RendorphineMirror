using ReepoBot.Models;
using ReepoBot.Services.Telegram;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramHelper;

namespace ReepoBot.Services.Node;

public class NodeSupervisor : IEventHandler<NodeInfo>
{
    readonly ILogger<NodeSupervisor> _logger;
    readonly TelegramBot _bot;
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

    public NodeSupervisor(ILogger<NodeSupervisor> logger, IConfiguration configuration, TelegramBot bot)
    {
        _logger = logger;
        _configuration = configuration;
        _bot = bot;
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
                AddNewNode(nodeInfo);
            }
            else
            {
                UpdateNodeVersion(previousVersionOfNode.Value, nodeInfo.Version);
            }
        }

        NodesOnline[nodeInfo].Stop();
        NodesOnline[nodeInfo].Start();
    }

    NodeInfo? GetPreviousVersionIfOnline(NodeInfo nodeInfo)
    {
        try
        {
            return NodesOnline.Single(nodeOnline => nodeOnline.Key.UserName == nodeInfo.UserName).Key;
        }
        catch (Exception) { return null; }
    }

    void AddNewNode(NodeInfo nodeInfo)
    {
        NodesOnline.Add(nodeInfo, Timer);
        foreach (var subscriber in _bot.Subscriptions)
        {
            try
            {
                var message = $"{nodeInfo.BriefInfoMDv2} is online".Sanitize();
                _bot.SendTextMessageAsync(
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

    void UpdateNodeVersion(NodeInfo nodeOnline, string newVersion)
    {
        var uptimeTimer = NodesOnline[nodeOnline];
        NodesOnline.Remove(nodeOnline);
        var updatedNode = nodeOnline with { Version = newVersion };
        NodesOnline.Add(updatedNode, uptimeTimer);

        foreach (var subscriber in _bot.Subscriptions)
        {
            try
            {
                var message = $"{updatedNode.BriefInfoMDv2} was updated: v.*{nodeOnline.Version}* *->* v.*{updatedNode.Version}*.".Sanitize();
                _bot.SendTextMessageAsync(
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
            var timer = new TimerPlus(TimeBeforeNodeGoesOffline) { AutoReset = false };
            timer.Elapsed += OnNodeWentOffline;
            return timer;
        }
    }

    void OnNodeWentOffline(object? sender, ElapsedEventArgs e)
    {
        var offlineNode = NodesOnline.Single(node => node.Value == sender);
        var offlineNodeInfo = offlineNode.Key;
        _logger.LogError("{PCName} {UserName} (v.{Version}) {IP} went offline after {Time} ms since the last ping.",
            offlineNodeInfo.PCName, offlineNodeInfo.UserName, offlineNodeInfo.Version, offlineNodeInfo.IP, TimeBeforeNodeGoesOffline);
        NodesOnline.Remove(offlineNodeInfo);
    }

    /// <returns>
    /// <see cref="TimeSpan"/> representing the last time ping was received from <paramref name="node" />;
    /// <c>null</c> if <paramref name="node"/> is offline.
    /// </returns>
    internal TimeSpan? GetUptimeFor(NodeInfo node)
    {
        if (!NodesOnline.ContainsKey(node)) return null;

        return NodesOnline[node].ElapsedTime;
    }
}
