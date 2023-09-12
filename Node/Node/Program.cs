global using System.Collections.Immutable;
global using System.Runtime.Versioning;
global using Autofac;
global using Autofac.Core;
global using Autofac.Features.Indexed;
global using Common;
global using Machine;
global using Microsoft.Extensions.Logging;
global using Node.Common;
global using Node.Common.Models;
global using Node.DataStorage;
global using Node.Plugins;
global using Node.Plugins.Models;
global using Node.Tasks;
global using Node.Tasks.Exec;
global using Node.Tasks.IO;
global using Node.Tasks.IO.Input;
global using Node.Tasks.IO.Output;
global using Node.Tasks.Models;
global using Node.Tasks.Watching;
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
using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader.Network;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using Node;
using Node.Heartbeat;
using Node.Listeners;
using Node.Profiling;
using Node.Services;
using Node.Services.Targets;
using Node.Tasks.Exec.Actions;
using Node.Tasks.Watching.Handlers.Input;
using Tomlyn.Syntax;


if (Path.GetFileNameWithoutExtension(Environment.ProcessPath!) != "dotnet")
    foreach (var proc in FileList.GetAnotherInstances())
        proc.Kill(true);

var builder = Init.CreateContainer(new("renderfin"), typeof(Program).Assembly);

_ = new ProcessesingModeSwitch().StartMonitoringAsync();


using var container = builder.Build(Autofac.Builder.ContainerBuildOptions.None);
initializeDotTracer(container);

IServiceTarget main = (container.Resolve<Init>().IsDebug, args.Contains("release")) switch
{
    (true, false) => container.Resolve<DebugMainTarget>(),
    (true, true) => container.Resolve<ReleaseMainTarget>(),
    (false, _) => container.Resolve<PublishMainTarget>(),
};

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