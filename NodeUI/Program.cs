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
global using NodeCommon;
global using NodeCommon.Plugins;
global using NodeCommon.Tasks;
global using NodeCommon.Tasks.Model;
global using NodeCommon.Tasks.Watching;
global using NodeToUI;
global using NodeUI.Controls;
global using NodeUI.Pages;
global using APath = Avalonia.Controls.Shapes.Path;
global using Path = System.IO.Path;
using System.Runtime.InteropServices;
using CefNet;

namespace NodeUI;

static class Program
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    [STAThread]
    public static void Main(string[] args)
    {
        Init.Initialize();
        ConsoleHide.Hide();

        if (args.Any(x => x.Contains("zygote", StringComparison.Ordinal) || x.Contains("sandbox", StringComparison.Ordinal) || x.StartsWith("--type", StringComparison.Ordinal)))
            InitializeCef();

        if (!Init.IsDebug)
        {
            var updater = FileList.GetUpdaterExe();
            Process.Start(new ProcessStartInfo(updater) { WorkingDirectory = Path.GetDirectoryName(updater), CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = true })!.WaitForExit();
        }

        Task.Run(WindowsTrayRefreshFix.RefreshTrayArea);
        if (!Init.IsDebug)
        {
            FileList.KillNodeUI();
            Task.Run(CreateShortcuts);
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new AvaloniaNativePlatformOptions { UseGpu = false }) // workaround for https://github.com/AvaloniaUI/Avalonia/issues/3533
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
            write(Path.Combine(desktop, "Renderfin.url"), data);

            var startmenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
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

    static bool CefInitialized = false;
    public static void InitializeCef()
    {
        if (CefInitialized) return;

        CefInitialized = true;
        var settings = new CefSettings()
        {
            NoSandbox = true,
            MultiThreadedMessageLoop = true,
            WindowlessRenderingEnabled = true,
            LogFile = Path.Combine(Path.GetTempPath(), "renderfin", "ceflog.log"),
            UserDataPath = "/temp/cef/data",
        };

        var app = new CefAppImpl();
        try { app.Initialize("assets/cef/", settings); }
        catch
        {
            try { app.Initialize("cef/", settings); }
            catch
            {
                try { app.Initialize("../assets/cef/", settings); }
                catch
                {
                    app.Initialize("../cef/", settings);

                }
            }
        }
    }


    class CefAppImpl : CefNetApplication
    {
        protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
        {
            base.OnBeforeCommandLineProcessing(processType, commandLine);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                commandLine.AppendSwitch("no-zygote");
                commandLine.AppendSwitch("no-sandbox");
            }

            commandLine.Program = "/temp/q/bin/Debug/net6.0/q";
            Console.WriteLine(commandLine.CommandLineString);

            /*commandLine.AppendSwitch("enable-devtools-experiments");

            commandLine.AppendSwitch("disable-gpu");
            commandLine.AppendSwitch("disable-gpu-compositing");
            commandLine.AppendSwitch("disable-gpu-vsync");

            commandLine.AppendSwitch("enable-begin-frame-scheduling");
            commandLine.AppendSwitch("enable-media-stream");
            commandLine.AppendSwitchWithValue("enable-blink-features", "CSSPseudoHas");*/

            /*Console.WriteLine("ChromiumWebBrowser_OnBeforeCommandLineProcessing");
            Console.WriteLine(commandLine.CommandLineString);

            //commandLine.AppendSwitchWithValue("proxy-server", "127.0.0.1:8888");

            commandLine.AppendSwitch("ignore-certificate-errors");
            commandLine.AppendSwitchWithValue("remote-debugging-port", "9222");

            //enable-devtools-experiments

            //e.CommandLine.AppendSwitchWithValue("user-agent", "Mozilla/5.0 (Windows 10.0) WebKa/" + DateTime.UtcNow.Ticks);

            //("force-device-scale-factor", "1");

            */
        }
    }
}