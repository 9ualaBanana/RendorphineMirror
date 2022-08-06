global using Common;
global using Machine.Plugins.Plugins;
using System.Net;
using System.Net.Http.Json;
using Machine.MessageBuilders;
using Machine.Plugins;

namespace Machine;

public record MachineInfo
{
    public static string NodeName => Settings.NodeName!;
    readonly public static string UserName = Environment.UserName;
    readonly public static string PCName = Environment.MachineName;
    readonly public static string Guid = Settings.Guid!;
    readonly public static string Version = Init.Version;
    static IPAddress? _publicIP;
    public static async Task<IPAddress> GetPublicIPAsync()
    {
        try { return _publicIP ??= await PortForwarding.GetPublicIPAsync(); }
        catch (Exception) { return IPAddress.None; }
    }
    readonly public static string Port = PortForwarding.Port.ToString();
    static IEnumerable<Plugin>? _installedPlugins;
    public static async Task<IEnumerable<Plugin>> DiscoverInstalledPluginsInBackground() =>
        _installedPlugins ??= await PluginsManager.DiscoverInstalledPluginsInBackground();
    public static IEnumerable<Plugin> DiscoverInstalledPlugins() =>
        _installedPlugins ??= PluginsManager.DiscoverInstalledPlugins();
    public static async Task<string> GetBriefInfoAsync() => $"{NodeName} {PCName} (v.{Version}) | {await GetPublicIPAsync()}:{Port}";

    public static async Task<string> ToTelegramMessageAsync(bool verbose = false)
    {
        if (!verbose) return await GetBriefInfoAsync();

        if (OperatingSystem.IsWindows()) return WindowsHardwareInfoMessage.Build();
        throw new PlatformNotSupportedException();
    }

    public static async Task<JsonContent> AsJsonContentAsync() => JsonContent.Create(new
    {
        NodeName,
        PCName,
        UserName,
        Guid,
        Version,
        IP = (await GetPublicIPAsync()).ToString(),
        Port,
        InstalledPlugins = await DiscoverInstalledPluginsInBackground(),
    });
}
