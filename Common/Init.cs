global using System.Collections.Immutable;
global using Common.NodeToUI;
global using Common.Tasks;
global using NLog;
using System.Diagnostics;

namespace Common
{
    public static class Init
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        public static readonly bool IsDebug = false;
        static readonly bool DebugFileExists = false;
        public static readonly string ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "renderphine");
        public static readonly string LogDirectory = Path.Combine(ConfigDirectory, "logs");
        public static readonly string TaskFilesDirectory = Path.Combine(ConfigDirectory, "tasks");
        public static readonly string Version = GetVersion();

        public static void Initialize() { }
        static Init()
        {
            try
            {
                DebugFileExists =
                    File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Environment.ProcessPath!))!, "_debugupd"))
                    || File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Environment.ProcessPath!))!, "../_debugupd"));
            }
            catch { }
            IsDebug = Debugger.IsAttached || DebugFileExists;

            Directory.CreateDirectory(ConfigDirectory);

            Logging.Configure(IsDebug);

            _logger.Info("{ProcessPath} {Version} on {Os} with UTC+{UtcOffset}",
                Path.GetFileName(Environment.ProcessPath), Version, GetOSInfo(), TimeZoneInfo.Local.BaseUtcOffset);
            _logger.Info("Debug: {IsDebug}", IsDebug);
            _logger.Trace($"-DEBUG VERSION-");



            try { _logger.Debug("Current process: {ProcessId} {ProcessName}", Environment.ProcessId, Process.GetCurrentProcess().ProcessName); }
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
                if (Debugger.IsAttached) return "debug";

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
    }
}