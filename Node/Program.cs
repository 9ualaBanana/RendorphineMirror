global using Common;
global using Machine;
global using Serilog;
using System.Diagnostics;
using Machine.Plugins;
using Machine.Plugins.Discoverers;
using Node;
using Node.Profiler;

var http = new HttpClient() { BaseAddress = new(Settings.ServerUrl) };
var profiler = new NodeProfiler(http);
var benchmarkResults = await NodeProfiler.RunBenchmarksAsyncIfBenchmarkVersionWasUpdated(1073741824/*1GB*/);

_ = profiler.SendNodeProfileAsync($"{Settings.ServerUrl}/node/profile", benchmarkResults);
// Move domain to Settings.ServerUrl when the server on VPS will be integrated to this server.
_ = profiler.SendNodeProfileAsync($"https://tasks.microstock.plus/rphtaskmgr/pheartbeat", benchmarkResults);
PluginsManager.RegisterPluginDiscoverers(
    new BlenderPluginDiscoverer(),
    new Autodesk3dsMaxPluginDiscoverer(),
    new TopazGigapixelAIPluginDiscoverer(),
    new DaVinciResolvePluginDiscoverer()
);
var discoveringInstalledPlugins = MachineInfo.DiscoverInstalledPluginsInBackground();

if (!Debugger.IsAttached)
    FileList.KillNodeUI();

var sessionManager = new SessionManager(http);

_ = Listener.StartLocalListenerAsync();
var autoauthenticated = await Authenticate().ConfigureAwait(false);
Log.Information($"{(autoauthenticated ? "Auto" : "Manual")} authentication completed");

PortForwarding.Initialize();
_ = PortForwarding.GetPublicIPAsync().ContinueWith(t =>
{
    Log.Information($"UPnP was {(PortForwarding.Initialized ? null : "not ")}initialized");
    Log.Information($"Public IP: {t.Result}:{PortForwarding.Port}");
});

if (autoauthenticated && !Debugger.IsAttached)
    Process.Start(new ProcessStartInfo(FileList.GetNodeUIExe(), "hidden"));

if (!Init.IsDebug)
{
    SystemService.Start();

    await discoveringInstalledPlugins;
    _ = new ServerPinger($"{Settings.ServerUrl}/node/ping", TimeSpan.FromMinutes(5), http).StartAsync();

    //var profiler = new NodeProfiler(http);
    //var benchmarkResults = await NodeProfiler.RunBenchmarksAsyncIfBenchmarkVersionWasUpdated(1073741824/*1GB*/);

    //_ = profiler.SendNodeProfileAsync($"{Settings.ServerUrl}/node/profile", benchmarkResults);
    //// Move domain to Settings.ServerUrl when the server on VPS will be integrated to this server.
    //_ = profiler.SendNodeProfileAsync($"https://tasks.microstock.plus/rphtaskmgr/pheartbeat", benchmarkResults);
}

_ = new ProcessesingModeSwitch().StartMonitoringAsync();
_ = Listener.StartPublicListenerAsync();

Thread.Sleep(-1);


// returns only when authenticated;
// returns true if sessionid was already valid; false if wasnt 
async ValueTask<bool> Authenticate()
{
    var check = await sessionManager.CheckAsync().ConfigureAwait(false);
    check.LogIfError();
    if (check) return check;

    return await Auth().ConfigureAwait(false);
}
async ValueTask<OperationResult> Auth()
{
    // or try to auth from auth.txt
    string? file = null;
    if (File.Exists(file = "auth.txt") || File.Exists(file = "../auth.txt"))
    {
        var data = File.ReadAllText(file).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (data.Length < 2) Log.Error("Not enough arguments in auth.txt");
        else
        {
            var auth = await sessionManager.AuthAsync(data[0], data[1]).ConfigureAwait(false);
            auth.LogIfError();
            if (auth) return true;
        }
    }

    return await WaitForAuth(CancellationToken.None).ConfigureAwait(false);
}
async Task<bool> WaitForAuth(CancellationToken token)
{
    Process.Start(new ProcessStartInfo(FileList.GetNodeUIExe()));

    Console.WriteLine(@$"You are not authenticated. Please use NodeUI app to authenticate or create auth.txt file in {Path.GetFullPath(".")} with your login and password divided by space");
    Console.WriteLine(@$"Example: ""makov@gmail.com password123""");

    while (true)
    {
        await Task.Delay(1000).ConfigureAwait(false);

        if (Settings.SessionId is null) continue;
        if (Settings.NodeName is null) continue;

        return false;
    }
}