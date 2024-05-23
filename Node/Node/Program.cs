global using System.Runtime.Versioning;
global using Autofac;
global using Autofac.Core;
global using Autofac.Features.Indexed;
global using Machine;
global using MarkTM.RFProduct;
global using Microsoft.Extensions.Logging;
global using Node.Common;
global using Node.Common.Models;
global using Node.DataStorage;
global using Node.Plugins;
global using Node.Plugins.Models;
global using Node.Tasks;
global using Node.Tasks.Exec;
global using Node.Tasks.IO.Input;
global using Node.Tasks.IO.Output;
global using Node.Tasks.Models;
global using Node.Tasks.Watching.Input;
global using NodeCommon;
global using NodeCommon.ApiModel;
global using NodeCommon.Tasks;
global using NodeCommon.Tasks.Model;
global using NodeCommon.Tasks.Watching;
global using NodeToUI;
global using Logger = NLog.Logger;
global using LogLevel = NLog.LogLevel;
global using LogManager = NLog.LogManager;
using System.Net;
using System.Text;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Node;
using Node.Listeners;
using Node.Services.Targets;
using SevenZip;

if (OperatingSystem.IsWindows())
    CefInitializer.Initialize();

if (Path.GetFileNameWithoutExtension(Environment.ProcessPath!) != "dotnet")
    foreach (var proc in FileList.GetAnotherInstances())
        proc.Kill(true);



/*var logfactory = LogManager.Setup()
    .SetupLogFactory(config => config
        .SetAutoShutdown(true)
        .SetGlobalThreshold(NLog.LogLevel.Trace)
        .SetTimeSourcAccurateUtc()
    )
    .LoadConfiguration(rule => rule.ForLogger()
        .FilterMinLevel(true ? NLog.LogLevel.Trace : NLog.LogLevel.Info)
        .WriteTo(new ColoredConsoleTarget()
        {
            Layout = "${time:universalTime=true} [${level:uppercase=true} @ ${logger:shortName=true} @ ${scopenested:separator= @ }] ${message:withException=true:exceptionSeparator=\n\n}",
            AutoFlush = true,
            DetectConsoleAvailable = true,
            UseDefaultRowHighlightingRules = true,
        })
    ).LogFactory;*/

var builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(builder =>
{
    Init.InitializeContainer(builder, new("renderfin"), [typeof(Program).Assembly]);
}));

builder.Services.AddControllers().AddNewtonsoftJson();

const int aspPort = PortForwarding.ASPPort;
builder.WebHost.UseKestrel((ctx, o) =>
{
    o.ListenLocalhost(aspPort);
});

await using var app = builder.Build();
app.MapControllers();
app.UseWebSockets();

