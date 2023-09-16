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
global using Microsoft.Extensions.Logging;
global using NLog;
global using Node.Common;
global using Node.Common.Models;
global using Node.Plugins.Models;
global using Node.Tasks.Models;
global using Node.UI.Controls;
global using Node.UI.Pages;
global using NodeCommon;
global using NodeCommon.ApiModel;
global using NodeCommon.Tasks;
global using NodeCommon.Tasks.Watching;
global using NodeToUI;
global using APath = Avalonia.Controls.Shapes.Path;
global using LogLevel = NLog.LogLevel;
global using Path = System.IO.Path;
using Autofac;
using Avalonia.Controls.ApplicationLifetimes;

namespace Node.UI;

static class Program
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    [STAThread]
    public static void Main(string[] args)
    {
        var builder = Init.CreateContainer(new("renderfin-ui"), typeof(Program).Assembly);
        builder.RegisterType<App>()
            .SingleInstance();

        builder.RegisterType<HttpClient>()
            .SingleInstance();
        builder.RegisterInstance(Api.Default)
            .SingleInstance();
        builder.RegisterInstance(Apis.Default)
            .SingleInstance();

        builder.RegisterType<UISettings>()
            .SingleInstance();
        builder.RegisterInstance(NodeGlobalState.Instance)
            .SingleInstance();
        builder.RegisterType<NodeStateUpdater>()
            .SingleInstance();

        var container = builder.Build();

        var init = container.Resolve<Init>();

        if (!init.IsDebug && !Process.GetCurrentProcess().ProcessName.Contains("dotnet", StringComparison.Ordinal))
        {
            SendShowRequest();
            ListenForShowRequests();
        }

        Task.Run(WindowsTrayRefreshFix.RefreshTrayArea);
        if (!init.IsDebug) Task.Run(CreateShortcuts);


        AppBuilder.Configure(container.Resolve<App>)
            .UsePlatformDetect()
            .With(new AvaloniaNativePlatformOptions { UseGpu = false }) // workaround for https://github.com/AvaloniaUI/Avalonia/issues/3533
            .With(new X11PlatformOptions { UseDBusMenu = true })
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }


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

        var settings = App.Instance.Settings;
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var startmenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        try
        {
            File.Delete(Path.Combine(desktop, "Renderphine.url"));
            settings.ShortcutsCreated = false;
        }
        catch { }
        try
        {
            File.Delete(Path.Combine(startmenu, "Renderphine.url"));
            settings.ShortcutsCreated = false;
        }
        catch { }


        if (settings.ShortcutsCreated) return;

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
        finally { settings.ShortcutsCreated = true; }


        static void write(string linkpath, string data)
        {
            _logger.Info($"Creating shortcut {linkpath}");
            File.WriteAllText(linkpath, data);
        }
    }
}