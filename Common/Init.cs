using System.Diagnostics;

namespace Common
{
    public static class Init
    {
        public static void InitLogger()
        {
            Logger.InitializeExceptionLogging();
            Logger.Log(ConsoleColor.White, "Renderphine " + GetVersion() + " on " + GetOSInfo() + " w UTC+" + TimeZoneInfo.Local.BaseUtcOffset, writeToConsole: false);

            try { Logger.Log($"Current process name: { Process.GetCurrentProcess().ProcessName }", writeToConsole: false); }
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

        public static string GetVersion()
        {
            DateTime? time = null;
            try { time = File.GetCreationTimeUtc(Environment.ProcessPath!); }
            catch { }

            return time?.ToString("ddMMyy") ?? "UNKNOWNVERSION";
        }
    }
}