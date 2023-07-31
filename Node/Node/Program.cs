global using System.Collections.Immutable;
global using Common;
global using Machine;
global using Microsoft.Extensions.Logging;
global using Node.Common;
global using Node.Common.Models;
global using Node.DataStorage;
global using Node.Plugins;
global using Node.Plugins.Models;
global using Node.Registry;
global using Node.Tasks;
global using Node.Tasks.Exec;
global using Node.Tasks.Handlers;
global using Node.Tasks.Models;
global using Node.Tasks.Watching;
global using NodeCommon;
global using NodeCommon.ApiModel;
global using NodeCommon.Tasks;
global using NodeCommon.Tasks.Model;
global using NodeCommon.Tasks.Watching;
global using NodeToUI;
global using Logger = NLog.Logger;
global using LogLevel = NLog.LogLevel;
global using LogManager = NLog.LogManager;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using Node;
using Node.Heartbeat;
using Node.Listeners;
using Node.Profiling;
using Node.Tasks.Exec.Actions;


Initializer.AppName = "renderfin";
ConsoleHide.Hide();

if (Path.GetFileNameWithoutExtension(Environment.ProcessPath!) != "dotnet")
    foreach (var proc in FileList.GetAnotherInstances())
        proc.Kill(true);

Init.Initialize();
var logger = LogManager.GetCurrentClassLogger();
var captured = new List<object>();

var builder = new ContainerBuilder();

// logging
builder.Populate(new ServiceCollection().With(services => services.AddLogging(l => l.AddNLog())));


builder.RegisterInstance(Api.Default)
    .SingleInstance();
builder.RegisterInstance(new NodeCommon.Apis(Api.Default, Settings.SessionId))
    .SingleInstance();

builder.RegisterInstance(Settings.Instance)
    .SingleInstance();

builder.RegisterType<NodeSettingsInstance>()
    .As<IQueuedTasksStorage>()
    .As<IPlacedTasksStorage>()
    .As<ICompletedTasksStorage>()
    .As<IWatchingTasksStorage>()
    .SingleInstance();


builder.RegisterType<ReconnectTarget>()
    .SingleInstance()
    .OnActivating(async t => await t.Instance.Execute());



var halfrelease = args.Contains("release");
var pluginManager = new PluginManager(PluginDiscoverers.GetAll());
builder.RegisterInstance(pluginManager)
    .AsSelf()
    .As<IInstalledPluginsProvider>()
    .SingleInstance()
    .OnActivating(async m => await m.Instance.GetInstalledPluginsAsync());

builder.RegisterType<SoftwareList>()
    .As<ISoftwareListProvider>()
    .SingleInstance();

builder.RegisterType<PluginChecker>()
    .SingleInstance();

builder.RegisterType<PluginDeployer>()
    .SingleInstance();

_ = new ProcessesingModeSwitch().StartMonitoringAsync();

{
    builder.RegisterType<LocalListener>()
        .SingleInstance()
        .OnActivating(async l =>
        {
            await UpdatePort("127.0.0.1", Settings.BLocalListenPort, "Local");
            l.Instance.Start();
        });

    await PortForwarding.GetPublicIPAsync()
        .ContinueWith(ip => Task.WhenAll(
            UpdatePort(ip.Result.ToString(), Settings.BUPnpPort, "Public"),
            UpdatePort(ip.Result.ToString(), Settings.BUPnpServerPort, "Server")
        )).Unwrap();

    InitializePlugins();
}

registerListener<NodeStateListener>();


if (Settings.SessionId is not null)
    logger.Info($"Session ID is present. Email: {Settings.Email ?? "<not saved>"}; User ID: {Settings.UserId}; {(Settings.IsSlave == true ? "slave" : "non-slave")}");
else
{
    await WaitForAuth().ConfigureAwait(false);
    logger.Info("Authentication completed");
}

builder.RegisterType<PortForwarder>()
    .SingleInstance()
    .OnActivating(p => p.Instance.Start());


builder.RegisterType<SystemTimerStartedTarget>()
    .SingleInstance()
    .OnActivating(p => p.Instance.Execute());

builder.RegisterType<MPlusHeartbeat>()
    .OnActivating(h => h.Instance.Start())
    .SingleInstance();

builder.RegisterType<TelegramBotHeartbeat>()
    .OnActivating(h => h.Instance.Start())
    .SingleInstance();

builder.RegisterType<UserSettingsHeartbeat>()
    .OnActivating(h => h.Instance.Start())
    .SingleInstance();


registerListener<TaskReceiver>();
registerListener<DirectUploadListener>();
registerListener<DirectDownloadListener>();

registerListener<PublicListener>();
registerListener<TaskListener>();
registerListener<DirectoryDiffListener>();
registerListener<DownloadListener>();
registerListener<PublicPagesListener>();

registerListener<DebugListener>();

void registerListener<T>() where T : ListenerBase =>
    builder.RegisterType<T>()
        .SingleInstance()
        .OnActivating(l => l.Instance.Start());

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

builder.RegisterInstance(NodeGlobalState.Instance)
    .SingleInstance();
builder.RegisterType<NodeGlobalStateInitializedTarget>()
    .SingleInstance()
    .OnActivating(s => s.Instance.Execute());

builder.RegisterType<TaskHandler2>()
    .SingleInstance()
    .OnActivating(c =>
    {
        c.Instance.InitializePlacedTasksAsync().Consume();
        c.Instance.StartUpdatingPlacedTasks();
        c.Instance.StartWatchingTasks();
        c.Instance.StartListening();
    });

builder.RegisterType<AutoCleanup>()
    .SingleInstance()
    .OnActivating(c => c.Instance.Start());

{
    builder.RegisterType<ServiceTargets.UI>()
        .SingleInstance();
    builder.RegisterType<ServiceTargets.PublicListeners>()
        .SingleInstance();
    builder.RegisterType<ServiceTargets.ReadyToExecuteTasks>()
        .SingleInstance();
    builder.RegisterType<ServiceTargets.ReadyToReceiveTasks>()
        .SingleInstance();
    builder.RegisterType<ServiceTargets.ConnectedToMPlus>()
        .SingleInstance();
    builder.RegisterType<ServiceTargets.Debug>()
        .SingleInstance();

    builder.RegisterType<ServiceTargets.BaseMain>()
        .SingleInstance();
}

var maintype = (Init.IsDebug, halfrelease) switch
{
    (true, false) => typeof(ServiceTargets.DebugMain),
    (true, true) => typeof(ServiceTargets.ReleaseMain),
    (false, _) => typeof(ServiceTargets.ReleaseMain),
};
builder.RegisterType(maintype)
    .SingleInstance()
    .AutoActivate();


var container = builder.Build(Autofac.Builder.ContainerBuildOptions.None);

Thread.Sleep(-1);
GC.KeepAlive(captured);
GC.KeepAlive(container);


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
void InitializePlugins()
{
    TaskList.Add(new IPluginAction[]
    {
        new EditRaster(), new EditVideo(),
        new EsrganUpscale(),
        new GreenscreenBackground(),
        new VeeeVectorize(),
        new GenerateQSPreview(),
        new GenerateTitleKeywords(),
        new GenerateImageByMeta(),
        new GenerateImageByPrompt(),
        new Topaz(),
    });

    TaskHandler.AutoInitializeHandlers();
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


class SoftwareList : ISoftwareListProvider
{
    public IReadOnlyDictionary<string, SoftwareDefinition> Software => NodeGlobalState.Instance.Software.Value;
}