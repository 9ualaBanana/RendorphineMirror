global using Common;
using Hardware;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;


if (!Debugger.IsAttached)
    SystemService.Start();

_ = SendHardwareInfo();
_ = StartHttpListenerAsync();

Thread.Sleep(-1);


async Task SendHardwareInfo()
{
    await new HttpClient().PostAsync(
        $"{Environment.GetEnvironmentVariable("SERVER_HOST", EnvironmentVariableTarget.Machine)}/hardware_info",
        JsonContent.Create(await HardwareInfo.GetForAll()));
}

async Task StartHttpListenerAsync()
{
    var listener = new HttpListener();
    listener.Prefixes.Add(@$"http://127.0.0.1:{Settings.ListenPort}/");
    listener.Start();
    Console.WriteLine(@$"Listener started @ {string.Join(", ", listener.Prefixes)}");

    while (true)
    {
        var context = await listener.GetContextAsync().ConfigureAwait(false);
        var request = context.Request;
        using var response = context.Response;
        using var writer = new StreamWriter(response.OutputStream);

        if (request.Url is null) continue;

        if (request.Url.AbsoluteUri.EndsWith("/ping"))
            response.StatusCode = (int) HttpStatusCode.OK;
    }
}