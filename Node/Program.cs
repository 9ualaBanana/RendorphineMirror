global using Common;
using System.Net;
using Pinger;


SystemService.Initialize(Environment.ProcessPath!);
SystemService.Start();

_ = StartHttpListenerAsync();

Thread.Sleep(-1);




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