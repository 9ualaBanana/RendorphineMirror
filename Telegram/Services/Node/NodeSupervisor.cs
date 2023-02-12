using Telegram.Models;
using System.Collections.Specialized;
using Telegram.Telegram.Authentication.Services;
using Telegram.Bot;
using Telegram.Commands;

namespace Telegram.Services.Node;

public class NodeSupervisor
{
    internal readonly AutoStorage<MachineInfo> NodesOnline;
    internal readonly AutoStorage<MachineInfo> AllNodes;
    internal HashSet<MachineInfo> NodesOffline => AllNodes.Except(NodesOnline).ToHashSet();

    readonly object _lock = new();
    readonly ILogger<NodeSupervisor> _logger;
    readonly TelegramBot _bot;
    readonly AuthenticatedUsersDbContext _authenticatedUsers;


    public NodeSupervisor(ILogger<NodeSupervisor> logger, IConfiguration configuration, TelegramBot bot, AuthenticatedUsersDbContext users)
    {
        AllNodes = new();
        NodesOnline = new(configuration.ReadIdleTimeBeforeGoingOfflineFrom(logger));
        NodesOnline.ItemStorageTimeElapsed += OnNodeWentOffline;
        _logger = logger;
        _bot = bot;
        _authenticatedUsers = users;
    }


    internal async Task UpdateNodeStatusAsync(MachineInfo nodeInfo)
    {
        _logger.LogTrace("Updating node status...");

        await AddOrUpdateNodeAsync(nodeInfo);

        _logger.LogTrace("Node status is updated");
    }

    async Task AddOrUpdateNodeAsync(MachineInfo nodeInfo)
    {
        // Using `nodeInfo` to look for `nodeOnline` is alright because MachineInfo's equality is based on its NodeName.
        // Also using `nodeInfo` allows adding `nodeInfo` if it's not online or update `nodeOnline's` version to the one of `nodeInfo` if it is.
        bool wasOnline = NodesOnline.TryGetValue(nodeInfo, out var nodeOnline);
        bool versionIsUpdated = nodeOnline?.Version != nodeInfo.Version;

        lock (_lock)
        {
            AllNodes.AddOrUpdateValue(nodeInfo, nodeInfo);
            NodesOnline.AddOrUpdateValue(nodeInfo, nodeInfo);
        }

        if (wasOnline)
        {
            if (versionIsUpdated)
            {
                var userChatAuthenticationTokens = _authenticatedUsers.Users.Where(user => user.MPlus.UserId == nodeInfo.UserId);
                if (userChatAuthenticationTokens.Any())
                    foreach (var chatAuthenticationToken in userChatAuthenticationTokens)
                        await _bot.SendMessageAsync_(chatAuthenticationToken.ChatId, $"{nodeInfo.BriefInfoMDv2} was updated: v.*{nodeOnline!.Version}* *=>* v.*{nodeInfo.Version}*.");
            }
        }
        else _logger.LogDebug("New node is online: {Node}", nodeInfo.BriefInfoMDv2);
    }

    void OnNodeWentOffline(object? sender, AutoStorageItem<MachineInfo> e)
    {
        _logger.LogWarning("{Node} went offline after {Time} ms since the last ping.",
            e.Value.BriefInfoMDv2, e.Timer.Interval);
    }

    /// <returns>
    /// <see cref="TimeSpan"/> representing the last time ping was received from <paramref name="nodeInfo" />;
    /// <c>null</c> if <paramref name="nodeInfo"/> is offline.
    /// </returns>
    internal bool UptimeOf(MachineInfo nodeInfo, out TimeSpan? uptime)
    {
        if (NodesOnline.TryGetStorageTimer(nodeInfo, out var storageTimer))
        { uptime = storageTimer.Uptime; return true; }
        else { uptime = null; return false; }
    }

    internal int TryRemoveNodesWithNames(params string[] nodeNames)
    {
        var namesToRemove = nodeNames.Select(nodeName => nodeName.CaseInsensitive());
        int removedNodes = 0;
        lock (_lock)
        {
            foreach (var node in AllNodes)
            {
                if (node.NameContainsAny(namesToRemove) && NodesOffline.Contains(node))
                { AllNodes.Remove(node); removedNodes++; }
            }
        }
        return removedNodes;
    }

    internal IEnumerable<MachineInfo> GetNodesByName(string nodeNameStart) =>
        AllNodes.Where(node => node.NodeName.CaseInsensitive().StartsWith(nodeNameStart.CaseInsensitive()));
}

static class IConfigurationExtensions
{
    internal static double ReadIdleTimeBeforeGoingOfflineFrom(this IConfiguration configuration, ILogger logger)
    {
        const string configKey = "TimeBeforeNodeGoesOffline";
        double result = TimeSpan.FromMinutes(10).TotalMilliseconds;
        try
        {
            result = double.Parse(configuration[configKey]);
        }
        catch (ArgumentNullException ex)
        {
            logger.LogWarning(ex, "\"{ConfigKey}\" config key is not defined.", configKey);
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "Value of \"{ConfigKey}\" can't be parsed as double.", configKey);
        }
        catch (OverflowException ex)
        {
            logger.LogWarning(ex, "Value of \"{ConfigKey}\" overflows.", configKey);
        }
        return result;
    }
}
