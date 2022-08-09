global using System.Collections.Immutable;
global using Common;
global using Common.NodeToUI;
global using Machine;
global using Node.Tasks.Exec;
global using Node.Tasks.Executor;
global using Node.Tasks.Models;
global using Node.Tasks.Watching;
global using Serilog;
using System.Diagnostics;
using Machine.Plugins;
using Machine.Plugins.Discoverers;
using Machine.Plugins.Plugins;
using Node;
using Node.Listeners;
using Node.Profiling;
using Node.UserSettings;

var halfrelease = args.Contains("release");
Logging.Configure();
Init.Initialize();

if (!Debugger.IsAttached)
    FileList.KillNodeUI();

_ = new ProcessesingModeSwitch().StartMonitoringAsync();
await InitializePlugins();

new LocalListener().Start();

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

if (!Init.IsDebug || halfrelease)
    PortForwarder.Initialize();

var captured = new List<object>();

if (!Init.IsDebug || halfrelease)
{
    if (!Init.IsDebug)
    {
        SystemService.Start();

        var reepoHeartbeat = new Heartbeat(
            new HttpRequestMessage(HttpMethod.Post, $"{Settings.ServerUrl}/node/ping") { Content = await MachineInfo.AsJsonContentAsync() },
            TimeSpan.FromMinutes(5), Api.Client);
        _ = reepoHeartbeat.StartAsync();

        captured.Add(reepoHeartbeat);

        //(await Api.Client.PostAsync($"{Settings.ServerUrl}/node/profile", Profiler.Run())).EnsureSuccessStatusCode();
    }

    var userSettingsHeartbeat = new Heartbeat(new UserSettingsManager(Api.Client), TimeSpan.FromMinutes(1), Api.Client);
    _ = userSettingsHeartbeat.StartAsync();

    captured.Add(userSettingsHeartbeat);

    var mPlusTaskManagerHeartbeat = new Heartbeat(
        new HttpRequestMessage(HttpMethod.Post, $"{Api.TaskManagerEndpoint}/pheartbeat") { Content = await Profiler.RunAsync() },
        TimeSpan.FromMinutes(1), Api.Client);
    _ = mPlusTaskManagerHeartbeat.StartAsync();

    captured.Add(mPlusTaskManagerHeartbeat);
}

var taskreceiver = new TaskReceiver(Api.Client);
taskreceiver.StartAsync().Consume();

new PublicListener().Start();
new NodeStateListener().Start();
if (Init.IsDebug) new DebugListener().Start();

PortForwarding.GetPublicIPAsync().ContinueWith(async t =>
{
    var ip = t.Result.ToString();
    Log.Information($"Public IP: {ip}; Public port: {PortForwarding.Port}; Web server port: {PortForwarding.ServerPort}");

    var ports = new[] { PortForwarding.Port, PortForwarding.ServerPort };
    foreach (var port in ports)
    {
        var open = await PortForwarding.IsPortOpenAndListening(ip, port).ConfigureAwait(false);

        if (open) Log.Information($"Port {port} is open and listening");
        else Log.Error($"Port {port} is either not open or not listening");
    }
}).Consume();


/*NodeSettings.WatchingTasks.Add(WatchingTask.Create(
    new LocalWatchingTaskSource("/tmp/ae"),
    FFMpegTasks.EditVideo,
    new() { Hflip = true, },
    new MPlusWatchingTaskOutputInfo("rep_outputdir")
));*/


if (NodeSettings.WatchingTasks.Count != 0)
{
    Log.Information($"Found {NodeSettings.WatchingTasks.Count} watching tasks, starting...");

    foreach (var task in NodeSettings.WatchingTasks)
        task.StartWatcher();
}

if (NodeSettings.SavedTasks.Count != 0)
{
    Log.Information($"Found {NodeSettings.SavedTasks.Count} saved tasks, starting...");

    // .ToArray() to not cause exception while removing tasks
    foreach (var task in NodeSettings.SavedTasks.ToArray())
    {
        try { await TaskHandler.HandleAsync(task, Api.Client).ConfigureAwait(false); }
        finally
        {
            task.LogInfo("Removing");
            NodeSettings.SavedTasks.Remove(task);
        }
    }
}

Thread.Sleep(-1);


async Task InitializePlugins()
{
    PluginsManager.RegisterPluginDiscoverers(
        new BlenderPluginDiscoverer(),
        new Autodesk3dsMaxPluginDiscoverer(),
        new TopazGigapixelAIPluginDiscoverer(),
        new DaVinciResolvePluginDiscoverer(),
        new FFmpegPluginDiscoverer(),
        new PythonPluginDiscoverer(),
        new PythonEsrganPluginDiscoverer()
    );

    var plugins = await MachineInfo.DiscoverInstalledPluginsInBackground();
    Task.Run(() => Log.Information($"Found {plugins.Count} installed plugins:\n{string.Join(Environment.NewLine, plugins.Select(x => $"{x.Type} {x.Version}: {Path.GetFullPath(x.Path)}"))}")).Consume();
}
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
