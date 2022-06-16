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
        public static readonly string Version = GetVersion();

        static Init()
        {
            try { DebugFileExists = File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Environment.ProcessPath!))!, "_debugupd")); }
            catch { }
            IsDebug = Debugger.IsAttached || DebugFileExists;

            Directory.CreateDirectory(ConfigDirectory);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(restrictedToMinimumLevel: IsDebug ? LogEventLevel.Debug : LogEventLevel.Information)
                .WriteTo.File(Path.Combine(ConfigDirectory, "logs", "log" + Path.GetFileNameWithoutExtension(Environment.ProcessPath!)) + ".log", restrictedToMinimumLevel: LogEventLevel.Information, retainedFileTimeLimit: TimeSpan.FromDays(7))
                .MinimumLevel.Debug()
                .CreateLogger();
        }

        public static void InitLogger()
        {
            Log.Information($"Renderphine {Version} on {GetOSInfo()} w UTC+{TimeZoneInfo.Local.BaseUtcOffset}");

            try { Log.Debug($"Current process: PID {Environment.ProcessId} {Process.GetCurrentProcess().ProcessName}"); }
            catch { }
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
            DateTime? time = null;
            try { time = Directory.GetFiles(Path.GetDirectoryName(typeof(Init).Assembly.Location ?? Environment.ProcessPath!)!, "*", SearchOption.AllDirectories).Select(File.GetLastWriteTimeUtc).Max(); }
            catch (Exception ex) { Console.WriteLine("err getting version " + ex.Message); }

            return time?.ToString("ddMMyy_HHmm") ?? "UNKNOWNVERSION";
        }
    }
}