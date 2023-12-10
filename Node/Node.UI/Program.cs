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

namespace Node.UI;

static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = Init.CreateContainer(new("renderfin") { AutoClearTempDir = false }, typeof(Program).Assembly);

        builder.RegisterType<HttpClient>()
            .SingleInstance();
        builder.RegisterInstance(Api.Default)
            .SingleInstance();

        AppBuilder.Configure(App.Initialize(builder).Resolve<Application>)
            .UsePlatformDetect()
            .With(new X11PlatformOptions { UseDBusMenu = true })
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }
}
