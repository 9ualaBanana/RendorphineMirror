﻿using Node.Plugins;
using System.Net;
using System.Net.Http.Json;

namespace Node;

public record MachineInfo
{
    public static string UserId => Settings.UserId!;
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
    readonly public static string Port = Settings.UPnpPort.ToString();
    readonly public static string WebServerPort = Settings.UPnpServerPort.ToString();
    public static async Task<string> GetBriefInfoAsync() => $"{NodeName} {PCName} (v.{Version}) | {await GetPublicIPAsync()}:{Port}";

    //public static async Task<string> ToTelegramMessageAsync(bool verbose = false)
    //{
    //    if (!verbose) return await GetBriefInfoAsync();

    //    if (OperatingSystem.IsWindows()) return WindowsHardwareInfoMessage.Build();
    //    throw new PlatformNotSupportedException();
    //}

    public static async Task<JsonContent> AsJsonContentAsync(PluginManager pluginManager) => JsonContent.Create(new
    {
        UserId,
        NodeName,
        PCName,
        UserName,
        Guid,
        Version,
        IP = (await GetPublicIPAsync()).ToString(),
        Port,
        WebServerPort,
        InstalledPlugins = await pluginManager.GetInstalledPluginsAsync(),
    });
}