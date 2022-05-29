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

namespace NodeUI;

static class Program
{
    public static void Main(string[] args)
    {
        Init.InitLogger();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new X11PlatformOptions { UseDBusMenu = true })
        .LogToTrace();
}