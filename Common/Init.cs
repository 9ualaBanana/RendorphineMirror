global using NLog;
using System.Diagnostics;

namespace Common;

public static class Initializer
{
    // should the node to be launched with admin rights?
    // always true, keeping just in case we'd want to change this
    public static readonly bool UseAdminRights = true;


    public static string ConfigDirectory = "renderfin";
}
public static class Init
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static readonly bool IsDebug = false;
    public static readonly bool DebugFeatures = false;
    public static readonly string Version, ConfigDirectory, LogDirectory;

    public static void Initialize() { }
    static Init()
    {
        ConfigDirectory = NewDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), Initializer.ConfigDirectory);
        LogDirectory = NewDirectory(typeof(Init).Assembly.Location, "..", "logs");
        Version = GetVersion();
        static string GetVersion()
        {
            if (IsDebug) return "debug";

            try
            {
                var assembly = typeof(Init).Assembly.Location ?? Environment.ProcessPath!;
                if (FileVersionInfo.GetVersionInfo(assembly).ProductVersion is { } ver && ver != "1.0.0")
                    return ver;

                var time = Directory.GetFiles(Path.GetDirectoryName(assembly)!, "*", SearchOption.AllDirectories).Select(File.GetLastWriteTimeUtc).Max();
                return time.ToString(@"y\.M\.d\-\Uhhmm");
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting version: {Exception}", ex);
                return "UNKNOWN";
            }
        }


        ConfigureDebug(out IsDebug, out DebugFeatures);
        void ConfigureDebug(out bool isDebug, out bool debugFeatures)
        {
            isDebug = Debugger.IsAttached;
#if DEBUG || DBG
            isDebug = true;
#endif

            debugFeatures = IsDebug;
            try
            {
                debugFeatures |=
                    File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Environment.ProcessPath!))!, "_debugupd"))
                    || File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Environment.ProcessPath!))!, "../_debugupd"));
            }
            catch { }
        }

        Directory.CreateDirectory(ConfigDirectory);
        if (Directory.Exists(RuntimeCacheDirectory()))
            Directory.Delete(RuntimeCacheDirectory(), true);


        ConfigureLogging();
        void ConfigureLogging()
        {
            Logging.Configure(DebugFeatures);
            LogInit();
            static void LogInit()
            {
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


                _logger.Info($"Starting {Environment.ProcessId} {Environment.ProcessPath}, {Process.GetCurrentProcess().ProcessName} v {Version} on \"{osinfo}\" at {DateTimeOffset.Now}");
                if (Environment.GetCommandLineArgs() is { Length: > 1 } args)
                    _logger.Info($"Arguments: {string.Join(' ', args.Select(x => $"\"{x}\""))}");


                try
                {
                    const long gig = 1024 * 1024 * 1024;
                    var drive = DriveInfo.GetDrives().First(x => Init.ConfigDirectory.StartsWith(x.RootDirectory.FullName));
                    _logger.Info(@$"Config dir: {Init.ConfigDirectory} on drive {drive.Name} (totalfree {drive.TotalFreeSpace / gig}G; availfree {drive.AvailableFreeSpace / gig}G; total {drive.TotalSize / gig}G)");
                }
                catch { }
            }

        }


        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException(e.ExceptionObject as Exception, "UnhandledException");
        TaskScheduler.UnobservedTaskException += (obj, e) => LogException(e.Exception, "UnobservedTaskException");
        static void LogException(Exception? ex, string type)
        {
            try { _logger.Error("[{Type}]: {Exception}", type, ex?.ToString() ?? "null unhandled exception"); }
            catch { }
        }
    }


    static string NewDirectory(params string?[] dirs)
    {
        var dir = Path.GetFullPath(Path.Combine(dirs.Where(x => x is not null).ToArray()!));
        Directory.CreateDirectory(dir);

        return dir;
    }

    public static string RuntimeCacheDirectory(string? subdir = null) => NewDirectory(ConfigDirectory, "cache", subdir);
    public static string TempDirectory(string? subdir = null) => NewDirectory(ConfigDirectory, "temp", subdir);
}