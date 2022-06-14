global using Common;
global using Hardware;
global using Serilog;
using System.Diagnostics;
using Node;


AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
{
    try { File.WriteAllText(Path.Combine(Init.ConfigDirectory, "unhexp"), ex.ExceptionObject?.ToString()); }
    catch
    {
        try { File.WriteAllText(Path.GetTempPath(), ex.ExceptionObject?.ToString()); }
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

var http = new HttpClient() { BaseAddress = new(Settings.ServerUrl) };
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

    Log.Debug("Retrieveing hardware info...");
    _ = new ServerPinger($"{Settings.ServerUrl}/node/ping", TimeSpan.FromMinutes(5), http).StartAsync();
}

_ = Listener.StartLocalListenerAsync();
_ = Listener.StartPublicListenerAsync();

Thread.Sleep(-1);


//async Task SendHardwareInfo()
//{
//    Log.Debug("Requesting hardware info message verbosity level from the server...");
//    var isVerbose = await http.GetFromJsonAsync<bool>("node/hardware_info/is_verbose");
//    var hardwareInfoMessage = await hardwareInfo.ToTelegramMessageAsync(isVerbose);
//    Log.Information("Sending hardware info to {Server}", http.BaseAddress);
//    try
//    {
//        var response = await http.PostAsJsonAsync($"node/hardware_info", hardwareInfoMessage);
//        response.EnsureSuccessStatusCode();
//    }
//    catch (HttpRequestException ex)
//    {
//        Log.Error(ex, "Sending hardware info to the server resulted in {StatusCode} status code.", ex.StatusCode);
//    }
//}


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