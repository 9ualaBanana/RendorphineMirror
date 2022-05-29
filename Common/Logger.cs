namespace Common
{
    public enum LogLevel { None, Basic, All }
    public static class Logger
    {
        static readonly string LatestLogName = Path.Combine(Variables.ConfigDirectory, "latest.log");
        static readonly string SecondLogName = Path.Combine(Variables.ConfigDirectory, "latest_0.log");

        public static bool Enabled = true;

        static StreamWriter? Stream;
        static readonly object LockObject = new object();

        static volatile bool IsInsideExceptionDelegate = false;

        static Logger()
        {
            if (File.Exists(LatestLogName))
                File.Move(LatestLogName, SecondLogName, true);
        }

        public static void InitializeExceptionLogging()
        {
            AppDomain.CurrentDomain.FirstChanceException += (_, ex) =>
            {
                if (IsInsideExceptionDelegate) return;
                IsInsideExceptionDelegate = true;

                try
                {
                    if (ex.Exception.GetType().Name == "DBusException") return;
                    if (ex.Exception is TaskCanceledException) return;
                    if (ex.Exception is OperationCanceledException) return;

                    Logger.Log("[FirstChanceException] " + ex.Exception, false);
                }
                finally { IsInsideExceptionDelegate = false; }
            };

            AppDomain.CurrentDomain.UnhandledException += (_, ex) => Logger.Log("[UnhandledException] " + ex?.ExceptionObject, false);
            TaskScheduler.UnobservedTaskException += (_, ex) => Logger.Log("[UnobservedTaskException] " + ex?.Exception, false);
        }
        static StreamWriter OpenFile() => new StreamWriter(File.Open(LatestLogName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));

        public static void Log(LocalizedString text, bool important = true) => Log(text.ToString(), important);
        public static void Log(string text, bool writeToConsole = false, bool important = true) => Log(Console.ForegroundColor, text, writeToConsole, important);
        public static void Log(ConsoleColor color, string text, bool writeToConsole = true, bool important = true) => Task.Run(() =>
        {
            try
            {
                if (Settings.LogLevel == LogLevel.None) return;
                if (Settings.LogLevel == LogLevel.Basic && !important) return;

                lock (LockObject)
                {
                    try { logInternal(color, text, writeToConsole); }
                    catch
                    {
                        try
                        {
                            Stream?.Close();
                            Stream = null;

                            logInternal(color, text, writeToConsole);
                        }
                        catch (Exception ex) { Console.WriteLine("Could not write to log: " + ex); }
                    }
                }
            }
            catch { }


            static void logInternal(ConsoleColor color, string text, bool writeToConsole = true)
            {
                if (writeToConsole) Console.Write(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "  ");
                Stream ??= OpenFile();

                var prevcolor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                if (writeToConsole) Console.WriteLine(text);

                Console.ForegroundColor = prevcolor;
                Stream.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "  " + text);
                Stream.Flush();
            }
        });

        public static void LogErr(string text) => Log("[Err] " + text);
        public static void LogWarn(string text) => Log("[Warn] " + text);

        public static void LogErr<T>(T value) => LogErr(value?.ToString() ?? string.Empty);
        public static void LogWarn<T>(T value) => LogWarn(value?.ToString() ?? string.Empty);
    }
}