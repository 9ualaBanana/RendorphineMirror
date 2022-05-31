global using Common;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Hardware;


var api = new Api();
//var uinfo = await Authenticate(CancellationToken.None).ConfigureAwait(false);

if (!Debugger.IsAttached)
{
    SystemService.Start();
    _ = SendHardwareInfo();
}

_ = StartHttpListenerAsync();
Thread.Sleep(-1);


async Task SendHardwareInfo()
{
    await new HttpClient().PostAsync(
        $"https://t.microstock.plus:8443/hardware_info",
        JsonContent.Create(await HardwareInfo.GetForAll())
    );
}


async Task<UserInfo> Authenticate(CancellationToken token)
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

    var txt = @$"You are not authenticated. Please use NodeUI app to authenticate or create auth.txt file in {Directory.GetCurrentDirectory()} with your login and password divided by space";
    Console.WriteLine(txt);
    Console.WriteLine(@$"Example: ""makov@gmail.com password123""");
    Console.ReadLine();

    Environment.Exit(0);
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