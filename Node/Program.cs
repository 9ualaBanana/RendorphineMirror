global using System.Collections.Immutable;
global using Common;
global using Machine;
global using NLog;
global using Node.Plugins;
global using Node.Plugins.Deployment;
global using Node.Registry;
global using Node.Tasks;
global using Node.Tasks.Exec;
global using Node.Tasks.Handlers;
global using Node.Tasks.Watching;
global using NodeCommon;
global using NodeCommon.ApiModel;
global using NodeCommon.NodeUserSettings;
global using NodeCommon.Plugins;
global using NodeCommon.Plugins.Deployment;
global using NodeCommon.Tasks;
global using NodeCommon.Tasks.Model;
global using NodeCommon.Tasks.Watching;
global using NodeToUI;
using Newtonsoft.Json.Linq;
using Node;
using Node.Heartbeat;
using Node.Listeners;
using Node.Plugins.Discoverers;
using Node.Profiling;


ConsoleHide.Hide();

if (Path.GetFileNameWithoutExtension(Environment.ProcessPath!) != "dotnet")
    foreach (var proc in FileList.GetAnotherInstances())
        proc.Kill(true);

var halfrelease = args.Contains("release");
Init.Initialize();
var logger = LogManager.GetCurrentClassLogger();
InitializeSettings();

_ = new ProcessesingModeSwitch().StartMonitoringAsync();

{
    var localport = UpdatePort("127.0.0.1", Settings.BLocalListenPort, "Local")
        .ContinueWith(_ => new LocalListener().Start());

    var publicports = PortForwarding.GetPublicIPAsync()
        .ContinueWith(ip => Task.WhenAll(
            UpdatePort(ip.Result.ToString(), Settings.BUPnpPort, "Public"),
            UpdatePort(ip.Result.ToString(), Settings.BUPnpServerPort, "Server")
        )).Unwrap();

    var pluginsinit = InitializePlugins();

    await Task.WhenAll(
        pluginsinit,
        publicports,
        localport
    );
}

new NodeStateListener().Start();
if (Settings.SessionId is not null)
    logger.Info($"Session ID is present. Email: {Settings.Email ?? "<not saved>"}; User ID: {Settings.UserId}; {(Settings.IsSlave == true ? "slave" : "non-slave")}");
else
{
    await WaitForAuth().ConfigureAwait(false);
    logger.Info("Authentication completed");
}

if (!Init.IsDebug || halfrelease)
    PortForwarder.Initialize();

var captured = new List<object>();

