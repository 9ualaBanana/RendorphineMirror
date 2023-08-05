using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Common
{
    public static class FileList
    {
        static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        public static IEnumerable<Process> GetProcesses(string file) =>
            Process.GetProcesses().Where(proc =>
            {
                try { return proc.MainModule?.FileName == file; }
                catch { return false; }
            });
        public static bool IsProcessRunning(string file) => GetProcesses(file).Any();
        public static bool IsAnotherProcessRunning(string file) => GetProcesses(file).Where(proc => proc.Id != Environment.ProcessId).Any();
        public static IEnumerable<Process> GetAnotherInstances() => GetProcesses(Environment.ProcessPath!).Where(proc => proc.Id != Environment.ProcessId);

        public static void KillProcesses() => KillProcesses(Path.GetFullPath(Path.GetDirectoryName(Environment.ProcessPath)!));
        public static void KillProcesses(string directory) => KillProcesses(path => path is not null && Path.GetFullPath(path).StartsWith(directory, StringComparison.Ordinal));
        public static void KillProcesses(Func<string?, bool> checkfunc)
        {
            var thisid = Environment.ProcessId;
            foreach (var proc in Process.GetProcesses())
            {
                if (proc.Id == thisid) continue;
                try { if (!checkfunc(proc.MainModule?.FileName)) continue; }
                catch { continue; }

                Logger.Info(@$"Killing {proc.Id} {proc.ProcessName}");
                try { proc.Kill(); }
                catch (Exception ex) { Logger.Error($"Could not kill {proc.Id}: {ex}"); }
            }
        }

        public static string GetUpdaterExe() => GetExe("Updater");
        public static string GetNodeExe() => GetExe("Node");
        public static string GetNodeUIExe() => GetExe("Node.UI");
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