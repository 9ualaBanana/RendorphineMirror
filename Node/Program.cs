global using System.Collections.Immutable;
global using Common;
global using Common.NodeToUI;
global using Common.Tasks;
global using Common.Tasks.Model;
global using Machine;
global using NLog;
global using Node.Plugins;
global using Node.Registry;
global using Node.Tasks.Exec;
global using Node.Tasks.Executor;
global using Node.Tasks.Models;
global using Node.Tasks.Watching;
using System.Diagnostics;
using Node;
using Node.Listeners;
using Node.Plugins.Discoverers;
using Node.Profiling;
using Node.UserSettings;

var halfrelease = args.Contains("release");
Init.Initialize();
var logger = LogManager.GetCurrentClassLogger();

if (!Debugger.IsAttached)
    FileList.KillNodeUI();

_ = new ProcessesingModeSwitch().StartMonitoringAsync();
await InitializePlugins();

new LocalListener().Start();

if (Settings.SessionId is not null)
{
    if (!Debugger.IsAttached)
        Process.Start(new ProcessStartInfo(FileList.GetNodeUIExe(), "hidden"));
    logger.Info("Already authenticated. Email: {Email}", Settings.Email);
}
else
{
    await AuthWithGui().ConfigureAwait(false);
    logger.Info("Authentication completed");
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

    var mPlusTaskManagerHeartbeat = new Heartbeat(
        new HttpRequestMessage(HttpMethod.Post, $"{Api.TaskManagerEndpoint}/pheartbeat") { Content = await Profiler.RunAsync() },
        TimeSpan.FromMinutes(1), Api.Client);
    _ = mPlusTaskManagerHeartbeat.StartAsync();

    captured.Add(mPlusTaskManagerHeartbeat);

    var userSettingsHeartbeat = new Heartbeat(new UserSettingsManager(Api.Client), TimeSpan.FromMinutes(1), Api.Client);
    _ = userSettingsHeartbeat.StartAsync();

    captured.Add(userSettingsHeartbeat);
}

new PublicListener().Start();
new TaskReceiver().Start();
new NodeStateListener().Start();
new DirectoryDiffListener().Start();
if (Init.IsDebug) new DebugListener().Start();

PortForwarding.GetPublicIPAsync().ContinueWith(async t =>
{
    var ip = t.Result.ToString();
    logger.Info("Public IP: {Ip}; Public port: {PublicPort}; Web server port: {ServerPort}", ip, PortForwarding.Port, PortForwarding.ServerPort);

    var ports = new[] { PortForwarding.Port, PortForwarding.ServerPort };
    foreach (var port in ports)
    {
        var open = await PortForwarding.IsPortOpenAndListening(ip, port).ConfigureAwait(false);

        if (open) logger.Info("Port {Port} is open and listening", port);
        else logger.Error("Port {Port} is either not open or not listening", port);
    }
}).Consume();

TaskRegistration.TaskRegistered += NodeSettings.PlacedTasks.Bindable.Add;

/*NodeSettings.WatchingTasks.Add(WatchingTask.Create(
    new LocalWatchingTaskSource("/tmp/ae"),
    FFMpegTasks.EditVideo,
    new() { Hflip = true, },
    new MPlusWatchingTaskOutputInfo("rep_outputdir")
));*/



if (NodeSettings.WatchingTasks.Count != 0)
{
    logger.Info("Found {WatchingTasksCount} watching tasks, starting...", NodeSettings.WatchingTasks.Count);

    foreach (var task in NodeSettings.WatchingTasks.Bindable)
        task.StartWatcher();
}

if (NodeSettings.SavedTasks.Count != 0)
{
    logger.Info("Found {SavedTasksCount} saved tasks, starting...", NodeSettings.SavedTasks.Count);

    // .ToArray() to not cause exception while removing tasks
    foreach (var task in NodeSettings.SavedTasks.Bindable.ToArray())
    {
        try { await TaskHandler.HandleAsync(task).ConfigureAwait(false); }
        finally
        {
            task.LogInfo("Removing");
            NodeSettings.SavedTasks.Bindable.Remove(task);
        }
    }
}

Thread.Sleep(-1);


async Task InitializePlugins()
{
    TaskList.Initialize();
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
    Task.Run(() => logger.Info($"Found {{Plugins}} installed plugins:\n{string.Join(Environment.NewLine, plugins.Select(x => $"{x.Type} {x.Version}: {Path.GetFullPath(x.Path)}"))}", plugins.Count)).Consume();
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
