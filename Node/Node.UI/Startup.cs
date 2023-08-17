using Avalonia.Controls.ApplicationLifetimes;

namespace Node.UI;

public class Startup
{
    public required NodeStateUpdater NodeStateUpdater { get; init; }
    public required IClassicDesktopStyleApplicationLifetime Lifetime { get; init; }
    public required MainWindowUpdater MainWindowUpdater { get; init; }
    public required TrayIndicator TrayIndicator { get; init; }
    public required App App { get; init; }

    public void Start()
    {
        App.Name = App.AppName;

        if (UISettings.Language is { } lang) LocalizedString.SetLocale(lang);
        else UISettings.Language = LocalizedString.Locale;

        TrayIndicator.Initialize();
        MainTheme.Apply(App.Resources, App.Styles);

        if (Environment.GetCommandLineArgs().Contains("registryeditor"))
        {
            Lifetime.MainWindow = new Window() { Content = new Pages.MainWindowTabs.JsonRegistryTab(), };
            return;
        }

        NodeStateUpdater.Start();
        NodeGlobalState.Instance.BAuthInfo.SubscribeChanged(() => Dispatcher.UIThread.Post(() => MainWindowUpdater.SetMainWindow().Show()));

        if (!Environment.GetCommandLineArgs().Contains("hidden"))
            MainWindowUpdater.SetMainWindow();
    }

}
