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
global using NodeUI.Controls;
global using NodeUI.Pages;
global using Serilog;
global using APath = Avalonia.Controls.Shapes.Path;
global using Path = System.IO.Path;
using System.Runtime.InteropServices;

namespace NodeUI;

static class Program
{
    public static void Main(string[] args)
    {
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
        if (Settings.ShortcutsCreated) return;

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
            write(Path.Combine(desktop, "Renderphine.url"), data);

            var startmenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            Directory.CreateDirectory(startmenu);
            write(Path.Combine(startmenu, "Renderphine.url"), data);
        }
        catch { }
        finally { Settings.ShortcutsCreated = true; }


        static void write(string linkpath, string data)
        {
            Log.Information($"Creating shortcut {linkpath}");
            File.WriteAllText(linkpath, data);
        }
    }
}