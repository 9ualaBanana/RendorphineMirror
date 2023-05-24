global using System.Collections.Immutable;
global using System.Diagnostics;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using Avalonia;
global using Avalonia.Animation;
global using Avalonia.Controls;
global using Avalonia.Controls.Presenters;
global using Avalonia.Controls.Primitives;
global using Avalonia.Controls.Shapes;
global using Avalonia.Input;
global using Avalonia.Layout;
global using Avalonia.Media;
global using Avalonia.Media.Imaging;
global using Avalonia.Styling;
global using Avalonia.Threading;
global using Avalonia.VisualTree;
global using Common;
global using NLog;
global using Node.Common;
global using Node.Plugins.Models;
global using Node.Tasks.Models;
global using NodeCommon;
global using NodeCommon.ApiModel;
global using NodeCommon.Tasks;
global using NodeCommon.Tasks.Watching;
global using NodeToUI;
global using Node.UI.Controls;
global using Node.UI.Pages;
global using APath = Avalonia.Controls.Shapes.Path;
global using Path = System.IO.Path;
using Avalonia.Controls.ApplicationLifetimes;

namespace Node.UI;

static class Program
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    [STAThread]
    public static void Main(string[] args)
    {
        Initializer.AppName = "renderfin-ui";
        Init.Initialize();
        ConsoleHide.Hide();

        if (!Init.IsDebug && !Process.GetCurrentProcess().ProcessName.Contains("dotnet", StringComparison.Ordinal))
        {
            SendShowRequest();
            ListenForShowRequests();
        }

        Task.Run(WindowsTrayRefreshFix.RefreshTrayArea);
        if (!Init.IsDebug) Task.Run(CreateShortcuts);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new AvaloniaNativePlatformOptions { UseGpu = false }) // workaround for https://github.com/AvaloniaUI/Avalonia/issues/3533
        .With(new X11PlatformOptions { UseDBusMenu = true })
        .LogToTrace();


    /// <summary> Check if another instance is already running, send show request to it and quit </summary>
    static void SendShowRequest()
    {
        if (!FileList.GetAnotherInstances().Any()) return;

        var dir = Path.Combine(Path.GetTempPath(), "renderfinuireq");
        if (!Directory.Exists(dir)) return;

        File.Create(Path.Combine(dir, "show")).Dispose();
        Environment.Exit(0);
    }
    /// <summary> Start listening for outside requests to show the window </summary>
    static void ListenForShowRequests()
    {
        new Thread(() =>
        {
            var dir = Path.Combine(Path.GetTempPath(), "renderfinuireq");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);

            using var watcher = new FileSystemWatcher(dir);
            watcher.Created += (obj, e) =>
            {
                var action = Path.GetFileName(e.FullPath);
                if (action == "show")
                    Dispatcher.UIThread.Post(() => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Show());

                new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {
                            if (File.Exists(e.FullPath))
                                File.Delete(e.FullPath);
                            return;
                        }
                        catch { Thread.Sleep(1000); }
                    }
                })
                { IsBackground = true }.Start();
            };

            watcher.EnableRaisingEvents = true;
            Thread.Sleep(-1);
        })
        { IsBackground = true }.Start();
    }

    static void CreateShortcuts()
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT) return;

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var startmenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        try
        {
            File.Delete(Path.Combine(desktop, "Renderphine.url"));
            UISettings.ShortcutsCreated = false;
        }
        catch { }
        try
        {
            File.Delete(Path.Combine(startmenu, "Renderphine.url"));
            UISettings.ShortcutsCreated = false;
        }
        catch { }


        if (UISettings.ShortcutsCreated) return;

        try
        {
            var ico = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath!)!, "Resources", "img", "icon.ico");
            var data = @$"
                [InternetShortcut]
                URL=file:///{FileList.GetUpdaterExe()}
                IconIndex=0
                IconFile={ico.Replace('\\', '/')}
            ".TrimLines();


            write(Path.Combine(desktop, "Renderfin.url"), data);

            Directory.CreateDirectory(startmenu);
            write(Path.Combine(startmenu, "Renderfin.url"), data);
        }
        catch { }
        finally { UISettings.ShortcutsCreated = true; }


        static void write(string linkpath, string data)
        {
            _logger.Info($"Creating shortcut {linkpath}");
            File.WriteAllText(linkpath, data);
        }
    }
}