using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Common
{
    public static class FileList
    {
        public static IEnumerable<Process> GetProcesses()
        {
            var thisdir = Path.GetDirectoryName(Environment.ProcessPath!)!;
            var executables = GetExecutables(thisdir, true, false).ToArray();

            return Process.GetProcesses().Where(x => filter(x, executables.Contains));

            static bool filter(Process proc, Func<string, bool> check)
            {
                try
                {
                    var module = proc.MainModule;
                    if (module?.FileName is null) return false;

                    return check(Path.GetFullPath(module.FileName));
                }
                catch { return false; }
            }
        }

        public static IEnumerable<string> GetExecutables(string directory, bool withUpdater, bool withUI)
        {
            yield return GetNodeExe();
            yield return GetPingerExe();

            if (withUI) yield return GetNodeUIExe();
            if (withUpdater) yield return GetUpdaterExe();
        }
        public static string GetUpdaterExe() => GetExe("../UUpdater");
        public static string GetNodeExe() => GetExe("Node");
        public static string GetNodeUIExe() => GetExe("NodeUI");
        public static string GetPingerExe() => GetExe("Pinger");


        static string GetExe(string filename)
        {
            var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "exe" : null;

            string thispath;
            if (Debugger.IsAttached) thispath = Assembly.GetCallingAssembly().Location;
            else thispath = Environment.ProcessPath!;

            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thispath)!, filename + extension));
        }
    }
}