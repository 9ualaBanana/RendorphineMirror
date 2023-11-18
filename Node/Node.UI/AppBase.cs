using Autofac;

namespace Node.UI;

public class AppBase : Application
{
    public static AppBase Instance => (AppBase) Current.ThrowIfNull();

    public string Version => Init.Version;
    public string AppName => $"Renderfin   v{Version}";
    public WindowIcon Icon { get; } = new WindowIcon(Resource.LoadStream(typeof(App).Assembly, Environment.OSVersion.Platform == PlatformID.Win32NT ? "img.icon.ico" : "img.tray_icon.png"));

    public required UISettings Settings { get; init; }
    public required DataDirs Dirs { get; init; }
    public required Init Init { get; init; }
    public required ILifetimeScope Container { get; init; }
    public required ILogger<App> Logger { get; init; }
}
