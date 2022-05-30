using System.Diagnostics;

namespace Common
{
    public static class FileList
    {
        public static IEnumerable<Process> GetProcesses()
        {
            var thisdir = Path.GetDirectoryName(Environment.ProcessPath!)!;
            var executables = GetExecutables(thisdir, true, false).ToArray();
            Console.WriteLine("Should be killing " + string.Join(',', executables));

            return Process.GetProcesses().Where(x => filter(x, executables.Contains));

            static bool filter(Process proc, Func<string, bool> check)
            {
                try
                {
                    var module = proc.MainModule;
                    if (module?.FileName is null) return false;

                    var fpath = Path.GetFullPath(module.FileName);
                    Console.WriteLine(fpath);
                    return check(fpath);
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
            var thispath = Environment.ProcessPath!;
            var exeextenion = Path.GetExtension(thispath);
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thispath)!, filename + exeextenion));
        }
    }
}