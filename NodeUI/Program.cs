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
using System.Runtime.InteropServices;

namespace NodeUI;

static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHide.Hide();
        WindowsTrayRefreshFix.RefreshTrayArea();
        if (!Debugger.IsAttached)
            FileList.KillNodeUI();

        // check and elevate privileges
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try { File.OpenWrite(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "temp")).Dispose(); }
            catch (UnauthorizedAccessException)
            {
                var proc = new ProcessStartInfo(Environment.ProcessPath!)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                };
                foreach (var arg in args) proc.ArgumentList.Add(arg);
                Process.Start(proc);

                return;
            }
        }


        Init.InitLogger();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new X11PlatformOptions { UseDBusMenu = true })
        .LogToTrace();
}