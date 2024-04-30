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
using System.Text;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Node;
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

builder.Services.AddControllers();

builder.WebHost.UseKestrel((ctx, o) =>
    o.ListenLocalhost(5336, o =>
    {
        // o.Protocols = HttpProtocols.Http2;
    })
    
);

await using var app = builder.Build();

static string FindFirstExistingDirectory(params string[] directories) => directories.First(Directory.Exists);

app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(
        FindFirstExistingDirectory(
            Path.Combine(builder.Environment.ContentRootPath, "dist"),
            Path.GetFullPath("dist"),
            Path.GetFullPath("marktm.client/dist")
        )
    )
});
app.UseDefaultFiles();
app.MapControllers();
app.UseWebSockets();

app.MapGet("/marktm", (IRFProductStorage products) =>
{
    var sb = new StringBuilder("""
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8">
        <meta http-equiv="X-UA-Compatible" content="IE=edge">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Your Page Title</title>
    </head>
    <body>
    """);
    sb.AppendJoin('\n', products.RFProducts.Where(_ => File.Exists(_.Value.QSPreview.First().Path)).Select(_ => $"<img src=http://localhost:5336/rfpreview/{_.Key} />"));
    sb.Append(
    """    
    </body>
    </html>
    """);
    return Results.Content(sb.ToString(), "text/html");
});
app.MapGet("/rfpreview/{id}", (IRFProductStorage products, string id)
    => new FileInfo(products.RFProducts[id].QSPreview.First().Path) is var file && file.Exists
        ? Results.File(file.FullName, $"image/png")
        : Results.NotFound());

await app.StartAsync();

var container = app.Services.GetRequiredService<IComponentContext>();
_ = new ProcessesingModeSwitch().StartMonitoringAsync();

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
