using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Common
{
    public static class FileList
    {
        public static void KillNodeUI()
        {
            var thisid = Environment.ProcessId;
            foreach (var proc in Process.GetProcesses())
            {
                if (proc.Id == thisid) continue;
                if (proc.ProcessName != "NodeUI") continue;

                Console.WriteLine(@$"Killing process {proc.Id} {proc.ProcessName}");
                try { proc.Kill(); proc.WaitForExit(); }
                catch { }
            }
        }
        public static void KillProcesses()
        {
            var thisid = Environment.ProcessId;
            var thisdir = Path.GetFullPath(Path.GetDirectoryName(Path.GetDirectoryName(Environment.ProcessPath!))!);

            foreach (var proc in Process.GetProcesses())
            {
                if (proc.Id == thisid) continue;
                if (!FilterProcCheck(proc, check)) continue;

                Console.WriteLine(@$"Found old process {proc.Id} {proc.ProcessName}, killing");
                try { proc.Kill(); }
                catch { }
            }

            static bool FilterProcCheck(Process proc, Func<string?, bool> check)
            {
                try { return check(proc.MainModule?.FileName); }
                catch { return false; }
            }
            bool check(string? path) => path?.StartsWith(thisdir, StringComparison.Ordinal) ?? false;
        }

        public static string GetUpdaterExe() => GetExe("../UUpdater");
        public static string GetNodeExe() => GetExe("Node");
        public static string GetNodeUIExe() => GetExe("NodeUI");
        public static string GetPingerExe() => GetExe("Pinger");


        static string GetExe(string filename)
        {
            var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : null;

            string thispath;
            if (Debugger.IsAttached) thispath = Assembly.GetCallingAssembly().Location;
            else thispath = Environment.ProcessPath!;

            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thispath)!, filename + extension));
        }
    }
}