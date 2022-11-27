global using System.Collections.Immutable;
global using Common.Plugins;
global using Common.Tasks;
global using Common.Tasks.Model;
global using Common.Tasks.Watching;
global using NLog;
using System.Diagnostics;

namespace Common
{
    public static class Initializer
    {
        public static string ConfigDirectory = "renderfin";
    }
    public static class Init
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        public static readonly bool IsDebug = false;
        static readonly bool DebugFileExists = false;
        public static readonly string ConfigDirectory = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), Initializer.ConfigDirectory));
        public static readonly string LogDirectory = Path.GetFullPath(Path.Combine(typeof(Init).Assembly.Location, "..", "logs"));
        public static readonly string TaskFilesDirectory = Path.Combine(ConfigDirectory, "tasks");
        public static readonly string PlacedTaskFilesDirectory = Path.Combine(ConfigDirectory, "ptasks");
        public static readonly string WatchingTaskFilesDirectory = Path.Combine(ConfigDirectory, "watchingtasks");
        static readonly string RuntimeCacheFilesDirectory = Path.Combine(ConfigDirectory, "cache");
        public static readonly string Version = GetVersion();

        public static void Initialize() { }
        static Init()
        {
            static void MoveOldConfigDir()
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), Initializer.ConfigDirectory.Replace("fin", "phine"));
                if (!Directory.Exists(path))
                {
                    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), Initializer.ConfigDirectory.Replace("fin", "phin"));
                    if (!Directory.Exists(path)) return;
                }

                if (!Directory.Exists(ConfigDirectory))
                    Directory.Move(path, ConfigDirectory);
            }
            MoveOldConfigDir();


            try
            {
                DebugFileExists =
                    File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Environment.ProcessPath!))!, "_debugupd"))
                    || File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Environment.ProcessPath!))!, "../_debugupd"));
            }
            catch { }
            IsDebug = Debugger.IsAttached || DebugFileExists;

            #if DEBUG
            IsDebug = true;
            #endif

            Directory.CreateDirectory(ConfigDirectory);
            if (Directory.Exists(RuntimeCacheFilesDirectory))
                Directory.Delete(RuntimeCacheFilesDirectory, true);

            Logging.Configure(IsDebug);

            _logger.Info($"Starting {Environment.ProcessId} {Process.GetCurrentProcess().ProcessName}, {Path.GetFileName(Environment.ProcessPath)} {Version}"
                + $" on {GetOSInfo()} with UTC+{TimeZoneInfo.Local.BaseUtcOffset}, {(IsDebug ? "debug" : "non-debug")}");

            try
            {
                const long gig = 1024 * 1024 * 1024;
                var drive = DriveInfo.GetDrives().First(x => Init.ConfigDirectory.StartsWith(x.RootDirectory.FullName));
                _logger.Info(@$"
                    Config drive: {drive.Name}, Space: {drive.TotalFreeSpace / gig}G totalfree & {drive.AvailableFreeSpace / gig}G availfree out of {drive.TotalSize / gig}G total
                ".TrimLines());
            }
            catch { }

            AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException(e.ExceptionObject as Exception, "UnhandledException", "unhexp");
            TaskScheduler.UnobservedTaskException += (obj, e) => LogException(e.Exception, "UnobservedTaskException", "untexp");


            static void LogException(Exception? ex, string type, string filename)
            {
                try { _logger.Error("[{Type}]: {Exception}", type, ex?.ToString() ?? "null unhandled exception"); }
                catch { }

                try { File.WriteAllText(Path.Combine(ConfigDirectory, "unhexp"), ex?.ToString()); }
                catch
                {
                    try { File.WriteAllText(Path.GetTempPath(), ex?.ToString()); }
                    catch { }
                }
            }
        }

        static string GetOSInfo()
        {
            var version = Environment.OSVersion;
            var str = version.ToString();

            if (version.Platform == PlatformID.Unix)
            {
                try
                {
                    var info = Process.Start(new ProcessStartInfo("/usr/bin/uname", "-a") { RedirectStandardOutput = true })!;
                    info.WaitForExit();

                    str += " " + info.StandardOutput.ReadToEnd().TrimEnd(Environment.NewLine.ToCharArray());
                }
                catch { }
            }

            return str;
        }
        static string GetVersion()
        {
            try
            {
                if (IsDebug) return "debug";

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

        public static string RuntimeCacheDirectory(string? subdir = null)
        {
            var dir = RuntimeCacheFilesDirectory;
            if (subdir is not null)
                dir = Path.Combine(dir, subdir);

            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}