app.MapGet("/", (SessionManager manager) => Results.Content($@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Node Dashboard</title>
    <style>
        body {{
            margin: 0;
            font-family: Arial, sans-serif;
            background-size: cover;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            height: 100vh;
            color: white;
        }}

        .header {{
            position: absolutet;
            top: 0;
            width: 100%;
            text-align: center;
            padding: 20px;
            background: rgba(0, 0, 0, 0.5);
            font-size: 18px;
            z-index: 100;
        }}

        .button-container {{
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
            animation: fadeIn 1s;
        }}

        .button {{
            background: rgba(0, 0, 0, 0.8);
            border: none;
            border-radius: 10px;
            padding: 15px 30px;
            margin: 10px;
            color: white;
            font-size: 18px;
            cursor: pointer;
            transition: background 0.3s;
        }}

        .button:hover {{
            background: rgba(0, 0, 139, 0.8);
        }}

        @keyframes fadeIn {{
            from {{ opacity: 0; }}

            to {{ opacity: 1; }}
        }}
    </style>
    <link rel=""stylesheet"" href=""Dashboard.css"" />
</head>
<body>
    <div class=""button-container"">
        {
        (manager.IsLoggedIn() ? "" : @"
        <form action=""/login"" method=""get"">
            <button class=""button"" type=""submit"">Login</button>
        </form>
        <form action=""/autologin"" method=""get"">
            <button class=""button"" type=""submit"">Auto Login</button>
        </form>")
        }
        <form action=""/marktm"" method=""get"">
            <button class=""button"" type=""submit"">Gallery</button>
        </form>
        <form action=""/marktm/sell"" method=""get"">
            <button class=""button"" type=""submit"">Upload</button>
        </form>
        <form action=""/marktm/sources"" method=""get"">
            <button class=""button"" type=""submit"">RFProduct Sources</button>
        </form>
        <form action=""/reset_rating"" method=""get"">
            <button class=""button"" type=""submit"">Reset Rating</button>
        </form>
        <form action=""/restart"" method=""get"">
            <button class=""button"" type=""submit"">Restart</button>
        </form>
    </div>
</body>
</html>
", "text/html"));

app.MapGet("/marktm", async (string[] sources, SettingsInstance settings, IRFProductStorage products, HttpContext context) =>
{
    var host = context.Request.Host.Host;
    if (sources.Length is not 0)
    {
        settings.RFProductSourceDirectories.Value = [.. sources];
        return Results.Created();
    }
    else
    {
        var sb = new StringBuilder("""
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8">
        <meta http-equiv="X-UA-Compatible" content="IE=edge">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Gallery</title>
    </head>
    <body>
    """);
        sb.AppendJoin('\n', products.RFProducts.Where(_ => File.Exists(_.Value.QSPreview.First().Path)).Select(_ => $"<img src={(host == "localhost" ? $"http://{host}:{aspPort}" : $"https://{host}")}/rfpreview/{_.Key} class=rfpreview/>"));
        sb.Append(
        """    
    </body>
    </html>
    """);
        return Results.Content(sb.ToString(), "text/html");
    }
});

app.MapGet("/marktm/sources", (SettingsInstance settings) => Results.Content($@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Sources</title>
    <style>
        .input-container {{
            display: flex;
            flex-direction: column;
        }}
        .button {{
            background: rgba(0, 0, 0, 0.8);
            border: none;
            border-radius: 10px;
            padding: 10px 20px;
            margin: 10px;
            color: white;
            font-size: 10px;
            cursor: pointer;
            transition: background 0.3s;
        }}

        .button:hover {{
            background: rgba(0, 0, 139, 0.8);
        }}

        @keyframes fadeIn {{
            from {{ opacity: 0; }}

            to {{ opacity: 1; }}
        }}
    </style>
    <script>
        function addInput() {{
            const form = document.getElementById('form');
            const inputContainer = document.createElement('div');
            inputContainer.className = 'input-container';
            const input = document.createElement('input');
            input.type = 'text';
            input.name = 'sources';
            inputContainer.appendChild(input);
            form.insertBefore(inputContainer, form.lastElementChild.previousElementSibling);
        }}
    </script>
</head>
<body>
    <form id='form' action='/marktm' method='get'>
        {string.Join("\n", settings.RFProductSourceDirectories.Value.Select(directory => $"<div class='input-container'><input type='text' name='sources' value='{directory}' /></div>"))}
        <button type='button' onclick='addInput()' class=""button"">Add Source</button>
        <button type='submit' class=""button"">Change</button>
    </form>
</body>
</html>", "text/html"));

app.MapGet("/marktm/sell", () => Results.Content(@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Sell</title>
    <style>
        .button {
            background: rgba(0, 0, 0, 0.8);
            border: none;
            border-radius: 10px;
            padding: 15px 30px;
            margin: 10px;
            color: white;
            font-size: 18px;
            cursor: pointer;
            transition: background 0.3s;
        }

        .button:hover {
            background: rgba(0, 0, 139, 0.8);
        }

        @keyframes fadeIn {
            from { opacity: 0; }

            to { opacity: 1; }
        }
    </style>
</head>
<body>
    <form action=""/marktm"" method=""post"" enctype=""multipart/form-data"">
        <input type=""file"" name=""files"" multiple>
        <button type=""submit"" class=""button"">Submit</button>
    </form>
</body>
</html>
", "text/html"));

app.MapPost("/marktm", async (IFormFileCollection files, SettingsInstance settings, IRFProductStorage products, RFProduct.Factory factory, CancellationToken cancellationToken) =>
{
    // Will get auto-generated.
    var Ccontainer = settings.RFProductSourceDirectories.Value.First();
    foreach (var file in files)
    {
        var CcontainedFile = new FileStream(Path.Combine(Ccontainer, file.FileName), FileMode.Create);
        await file.OpenReadStream().CopyToAsync(CcontainedFile, cancellationToken);
    }
    return Results.Created();
})
    .DisableAntiforgery();

app.MapGet("/rfpreview/{id}", (IRFProductStorage products, string id)
    => new FileInfo(products.RFProducts[id].QSPreview.First().Path) is var file && file.Exists
        ? Results.File(file.FullName, $"image/png")
        : Results.NotFound());

app.MapGet("/reset_rating", (SettingsInstance settings) => { settings.BenchmarkResult.Value = null; Results.Ok(); });
app.MapGet("/restart", () => Environment.Exit(0));

app.MapGet("/login", () => Results.Content(@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Login</title>
</head>
<body>
    <h1>Login</h1>
    <form action=""/login"" method=""post"">
        <label for=""login"">Username:</label>
        <input type=""text"" id=""login"" name=""login""><br><br>
        <label for=""password"">Password:</label>
        <input type=""password"" id=""password"" name=""password""><br><br>
        <input type=""submit"" value=""Login"">
    </form>
</body>
</html>
", "text/html"));

app.MapGet("/autologin", () => Results.Content(@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Auto Login</title>
</head>
<body>
    <h1>Auto Login</h1>
    <form action=""/autologin"" method=""post"">
        <label for=""login"">Username:</label>
        <input type=""text"" id=""login"" name=""login""><br><br>
        <input type=""submit"" value=""Auto Login"">
    </form>
</body>
</html>
", "text/html"));
app.MapPost("/login", async ([FromForm] string login, [FromForm] string password, [FromServices] SessionManager sessionManager) =>
{
    if (sessionManager.IsLoggedIn()) return JsonApi.Error("Already authenticated.").ToString();
    return JsonApi.JsonFromOpResult(await sessionManager.AuthAsync(login, password)).ToString();
})
    .DisableAntiforgery();
app.MapPost("/autologin", async ([FromForm] string login, [FromServices] SessionManager sessionManager) =>
{
    if (sessionManager.IsLoggedIn()) return JsonApi.Error("Already authenticated.").ToString();
    return JsonApi.JsonFromOpResult(await sessionManager.AutoAuthAsync(login)).ToString();
})
    .DisableAntiforgery();

await app.StartAsync();

var container = app.Services.GetRequiredService<IComponentContext>();
_ = new ProcessesingModeSwitch().StartMonitoringAsync();

var pl = container.Resolve<ProxyListener>();

var notifier = container.Resolve<Notifier>();
notifier.Notify("Starting node");
// initializeDotTracer(container);

if (OperatingSystem.IsWindows())
    SevenZipBase.SetLibraryPath(Path.GetFullPath("assets/7z.dll"));

IServiceTarget main = (container.Resolve<Init>().IsDebug, args.Contains("release")) switch
{
    (true, false) => container.Resolve<DebugMainTarget>(),
    (true, true) => container.Resolve<ReleaseMainTarget>(),
    (false, _) => container.Resolve<PublishMainTarget>(),
};

notifier.Notify("Started node");
await app.WaitForShutdownAsync();
GC.KeepAlive(main);



[Conditional("DEBUG")]
static void initializeDotTracer(IContainer container)
{
    Directories.NewDirCreated("temp/dot");

    var tracer = new Autofac.Diagnostics.DotGraph.DotDiagnosticTracer();
    tracer.OperationCompleted += (sender, args) =>
    {
        try
        {
            if (args.Operation.InitiatingRequest?.Service is not IServiceWithType service) return;

            using var file = File.OpenWrite($"temp/dot/{service.ServiceType.Name}.dot");
            using var writer = new StreamWriter(file);

            // removing ILogger entries
            var content = args.TraceContent
                .Split('\n')
                .Select(s => s.ContainsOrdinal("label=<ILogger`1>") ? string.Empty : s)
                .Select(s => s.ContainsOrdinal("label=<ILoggerProvider>") ? string.Empty : s)
                .Select(s => s.ContainsOrdinal("Microsoft.Extensions.Logging.ILogger&lt") ? string.Empty : s);

            writer.WriteLine(string.Join('\n', content));
        }
        catch { }
    };
    container.SubscribeToDiagnostics(tracer);
}


/* not working
[AutoRegisteredService(true)]
class TcpRedirector
{
    readonly TcpListener Listener;
    readonly string TargetAddress;
    readonly int TargetPort;

    public TcpRedirector(INodeSettings settings, ILogger<TcpRedirector> logger)
    {
        Listener = new TcpListener(IPAddress.Any, settings.UPnpPort);
        logger.Info("Listening redirector on " + settings.UPnpPort);

        TargetAddress = "127.0.0.1";
        TargetPort = PortForwarding.ASPPort;
    }

    public void Start() => new Thread(_Start) { IsBackground = true }.Start();
    void _Start()
    {
        Listener.Start();

        while (true)
        {
            var client = Listener.AcceptTcpClient();
            Task.Run(() => Redirect(client)).Consume();
        }
    }

    async Task Redirect(TcpClient client)
    {
        using var _ = client;
        using var clientStream = client.GetStream();

        using var targetClient = new TcpClient(TargetAddress, TargetPort);
        using var targetStream = targetClient.GetStream();

        var t1 = Task.Run(async () =>
        {
            var buffer = new byte[1024];
            int read;

            while (client.Connected && ((read = await clientStream.ReadAsync(buffer)) != 0))
                await targetStream.WriteAsync(buffer.AsMemory(0, read));
        });

        var t2 = Task.Run(async () =>
        {
            var buffer = new byte[1024];
            int read;

            while (client.Connected && ((read = await targetStream.ReadAsync(buffer)) != 0))
                await clientStream.WriteAsync(buffer.AsMemory(0, read));
        });

        await Task.WhenAll(t1, t2);
    }
}


*/

// temporary while ASP isn't the front facing listener
[AutoRegisteredService(true)]
class ProxyListener : ListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.WebServer;

    public required HttpClient Client { get; init; }

    public ProxyListener(ILogger<ProxyListener> logger) : base(logger) { }

    protected override async ValueTask Execute(HttpListenerContext context)
    {
        var stream = await readStream(context);

        var exec = await execute(context);
        context.Response.StatusCode = (int) exec;
        context.Response.Close();


        async Task<HttpStatusCode> execute(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            using var message = new HttpRequestMessage(HttpMethod.Parse(context.Request.HttpMethod), "http://127.0.0.1:" + PortForwarding.ASPPort + request.RawUrl);
            message.Content = new StreamContent(stream);
            message.Content.Headers.TryAddWithoutValidation("Content-Type", request.ContentType);

            foreach (var header in request.Headers.AllKeys)
                if (header is not (null or "Content-Length" or "Content-Type"))
                    message.Headers.Add(header, request.Headers[header]);

            var call = await Client.SendAsync(message);
            await call.Content.CopyToAsync(response.OutputStream);
            return call.StatusCode;
        }
        async Task<Stream> readStream(HttpListenerContext context)
        {
            var data = new byte[context.Request.ContentLength64];
            var span = data.AsMemory();

            while (true)
            {
                var read = await context.Request.InputStream.ReadAsync(span);
                if (read == 0) break;

                span = span.Slice(read);
            }

            try
            {
                if (context.Request.ContentType is ("plain/text" or "application/json"))
                {
                    var poststr = await new StreamReader(new MemoryStream(data)).ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(poststr))
                        Logger.Trace($"{context.Request.RemoteEndPoint} {context.Request.ContentType} POST data:\n{poststr}");
                }
            }
            catch { }

            return new MemoryStream(data);
        }
    }
}
