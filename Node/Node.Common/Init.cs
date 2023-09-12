using System.Reflection;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;

namespace Node.Common;

public class Init : IServiceTarget
{
    //public static Init For(InitConfig config) => new Init() { Configuration = config, Dirs = new DataDirs(config.AppName), Logger = new NLogLoggerFactory().CreateLogger<Init>(), };
    public static Init For(InitConfig config)
    {
        var builder = CreateContainer(config);
        var container = builder.Build();

        return container.Resolve<Init>();
    }

    public static ContainerBuilder CreateContainer(InitConfig config, params Assembly[] targetAssemblies)
    {
        var builder = new ContainerBuilder();

        builder.RegisterInstance(config);
        builder.RegisterType<Init>()
            .AutoActivate();

        foreach (var assembly in targetAssemblies)
            RegisterTargets(builder, assembly);

        Init.CreateRegistrations(builder);
        return builder;
    }

    public static void RegisterTargets(ContainerBuilder builder, Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsAssignableTo(typeof(IServiceTarget)))
            .ToArray();

        RegisterTargets(builder, types);
    }
    public static void RegisterTarget<T>(ContainerBuilder builder) where T : IServiceTarget => RegisterTargets(builder, typeof(T));
    public static void RegisterTargets(ContainerBuilder builder, params Type[] types)
    {
        if (types.FirstOrDefault(type => !type.IsAssignableTo(typeof(IServiceTarget))) is { } invalidtype)
            throw new ArgumentException($"Invalid target type {invalidtype}", nameof(types));

        builder.RegisterTypes(types)
            .SingleInstance()
            .OnActivating(async l =>
            {
                var logger = l.Context.ResolveLogger(l.Instance.GetType());

                logger.LogInformation($"Resolved target {l.Instance}");
                await ((IServiceTarget) l.Instance).ExecuteAsync();
                logger.LogInformation($"Reached target {l.Instance}");
            });

        foreach (var type in types)
            type.GetMethod(nameof(IServiceTarget.CreateRegistrations))?.Invoke(null, new object[] { builder });
    }

    public static void CreateRegistrations(ContainerBuilder builder)
    {
        // logging
        builder.Populate(new ServiceCollection().With(services => services.AddLogging(l => l.AddNLog())));

        builder.RegisterType<DataDirs>()
            .SingleInstance();

        builder.RegisterSource<AutoServiceRegistrator>();
    }

    public required InitConfig Configuration { get; init; }
    public required DataDirs Dirs { get; init; }
    public required ILogger<Init> Logger { get; init; }

#pragma warning disable CS8618 // Properties are not set
    public string Version { get; private set; }
    public bool IsDebug { get; private set; }
    public bool DebugFeatures { get; private set; }
#pragma warning restore CS8618

    public async Task ExecuteAsync()
    {
        Version = GetVersion();
        ConfigureDebug();
        ConfigureLogging();
        ConfigureUnhandledExceptions();
    }

    string GetVersion()
    {
        if (IsDebug) return "debug";

        try
        {
            var assembly = (Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).Location ?? Environment.ProcessPath!;
            if (FileVersionInfo.GetVersionInfo(assembly).ProductVersion is { } ver && ver != "1.0.0")
                return ver;

            var time = Directory.GetFiles(Path.GetDirectoryName(assembly)!, "*", SearchOption.AllDirectories).Select(File.GetLastWriteTimeUtc).Max();
            return time.ToString(@"y\.M\.d\-\Uhhmm", CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error getting version: {Exception}", ex);
            return "UNKNOWN";
        }
    }
    void ConfigureDebug()
    {
        IsDebug = Debugger.IsAttached;
#if DEBUG || DBG
        IsDebug = true;
#endif

        DebugFeatures = IsDebug;
        try
        {
            DebugFeatures |=
                File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Environment.ProcessPath!))!, "_debugupd"))
                || File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Environment.ProcessPath!))!, "../_debugupd"));
        }
        catch { }
    }
    void ConfigureLogging()
    {
        Logging.Configure(DebugFeatures, Configuration.LogToFile);


        var version = Environment.OSVersion;
        var osinfo = version.ToString();

        if (version.Platform == PlatformID.Unix)
        {
            try
            {
                using var info = Process.Start(new ProcessStartInfo("/usr/bin/uname", "-a") { RedirectStandardOutput = true })!;
                info.WaitForExit();

                osinfo += " " + info.StandardOutput.ReadToEnd().TrimEnd(Environment.NewLine.ToCharArray());
            }
            catch { }
        }


        Logger.LogInformation($"Starting {Environment.ProcessId} {Environment.ProcessPath}, {Process.GetCurrentProcess().ProcessName} v {Version} on \"{osinfo}\" at {DateTimeOffset.Now}");
        if (Environment.GetCommandLineArgs() is { Length: > 1 } args)
            Logger.LogInformation($"Arguments: {string.Join(' ', args.Select(x => $"\"{x}\""))}");


        try
        {
            const long gig = 1024 * 1024 * 1024;
            var drive = DriveInfo.GetDrives().First(x => Dirs.Data.StartsWith(x.RootDirectory.FullName, StringComparison.OrdinalIgnoreCase));
            Logger.LogInformation(@$"Config dir: {Dirs.Data} on drive {drive.Name} (totalfree {drive.TotalFreeSpace / gig}G; availfree {drive.AvailableFreeSpace / gig}G; total {drive.TotalSize / gig}G)");
        }
        catch { }
    }
    void ConfigureUnhandledExceptions()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException(e.ExceptionObject as Exception, "UnhandledException");
        TaskScheduler.UnobservedTaskException += (obj, e) => LogException(e.Exception, "UnobservedTaskException");
        void LogException(Exception? ex, string type)
        {
            try { Logger.LogError($"[{type}]: {ex?.ToString() ?? "null exception"}"); }
            catch { }
        }
    }


    public record InitConfig(string AppName, bool UseAdminRights = true, bool LogToFile = true);
}
