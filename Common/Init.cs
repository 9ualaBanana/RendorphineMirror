global using System.Collections.Immutable;
global using Serilog;
using System.Diagnostics;
using Serilog.Events;

namespace Common
{
    public static class Init
    {
        public static readonly bool IsDebug = false;
        static readonly bool DebugFileExists = false;
        public static readonly string ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "renderphine");
        public static readonly string LogDirectory = Path.Combine(ConfigDirectory, "logs");
        public static readonly string TaskFilesDirectory = Path.Combine(ConfigDirectory, "tasks");
        public static readonly string Version = GetVersion();

        // empty method to trigger static ctor
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

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(restrictedToMinimumLevel: IsDebug ? LogEventLevel.Verbose : LogEventLevel.Information)
                .WriteTo.File(
                    Path.Combine(LogDirectory, "log" + Path.GetFileNameWithoutExtension(Environment.ProcessPath!)) + ".log",
                    restrictedToMinimumLevel: IsDebug ? LogEventLevel.Verbose : LogEventLevel.Information,
                    retainedFileTimeLimit: TimeSpan.FromDays(7),
                    shared: true
                )
                .MinimumLevel.Is(IsDebug ? LogEventLevel.Verbose : LogEventLevel.Debug)
                .CreateLogger();

            Log.Information($"{Path.GetFileName(Environment.ProcessPath)} {Version} on {GetOSInfo()} w UTC+{TimeZoneInfo.Local.BaseUtcOffset}");
            Log.Information($"Debug: {IsDebug}");
            Log.Verbose($"-DEBUG VERSION-");

            try { Log.Debug($"Current process: {Environment.ProcessId} {Process.GetCurrentProcess().ProcessName}"); }
            catch { }

            if (!IsDebug)
            {
                var fce = false;
                AppDomain.CurrentDomain.FirstChanceException += (obj, e) =>
                {
                    try
                    {
                        if (fce) return;

                        fce = true;
                        LogException(e.Exception, "FirstChanceException", "fcexp");
                    }
                    finally { fce = false; }
                };
            }

            AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException(e.ExceptionObject as Exception, "UnhandledException", "unhexp");
            TaskScheduler.UnobservedTaskException += (obj, e) => LogException(e.Exception, "UnobservedTaskException", "untexp");


            static void LogException(Exception? ex, string type, string filename)
            {
                try { Log.Error($"[{type}]: {ex?.ToString() ?? "null unhandled exception"}"); }
                catch { }

                try { File.WriteAllText(Path.Combine(Init.ConfigDirectory, "unhexp"), ex?.ToString()); }
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
                var assembly = typeof(Init).Assembly.Location ?? Environment.ProcessPath!;
                if (FileVersionInfo.GetVersionInfo(assembly).ProductVersion is { } ver && ver != "1.0.0")
                    return ver;

                var time = Directory.GetFiles(Path.GetDirectoryName(assembly)!, "*", SearchOption.AllDirectories).Select(File.GetLastWriteTimeUtc).Max();
                return time.ToString(@"y\.M\.d\-\Uhhmm");
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting version: {ex}");
                return "UNKNOWN";
            }
        }
    }
}