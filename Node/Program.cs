global using Common;
global using Machine;
global using Serilog;
using System.Diagnostics;
using Node;
using Node.Profiler;

var http = new HttpClient() { BaseAddress = new(Settings.ServerUrl) };

AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
{
    try { Log.Error(ex.ExceptionObject?.ToString() ?? "null unhandled exception"); }
    catch { }

    try { File.WriteAllText(Path.Combine(Init.ConfigDirectory, "unhexp"), ex.ExceptionObject?.ToString()); }
    catch
    {
        try { File.WriteAllText(Path.GetTempPath(), ex.ExceptionObject?.ToString()); }
        catch { }
    }
};
TaskScheduler.UnobservedTaskException += (obj, ex) =>
{
    try { Log.Error(ex.Exception.ToString()); }
    catch { }

    try { File.WriteAllText(Path.Combine(Init.ConfigDirectory, "unhexpt"), ex.Exception.ToString()); }
    catch
    {
        try { File.WriteAllText(Path.GetTempPath(), ex.Exception.ToString()); }
        catch { }
    }
};

if (!Debugger.IsAttached)
    FileList.KillNodeUI();

var api = new Api();

if (!Init.IsDebug)
{
    var uinfor = await Authenticate(CancellationToken.None).ConfigureAwait(false);
    if (uinfor is null)
    {
        await AuthenticateWithUI(CancellationToken.None).ConfigureAwait(false);
        return;
    }
    var uinfo = uinfor.Value;
}

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

    var sessionManager = new SessionManager("mephisto123@gmail.com", "123", "https://tasks.microstock.plus", http);
    Settings.SessionId = await sessionManager.LoginAsync();
    Settings.Username ??= await sessionManager.RequestNicknameAsync();

    var profiler = new NodeProfiler(http);
    var benchmarkResults = await NodeProfiler.RunBenchmarksAsyncIfBenchmarkVersionWasUpdated(1073741824);

    _ = profiler.SendNodeProfileAsync($"{Settings.ServerUrl}/node/profile", benchmarkResults);
    // Move domain to Settings.ServerUrl when the server on VPS will be integrated to this server.
    _ = profiler.SendNodeProfileAsync($"https://tasks.microstock.plus/rphtaskmgr/pheartbeat", benchmarkResults);
}

_ = new ProcessesingModeSwitch().StartMonitoring();
_ = Listener.StartLocalListenerAsync();
_ = Listener.StartPublicListenerAsync();

Thread.Sleep(-1);


async Task<UserInfo> AuthenticateWithUI(CancellationToken token)
{
    Process.Start(new ProcessStartInfo(FileList.GetNodeUIExe()));

    Console.WriteLine(@$"You are not authenticated. Please use NodeUI app to authenticate or create auth.txt file in {Path.GetFullPath(".")} with your login and password divided by space");
    Console.WriteLine(@$"Example: ""makov@gmail.com password123""");

    while (true)
    {
        await Task.Delay(1000);

        Settings.BSessionId.Reload();
        if (Settings.SessionId is null) continue;

        Settings.BUserId.Reload();
        if (Settings.UserId is null) break;

        Settings.BUsername.Reload();
        if (Settings.Username is null) break;

        Process.Start(Environment.ProcessPath!);
        break;
    }

    Environment.Exit(0);
    return default;
}
async Task<UserInfo?> Authenticate(CancellationToken token)
{
    // either check sid
    if (Settings.SessionId is not null)
    {
        while (true)
        {
            var auth = await OperationResult.WrapException(() => api.CheckAuthenticationAsync(Settings.SessionId, token)).ConfigureAwait(false);
            auth.LogIfError();

            if (!auth)
            {
                if (auth.Message?.Contains("443") ?? false)
                {
                    await Task.Delay(1000);
                    continue;
                }

                break;
            }

            return auth.Value;
        }
    }

    // or try to auth from auth.txt
    string? file = null;
    if (File.Exists(file = "auth.txt") || File.Exists(file = "../auth.txt"))
    {
        var data = File.ReadAllText(file).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (data.Length == 2)
        {
            var auth = await api.AuthenticateAsync(data[0], data[1], token).ConfigureAwait(false);
            auth.LogIfError();

            if (auth)
            {
                auth.Result.SaveToConfig();

                var userinfo = await api.CheckAuthenticationAsync(Settings.SessionId!, token).ConfigureAwait(false);
                if (userinfo) return userinfo.Value;
            }
        }
    }

    return default;
}