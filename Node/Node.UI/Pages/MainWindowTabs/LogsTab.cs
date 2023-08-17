using System.Text;

namespace Node.UI.Pages.MainWindowTabs;

public class LogsTab : Panel
{
    public LogsTab()
    {
        var tab = new TabbedControl();
        Children.Add(tab);

        tab.Add("node", new LogViewer("Node", LogLevel.Debug));
        tab.Add("nodeui", new LogViewer("Node.UI", LogLevel.Debug));
        tab.Add("node-trace", new LogViewer("Node", LogLevel.Trace));
        tab.Add("nodeui-trace", new LogViewer("Node.UI", LogLevel.Trace));
        tab.Add("ONLY WORKS WHEN TEXTBOX IS FOCUSED", new Panel());
    }


    class LogViewer : Panel
    {
        public LogViewer(string logName, LogLevel level)
        {
            var flogname = logName;
            string getlogdir() => Path.Combine(Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!, "logs", logName, level.ToString(), "log.log").Replace("Node.UI", flogname);

            var dir = getlogdir();
            if (!File.Exists(dir))
            {
                logName = "dotnet";
                dir = getlogdir();
            }

            var tb = new TextBox() { AcceptsReturn = true };
            Children.Add(new Grid()
            {
                RowDefinitions = RowDefinitions.Parse("Auto *"),
                Children =
                    {
                        new TextBlock() { Text = dir }.WithRow(0),
                        tb.WithRow(1),
                    },
            });


            new Thread(() =>
            {
                var buffer = new byte[1024 * 8];

                while (true)
                {
                    Thread.Sleep(1000);

                    try
                    {
                        var visible = Dispatcher.UIThread.InvokeAsync(() => tb.IsFocused).Result;
                        if (!visible) continue;

                        int read = 0;
                        if (File.Exists(dir))
                        {
                            using var reader = File.Open(dir, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            reader.Position = Math.Max(reader.Length - buffer.Length, 0);
                            read = reader.Read(buffer);
                        }

                        var str = $"{Encoding.UTF8.GetString(buffer.AsSpan(0, read))}\n\n<read on {DateTime.UtcNow}>";
                        Dispatcher.UIThread.Post(() =>
                        {
                            tb.Text = str;
                            tb.CaretIndex = tb.Text.Length;
                        });
                    }
                    catch { }
                }
            })
            { IsBackground = true }.Start();
        }
    }
}