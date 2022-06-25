global using Common;
global using Machine;
global using Serilog;
using System.Diagnostics;
using Machine.Plugins;
using Machine.Plugins.Discoverers;
using Node;
using Node.Profiler;

var http = new HttpClient() { BaseAddress = new(Settings.ServerUrl) };

PluginsManager.RegisterPluginDiscoverers(
    new BlenderPluginDiscoverer(),
    new Autodesk3dsMaxPluginDiscoverer(),
    new TopazGigapixelAIPluginDiscoverer(),
    new DaVinciResolvePluginDiscoverer()
);
var discoveringInstalledPlugins = MachineInfo.DiscoverInstalledPluginsInBackground();

if (!Debugger.IsAttached)
    FileList.KillNodeUI();

_ = Listener.StartLocalListenerAsync();

// TODO: probably remove \/
if (Settings.Email is null) Settings.SessionId = null;

if (Settings.SessionId is not null)
{
    if (!Debugger.IsAttached)
        Process.Start(new ProcessStartInfo(FileList.GetNodeUIExe(), "hidden"));
    Log.Information($"Already authenticated. Email: {Settings.Email}");
}
else
{
    await AuthWithGui().ConfigureAwait(false);
    Log.Information($"Authentication completed");
}

PortForwarder.Initialize();
_ = PortForwarding.GetPublicIPAsync().ContinueWith(t => Log.Information($"Public IP: {t.Result}:{PortForwarding.Port}"));

if (!Init.IsDebug)
{
    SystemService.Start();

    await discoveringInstalledPlugins;
    _ = new ServerPinger($"{Settings.ServerUrl}/node/ping", TimeSpan.FromMinutes(5), http).StartAsync();

    var nodeProfiler = new NodeProfiler(http);
    var benchmarkResults = await NodeProfiler.RunBenchmarksAsyncIfBenchmarkVersionWasUpdated(1073741824/*1GB*/);
    var benchmarkPayload = await NodeProfiler.GetPayloadAsync(benchmarkResults);

    new NodeProfiler(http).SendNodeProfileAsync($"{Settings.ServerUrl}/node/profile", benchmarkResults);
    // Move domain to Settings.ServerUrl when the server on VPS will be integrated to this server.
    new NodeProfiler(http).SendNodeProfileAsync($"https://tasks.microstock.plus/rphtaskmgr/pheartbeat", benchmarkResults, TimeSpan.FromMinutes(1));
}

_ = new ProcessesingModeSwitch().StartMonitoringAsync();
_ = Listener.StartPublicListenerAsync();

Thread.Sleep(-1);


async ValueTask AuthWithGui()
{
    Console.WriteLine(@$"You are not authenticated. Please use NodeUI app to authenticate");
    Process.Start(new ProcessStartInfo(FileList.GetNodeUIExe()));

    while (true)
    {
        await Task.Delay(1000).ConfigureAwait(false);

        if (Settings.SessionId is null) continue;
        if (Settings.NodeName is null) continue;

        return;
    }
}