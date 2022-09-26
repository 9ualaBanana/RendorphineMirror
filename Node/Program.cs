global using System.Collections.Immutable;
global using Common;
global using Common.Tasks;
global using Common.Tasks.Model;
global using Common.Tasks.Watching;
global using Machine;
global using NLog;
global using Node.Tasks.Exec;
global using Node.Tasks.Handlers;
global using Node.Tasks.Watching;
global using NodeToUI;
using System.Diagnostics;
using Common.Heartbeat;
using Common.NodeUserSettings;
using Node;
using Node.Listeners;
using Node.Plugins;
using Node.Plugins.Discoverers;
using Node.Profiling;

var halfrelease = args.Contains("release");
Init.Initialize();
var logger = LogManager.GetCurrentClassLogger();

if (!Debugger.IsAttached)
    FileList.KillNodeUI();

_ = new ProcessesingModeSwitch().StartMonitoringAsync();
await Task.WhenAll(
    InitializePlugins(),
    UpdatePublicPorts()
);

new LocalListener().Start();

if (Settings.SessionId is not null)
{
    logger.Info($"Session ID is present. Email: {Settings.Email}; User ID: {Settings.UserId}; {(Settings.IsSlave == true ? "slave" : "non-slave")}");

    if (Settings.UserId is null)
    {
        logger.Info("User ID is null, relogging in a slave mode");

        // node name should stay the same
        var auth = await SessionManager.AutoAuthAsync(Settings.Email);
        auth.LogIfError();
    }

    if (Settings.SessionId is not null && !Debugger.IsAttached)
        try { Process.Start(new ProcessStartInfo(FileList.GetNodeUIExe(), "hidden")); }
        catch { }
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

    var userSettingsHeartbeat = new Heartbeat(new PluginsUpdater(), TimeSpan.FromMinutes(1), Api.Client);
    _ = userSettingsHeartbeat.StartAsync();

    captured.Add(userSettingsHeartbeat);
}

new TaskReceiver().Start();
new DirectUploadListener().Start();
new DirectDownloadListener().Start();

new PublicListener().Start();
new TaskListener().Start();
new NodeStateListener().Start();
new DirectoryDiffListener().Start();
new DownloadListener().Start();
new PublicPagesListener().Start();

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


logger.Info(@$"Tasks found
    {NodeSettings.CompletedTasks.Count} self-completed
    {NodeSettings.WatchingTasks.Count} watching
    {NodeSettings.QueuedTasks.Count} queued
    {NodeSettings.PlacedTasks.Count} placed
    {NodeSettings.PlacedTasks.Bindable.Count(x => !x.State.IsFinished())} non-finished placed
".TrimLines().Replace("\n", "; "));



Task.WhenAll(Enum.GetValues<TaskState>().Select(s => Apis.GetMyTasksAsync(s).Then(x => (s, x).AsOpResult()).AsTask()))
    .Then(items =>
    {
        logger.Info($"Server tasks: {string.Join(", ", items.Select(oplistr => $"{oplistr.ThrowIfError().s}: {oplistr.ThrowIfError().x.ThrowIfError().Length}"))}");
        return true;
    })
    .AsTask().Consume();


TaskRegistration.TaskRegistered += NodeSettings.PlacedTasks.Bindable.Add;

TaskHandler.InitializePlacedTasksAsync().Consume();
TaskHandler.StartUpdatingTaskState();
TaskHandler.StartWatchingTasks();
TaskHandler.StartListening();

Thread.Sleep(-1);


/// <summary> Check settings-saved public ports and change them if someone is already listening </summary>
async Task UpdatePublicPorts()
{
    var publicip = await PortForwarding.GetPublicIPAsync();

    foreach (var port in new[] { Settings.BLocalListenPort, Settings.BUPnpPort, Settings.BUPnpServerPort })
    {
        logger.Warn($"Checking port {port.Value} for availability");

        while (true)
        {
            var open = await PortForwarding.IsPortOpenAndListening(publicip.ToString(), port.Value, new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);
            if (!open) break;

            logger.Warn($"Port {port.Value} is already listening, skipping");
            port.Value++;
        }
    }
}
async Task InitializePlugins()
{
    Directory.CreateDirectory("plugins");


    TaskList.Add(new[]
    {
        FFMpegTasks.CreateTasks(),
        EsrganTasks.CreateTasks(),
        VectorizerTasks.CreateTasks(),
        GenerateQSPreviewTasks.CreateTasks(),
    });

    PluginsManager.RegisterPluginDiscoverers(
        new BlenderPluginDiscoverer(),
        new Autodesk3dsMaxPluginDiscoverer(),
        new TopazGigapixelAIPluginDiscoverer(),
        new DaVinciResolvePluginDiscoverer(),
        new FFmpegPluginDiscoverer(),
        new PythonPluginDiscoverer(),
        new PythonEsrganPluginDiscoverer(),
        new VeeeVectorizerPluginDiscoverer()
    );

    TaskHandler.AddHandlers(
        new MPlusTaskHandler(),
        new DownloadLinkTaskHandler(),
        new TorrentTaskHandler(),
        new QSPreviewTaskHandler(),
        new DirectUploadTaskHandler()
    );

    var plugins = await MachineInfo.DiscoverInstalledPluginsInBackground();
    Task.Run(() => logger.Info($"Found {{Plugins}} installed plugins:\n{string.Join(Environment.NewLine, plugins.Select(x => $"{x.Type} {x.Version}: {Path.GetFullPath(x.Path)}"))}", plugins.Count)).Consume();

}
async ValueTask AuthWithGui()
{
    logger.Warn(@$"You are not authenticated. Please use NodeUI app to authenticate");
    Process.Start(new ProcessStartInfo(FileList.GetNodeUIExe()));

    if (File.Exists("login"))
    {
        var data = File.ReadAllText("login").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var login = data[0];
        var password = data[1];

        await SessionManager.AuthAsync(login, password).ThrowIfNull();
        return;
    }

    while (true)
    {
        await Task.Delay(1000).ConfigureAwait(false);

        if (Settings.SessionId is null) continue;
        if (Settings.NodeName is null) continue;
        if (Settings.Guid is null) continue;
        if (Settings.UserId is null) continue;

        return;
    }
}
