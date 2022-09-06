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
global using NodeToUI;
global using Common.Tasks;
global using NLog;
global using NodeUI.Controls;
global using NodeUI.Pages;
global using APath = Avalonia.Controls.Shapes.Path;
global using Path = System.IO.Path;

namespace NodeUI;

static class Program
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static void Main(string[] args)
    {
        Init.Initialize();
        ConsoleHide.Hide();

        if (!Debugger.IsAttached)
        {
            var updater = FileList.GetUpdaterExe();
            Process.Start(new ProcessStartInfo(updater) { WorkingDirectory = Path.GetDirectoryName(updater), CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = true })!.WaitForExit();
        }

        Task.Run(WindowsTrayRefreshFix.RefreshTrayArea);
        if (!Debugger.IsAttached)
        {
            FileList.KillNodeUI();
            Task.Run(CreateShortcuts);
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new X11PlatformOptions { UseDBusMenu = true })
        .LogToTrace();


    static void CreateShortcuts()
    {
        if (UISettings.ShortcutsCreated) return;

        try
        {
            var ico = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath!)!, "Resources", "img", "icon.ico");
            var data = @$"
                [InternetShortcut]
                URL=file:///{Environment.ProcessPath}
                IconIndex=0
                IconFile={ico.Replace('\\', '/')}
            ".TrimLines();


            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            write(Path.Combine(desktop, "Renderphin.url"), data);

            var startmenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            Directory.CreateDirectory(startmenu);
            write(Path.Combine(startmenu, "Renderphin.url"), data);
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