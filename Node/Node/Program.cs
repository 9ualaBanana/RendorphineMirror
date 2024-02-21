global using System.Collections.Immutable;
global using System.Runtime.Versioning;
global using Autofac;
global using Autofac.Core;
global using Autofac.Features.Indexed;
global using Common;
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
using _3DProductsPublish.Turbosquid;
using Node;
using Node.Services.Targets;
using SevenZip;


if (Path.GetFileNameWithoutExtension(Environment.ProcessPath!) != "dotnet")
    foreach (var proc in FileList.GetAnotherInstances())
        proc.Kill(true);

var builder = Init.CreateContainer(new("renderfin"), typeof(Program).Assembly);

_ = new ProcessesingModeSwitch().StartMonitoringAsync();

using var container = builder.Build(Autofac.Builder.ContainerBuildOptions.None);
var notifier = container.Resolve<Notifier>();
notifier.Notify("Starting node");
initializeDotTracer(container);

if (OperatingSystem.IsWindows())
    SevenZipBase.SetLibraryPath(Path.GetFullPath("assets/7z.dll"));

IServiceTarget main = (container.Resolve<Init>().IsDebug, args.Contains("release")) switch
{
    (true, false) => container.Resolve<DebugMainTarget>(),
    (true, true) => container.Resolve<ReleaseMainTarget>(),
    (false, _) => container.Resolve<PublishMainTarget>(),
};

var logger = container.Resolve<ILogger<Program>>();
logger.Info("PRODUCTS: " + string.Join(", ", container.Resolve<IRFProductStorage>().RFProducts.Values.Select(r => r.Type + ": " + r.GetType().Name)));
if (false)
{
    var RFProductFactory = container.Resolve<RFProduct.Factory>();
    var Input = new
    {
        RFProductsDirectory = "C:/occ/outproc"
    };
    var token = default(CancellationToken);

    var Logger = container.Resolve<ILogger<Program>>();
    while (true)
    {
        await Task.Delay(1000);

        try
        {
            var rfps = container.Resolve<IRFProductStorage>();

            Logger.Info("rf");
            foreach (var productDir in Directory.GetDirectories("C:/occ/inproc"))
            {
                Logger.Info($"Producting {productDir}");

                if (File.Exists(Path.Combine(productDir, ".rfproducted")))
                    Logger.Info("No, already exists");

                if (File.Exists(Path.Combine(productDir, ".rfproducted"))) continue;

                var rfp = await RFProductFactory.CreateAsync(productDir, Directories.DirCreated(Input.RFProductsDirectory, Path.GetFileNameWithoutExtension(productDir)), default, false);
                Logger.Info($"Auto-created rfproduct {rfp.ID} @ {rfp.Idea.Path}");
                File.Create(Path.Combine(productDir, ".rfproducted")).Dispose();
            }
            Logger.Info("rfe");

            //

            Logger.Info("brfes t");
            var turbo = container.Resolve<TurboSquidContainer>();
            Logger.Info("AREST");
            var ui = container.Resolve<INodeGui>();
            Logger.Info("re");

            Logger.Info($"Products: {string.Join(", ", container.Resolve<IRFProductStorage>().RFProducts.Values.Select(r => $"{r.Type} ${r.Path}"))}");

            foreach (var rfproduct in container.Resolve<IRFProductStorage>().RFProducts.Values.Where(r => r.Type == nameof(RFProduct._3D) && r.Path.StartsWith(Path.GetFullPath(Input.RFProductsDirectory))))
            {
                Logger.Info($"Publishing {rfproduct}");
                if (File.Exists(Path.Combine(rfproduct, "turbosquid.meta")))
                {
                    Logger.Info(File.ReadLines(Path.Combine(rfproduct, "turbosquid.meta")).First());
                    if (File.ReadLines(Path.Combine(rfproduct, "turbosquid.meta")).First().Contains(@"\[\d+\]"))
                    {
                        Logger.Info("No, not really.");
                        continue;
                    }
                }

                Logger.Info("pubpusfbiop");
                //var p = _3DProduct.FromDirectory(rfproduct.Path);
                //TurboSquid3DProduct.FromDirectory(rfproduct.Idea.Path).With_();
                // await (await turbo.GetAsync(default)).PublishAsync(rfproduct, ui, token);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
}

notifier.Notify("Started node");
Thread.Sleep(-1);
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
