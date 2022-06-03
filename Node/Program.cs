global using Common;
using Hardware;
using Node;
using Serilog;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;

var api = new Api();

var uinfor = await Authenticate(CancellationToken.None).ConfigureAwait(false);
if (uinfor is null)
{
    await AuthenticateWithUI(CancellationToken.None).ConfigureAwait(false);
    return;
}
var uinfo = uinfor.Value;

var http = new HttpClient() { BaseAddress = new(Settings.ServerUrl) };
PortForwarding.Initialize();
_ = PortForwarding.GetPublicIPAsync().ContinueWith(t =>
{
    Console.WriteLine($"UPnP was {(PortForwarding.Initialized ? null : "not ")}initialized");
    Console.WriteLine($"Public port: {PortForwarding.Port}; Public IP: {t.Result}");
});

Log.Debug("Retrieveing hardware info...");
var hardwareInfo = HardwareInfo.Get();

if (!Debugger.IsAttached)
{
    Process.Start(new ProcessStartInfo(FileList.GetNodeUIExe(), "hidden"));
    SystemService.Start();
    _ = SendHardwareInfo();
}

_ = StartHttpListenerAsync();
_ = new ServerPinger(hardwareInfo, TimeSpan.FromMinutes(5), http).Start();
Thread.Sleep(-1);


async Task SendHardwareInfo()
{
    Log.Debug("Requesting hardware info message verbosity level from the server...");
    var isVerbose = await http.GetFromJsonAsync<bool>("node/hardware_info/is_verbose");
    var hardwareInfoMessage = hardwareInfo.ToTelegramMessage(isVerbose);
    Log.Information("Sending hardware info to {server}", http.BaseAddress);
    try
    {
        var response = await http.PostAsJsonAsync($"node/hardware_info", hardwareInfoMessage);
        response.EnsureSuccessStatusCode();
    }
    catch (HttpRequestException ex)
    {
        Log.Error(ex, "Sending hardware info to the server resulted in {StatusCode} status code.", ex.StatusCode);
    }
}


// TODO: починить костыли бесят
async Task<UserInfo> AuthenticateWithUI(CancellationToken token)
{
    Process.Start(new ProcessStartInfo(FileList.GetNodeUIExe()));

    Console.WriteLine(@$"You are not authenticated. Please use NodeUI app to authenticate or create auth.txt file in {Directory.GetCurrentDirectory()} with your login and password divided by space");
    Console.WriteLine(@$"Example: ""makov@gmail.com password123""");

    var l = new HttpListener();
    l.Prefixes.Add(@$"http://127.0.0.1:{Settings.ListenPort}/");
    l.Start();
    Console.WriteLine(@$"Waiting for auth or exit...");
    var context = await l.GetContextAsync().ConfigureAwait(false);
    var request = context.Request;
    if (request.Url?.LocalPath.EndsWith("auth") ?? false)
        Process.Start(Environment.ProcessPath!);

    Environment.Exit(0);
    return default;
}
async Task<UserInfo?> Authenticate(CancellationToken token)
{
    // either check sid
    if (Settings.SessionId is not null)
    {
        var userinfo = await api.GetUserInfo(Settings.SessionId, token).ConfigureAwait(false);
        if (userinfo) return userinfo.Value;
    }

    // or try to auth from auth.txt
    string? file = null;
    if (File.Exists(file = "auth.txt") || File.Exists(file = "../auth.txt"))
    {
        var data = File.ReadAllText(file).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (data.Length == 2)
        {
            var auth = await api.AuthenticateAsync(data[0], data[1], token).ConfigureAwait(false);
            if (auth)
            {
                auth.Result.SaveToConfig();

                var userinfo = await api.GetUserInfo(Settings.SessionId!, token).ConfigureAwait(false);
                if (userinfo) return userinfo.Value;
            }
        }
    }

    return default;
}
async Task StartHttpListenerAsync()
{
    var listener = new HttpListener();
    listener.Prefixes.Add(@$"http://127.0.0.1:{Settings.ListenPort}/");
    listener.Start();
    Logger.Log(@$"Listener started @ {string.Join(", ", listener.Prefixes)}");

    while (true)
    {
        var context = await listener.GetContextAsync().ConfigureAwait(false);
        var request = context.Request;
        using var response = context.Response;
        using var writer = new StreamWriter(response.OutputStream);

        if (request.Url is null) continue;

        var segments = request.Url.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0) continue;

        response.StatusCode = (int) await execute().ConfigureAwait(false);



        async ValueTask<HttpStatusCode> execute()
        {
            const HttpStatusCode ok = HttpStatusCode.OK;

            var subpath = segments[0];
            if (subpath == "ping") return ok;

            await Task.Yield(); // TODO: remove


            return HttpStatusCode.NotFound;
        }
    }
}
