global using System.Collections.Immutable;
global using Common;
global using Common.NodeToUI;
global using Common.Tasks.Tasks;
global using Machine;
global using Node.P2P;
global using Node.Tasks.Exec;
global using Node.Tasks.Executor;
global using Node.Tasks.Models;
global using Serilog;
using System.Diagnostics;
using Machine.Plugins;
using Machine.Plugins.Discoverers;
using Node;
using Node.Profiler;

var halfrelease = args.Contains("release");
Init.Initialize();

_ = new ProcessesingModeSwitch().StartMonitoringAsync();

var http = new HttpClient() { BaseAddress = new(Settings.ServerUrl) };

PluginsManager.RegisterPluginDiscoverers(
    new BlenderPluginDiscoverer(),
    new Autodesk3dsMaxPluginDiscoverer(),
    new TopazGigapixelAIPluginDiscoverer(),
    new DaVinciResolvePluginDiscoverer(),
    new FFMpegPluginDiscoverer()
);
var discoveringInstalledPlugins = MachineInfo.DiscoverInstalledPluginsInBackground();

if (!Debugger.IsAttached)
    FileList.KillNodeUI();

_ = Listener.StartLocalListenerAsync();

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

var captured = new List<object>();

// Precomputed for sending by NodeProfiler.
var plugins = await discoveringInstalledPlugins;
if (!Init.IsDebug || halfrelease)
{
    if (!Init.IsDebug)
    {
        SystemService.Start();

        var serverPinger = new ServerPinger($"{Settings.ServerUrl}/node/ping", TimeSpan.FromMinutes(5), http);
        _ = serverPinger.StartAsync();

        captured.Add(serverPinger);
    }

    var nodeProfiler = new NodeProfiler(http);
    var benchmarkResults = await NodeProfiler.RunBenchmarksAsyncIfBenchmarkVersionWasUpdated(1073741824/*1GB*/);

    if (!Init.IsDebug)
    {
        var reepoProfiler = new NodeProfiler(http);
        await reepoProfiler.SendNodeProfile($"{Settings.ServerUrl}/node/profile", benchmarkResults);

        captured.Add(reepoProfiler);
    }

    // Move domain to Settings.ServerUrl when the server on VPS will be integrated to this server.
    var serverProfiler = new NodeProfiler(http);
    await serverProfiler.SendNodeProfile($"https://tasks.microstock.plus/rphtaskmgr/pheartbeat", benchmarkResults, TimeSpan.FromMinutes(1));
    captured.Add(serverProfiler);
}

var taskreceiver = new TaskReceiver();
taskreceiver.StartAsync().Consume();

_ = Listener.StartPublicListenerAsync();

if (NodeSettings.ActiveTasks.Count != 0)
{
    Log.Information($"Found {NodeSettings.ActiveTasks.Count} saved tasks, starting...");

    // .ToArray() to not cause exception while removing tasks
    foreach (var task in NodeSettings.ActiveTasks.ToArray())
    {
        try { await TaskHandler.HandleAsync(task).ConfigureAwait(false); }
        finally
        {
            task.LogInfo("Removing");
            NodeSettings.ActiveTasks.Remove(task);
        }
    }
}

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
        if (Settings.Guid is null) continue;

        return;
    }
}