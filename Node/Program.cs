global using Common;
global using Machine;
global using Serilog;
using System.Diagnostics;
using Node;
using Node.Plugins;
using Node.Plugins.Discoverers;
using Node.Profiler;

var http = new HttpClient() { BaseAddress = new(Settings.ServerUrl) };

PluginsManager.RegisterPluginDiscoverers(
    new BlenderPluginDiscoverer(),
    new Autodesk3dsMaxPluginDiscoverer(),
    new TopazGigapixelAIPluginDiscoverer(),
    new DaVinciResolvePluginDiscoverer()
    );
var discoveringPlugins = PluginsManager.DiscoverInstalledPluginsInBackground();

if (!Debugger.IsAttached)
    FileList.KillNodeUI();

var sessionManager = new SessionManager(http);

_ = Listener.StartLocalListenerAsync();
if (!Init.IsDebug) await Authenticate().ConfigureAwait(false);

PortForwarding.Initialize();
_ = PortForwarding.GetPublicIPAsync().ContinueWith(t =>
{
    Console.WriteLine($"UPnP was {(PortForwarding.Initialized ? null : "not ")}initialized");
    Console.WriteLine($"Public IP: {t.Result}:{PortForwarding.Port}");
});


if (!Debugger.IsAttached)
    Process.Start(new ProcessStartInfo(FileList.GetNodeUIExe(), "hidden"));

if (!Init.IsDebug)
{
    SystemService.Start();

    _ = new ServerPinger($"{Settings.ServerUrl}/node/ping", TimeSpan.FromMinutes(5), http).StartAsync();

    var profiler = new NodeProfiler(http);
    var benchmarkResults = await NodeProfiler.RunBenchmarksAsyncIfBenchmarkVersionWasUpdated(1073741824/*1GB*/);

    _ = profiler.SendNodeProfileAsync($"{Settings.ServerUrl}/node/profile", benchmarkResults);
    // Move domain to Settings.ServerUrl when the server on VPS will be integrated to this server.
    _ = profiler.SendNodeProfileAsync($"https://tasks.microstock.plus/rphtaskmgr/pheartbeat", benchmarkResults);

    var pluginsManager = new PluginsManager(await discoveringPlugins);
    _ = pluginsManager.SendInstalledPluginsInfoAsync($"{Settings.ServerUrl}/node/plugins", http);
}

_ = new ProcessesingModeSwitch().StartMonitoringAsync();
_ = Listener.StartPublicListenerAsync();

Thread.Sleep(-1);


async Task WaitForAuth(CancellationToken token)
{
    Process.Start(new ProcessStartInfo(FileList.GetNodeUIExe()));

    Console.WriteLine(@$"You are not authenticated. Please use NodeUI app to authenticate or create auth.txt file in {Path.GetFullPath(".")} with your login and password divided by space");
    Console.WriteLine(@$"Example: ""makov@gmail.com password123""");

    while (true)
    {
        await Task.Delay(1000).ConfigureAwait(false);

        if (Settings.SessionId is null) continue;
        if (Settings.NodeName is null) break;

        Process.Start(Environment.ProcessPath!);
        break;
    }

    Environment.Exit(0);
    throw new Exception();
}
async ValueTask Authenticate()
{
    var check = await sessionManager.CheckAsync().ConfigureAwait(false);
    check.LogIfError();
    if (check) return;

    await Auth().ConfigureAwait(false);
}
async ValueTask<OperationResult> Auth()
{
    // or try to auth from auth.txt
    // string? file = null;
    //if (File.Exists(file = "auth.txt") || File.Exists(file = "../auth.txt"))
    {
        // TODO: remove/fix when registration is done
        var data = new[] { "mephisto123@gmail.com", "123" };

        // var data = File.ReadAllText(file).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (data.Length < 2) Log.Error("Not enough arguments in auth.txt");
        else
        {
            var auth = await sessionManager.AuthAsync(data[0], data[1]).ConfigureAwait(false);
            auth.LogIfError();
            if (auth) return true;
        }
    }

    await WaitForAuth(CancellationToken.None).ConfigureAwait(false);
    return OperationResult.Err();
}