if (!Init.IsDebug || halfrelease)
{
    if (!Init.IsDebug)
    {
        // removing old service
        try { SystemService.Stop("renderphinepinger"); }
        catch { }
        SystemService.Start();

        var reepoHeartbeat = new Heartbeat(
            new HttpRequestMessage(HttpMethod.Post, $"{Settings.ServerUrl}/node/ping") { Content = await MachineInfo.AsJsonContentAsync() },
            TimeSpan.FromMinutes(5), Api.Client);
        _ = reepoHeartbeat.StartAsync();

        captured.Add(reepoHeartbeat);

        //(await Api.Client.PostAsync($"{Settings.ServerUrl}/node/profile", Profiler.Run())).EnsureSuccessStatusCode();
    }

    var mPlusTaskManagerHeartbeat = new Heartbeat(new MPlusHeartbeatGenerator(), TimeSpan.FromMinutes(1), Api.Client);
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
new DirectoryDiffListener().Start();
new DownloadListener().Start();
new PublicPagesListener().Start();

if (Init.DebugFeatures) new DebugListener().Start();

PortForwarding.GetPublicIPAsync().ContinueWith(async t =>
{
    var ip = t.Result.ToString();
    logger.Info("Public IP: {Ip}; Public port: {PublicPort}; Web server port: {ServerPort}", ip, Settings.UPnpPort, Settings.UPnpServerPort);

    var ports = new[] { Settings.UPnpPort, Settings.UPnpServerPort };
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
    {NodeSettings.PlacedTasks.Values.Count(x => !x.State.IsFinished())} non-finished placed
".TrimLines().Replace("\n", "; ").Replace("\r", string.Empty));


NodeCommon.Tasks.TaskRegistration.TaskRegistered += NodeSettings.PlacedTasks.Add;

TaskHandler.InitializePlacedTasksAsync().Consume();
TaskHandler.StartUpdatingPlacedTasks();
TaskHandler.StartWatchingTasks();
TaskHandler.StartListening();

new Thread(() =>
{
    while (true)
    {
        OperationResult.WrapException(() => AutoCleanup.Start()).LogIfError();
        Thread.Sleep(60 * 60 * 24 * 1000);
    }
})
{ IsBackground = true }.Start();

new Thread(() =>
{
    while (true)
    {
        var root = Path.GetPathRoot(ReceivedTask.FSTaskDataDirectory());
        var drive = DriveInfo.GetDrives().First(d => d.RootDirectory.Name == root);

        if (drive.AvailableFreeSpace < 16L * 1024 * 1024 * 1024)
        {
            // creating new thread for logging in case of 0 bytes free space available
            // because then the logger wouldn't be able to write into log file and might just freeze
            // and not let the cleanup happen
            new Thread(() => logger.Info($"Low free space ({drive.AvailableFreeSpace / 1024 / 1024f} MB), starting a cleanup..")) { IsBackground = true }.Start();

            OperationResult.WrapException(() => AutoCleanup.CleanForLowFreeSpace()).LogIfError();
        }

        Thread.Sleep(60 * 1000);
    }
})
{ IsBackground = true }.Start();

Thread.Sleep(-1);
GC.KeepAlive(captured);


void InitializeSettings()
{
    var state = NodeGlobalState.Instance;

    state.WatchingTasks.Bind(NodeSettings.WatchingTasks.Bindable);
    state.PlacedTasks.Bind(NodeSettings.PlacedTasks.Bindable);
    NodeSettings.QueuedTasks.Bindable.SubscribeChanged(() => state.QueuedTasks.SetRange(NodeSettings.QueuedTasks.Values), true);
    NodeSettings.BenchmarkResult.Bindable.SubscribeChanged(() => state.BenchmarkResult.Value = NodeSettings.BenchmarkResult.Value is null ? null : JObject.FromObject(NodeSettings.BenchmarkResult.Value), true);
    state.TaskAutoDeletionDelayDays.Bind(NodeSettings.TaskAutoDeletionDelayDays.Bindable);

    state.BServerUrl.Bind(Settings.BServerUrl.Bindable);
    state.BLocalListenPort.Bind(Settings.BLocalListenPort.Bindable);
    state.BUPnpPort.Bind(Settings.BUPnpPort.Bindable);
    state.BUPnpServerPort.Bind(Settings.BUPnpServerPort.Bindable);
    state.BDhtPort.Bind(Settings.BDhtPort.Bindable);
    state.BTorrentPort.Bind(Settings.BTorrentPort.Bindable);
    state.BNodeName.Bind(Settings.BNodeName.Bindable);
    state.BAuthInfo.Bind(Settings.BAuthInfo.Bindable);


    Software.StartUpdating(null, default);
    Settings.BLocalListenPort.Bindable.SubscribeChanged(() => File.WriteAllText(Path.Combine(Init.ConfigDirectory, "lport"), Settings.LocalListenPort.ToString()), true);
}

/// <summary> Try to connect to the port and change it if someone is already listening there </summary>
async Task UpdatePort(string ip, DatabaseValue<ushort> port, string description)
{
    logger.Info($"[PORTCHECK] Checking {description.ToLowerInvariant()} port {port.Value}");

    while (true)
    {
        var open = await PortForwarding.IsPortOpenAndListening(ip, port.Value, new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);
        if (!open)
        {
            logger.Info($"[PORTCHECK] {description} port: {port.Value}");
            break;
        }

        logger.Warn($"[PORTCHECK] {description} port {port.Value} is already listening, skipping");
        port.Value++;
    }
}
async Task InitializePlugins()
{
    Directory.CreateDirectory("plugins");


    TaskList.Add(new[]
    {
        FFMpegTasks.CreateTasks(),
        EsrganTasks.CreateTasks(),
        RobustVideoMatting.CreateTasks(),
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
        new RobustVideoMattingPluginDiscoverer(),
        new VeeeVectorizerPluginDiscoverer(),
        new NvidiaDriverPluginDiscoverer(),
        new CondaPluginDiscoverer()
    );

    TaskHandler.AutoInitializeHandlers();

    var plugins = await MachineInfo.DiscoverInstalledPluginsInBackground();
    Task.Run(() => logger.Info($"Found {{Plugins}} installed plugins:\n{string.Join(Environment.NewLine, plugins.Select(x => $"{x.Type} {x.Version}: {(x.Path.Length == 0 ? "<nopath>" : Path.GetFullPath(x.Path))}"))}", plugins.Count)).Consume();
}
async ValueTask WaitForAuth()
{
    logger.Warn(@$"You are not authenticated. Please use NodeUI app to authenticate or create an 'login' file with username and password separated by newline");

    while (true)
    {
        await Task.Delay(1000).ConfigureAwait(false);
        if (File.Exists("login"))
        {
            var data = File.ReadAllText("login").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (data.Length < 2) continue;

            var login = data[0];
            var password = data[1];

            var auth = await SessionManager.AuthAsync(login, password);
            auth.LogIfError();
            if (!auth) continue;

            return;
        }

        if (Settings.SessionId is null) continue;
        if (Settings.NodeName is null) continue;
        if (Settings.Guid is null) continue;
        if (Settings.UserId is null) continue;

        return;
    }
}
