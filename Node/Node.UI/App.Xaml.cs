using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Node.UI
{
    public class App : Application
    {
        public static App Instance => (App) Current.ThrowIfNull();

        public string Version => Init.Version;
        public string AppName => $"Renderfin   v{Version}";
        public WindowIcon Icon { get; } = new WindowIcon(Resource.LoadStream(typeof(App).Assembly, Environment.OSVersion.Platform == PlatformID.Win32NT ? "img.icon.ico" : "img.tray_icon.png"));

        public required NodeGlobalState NodeGlobalState { get; init; }
        public required UISettings Settings { get; init; }
        public required DataDirs Dirs { get; init; }
        public required Init Init { get; init; }
        bool WasConnected = false;

        public override void Initialize() => AvaloniaXamlLoader.Load(this);
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                throw new InvalidOperationException("Non-desktop platforms are not supported");

            Start();
            base.OnFrameworkInitializationCompleted();


            void Start()
            {
                Name = AppName;

                if (Settings.Language is { } lang) LocalizedString.SetLocale(lang);
                else Settings.Language = LocalizedString.Locale;

                this.InitializeTrayIndicator();
                MainTheme.Apply(Resources, Styles);

                if (Environment.GetCommandLineArgs().Contains("registryeditor"))
                {
                    desktop.MainWindow = new Window() { Content = new Pages.MainWindowTabs.JsonRegistryTab(), };
                    return;
                }

                NodeStateUpdater.Start(Dirs);
                NodeStateUpdater.IsConnectedToNode.SubscribeChanged(() => Dispatcher.UIThread.Post(() => SetMainWindow(desktop).Show()));
                NodeGlobalState.Instance.BAuthInfo.SubscribeChanged(() => Dispatcher.UIThread.Post(() => SetMainWindow(desktop).Show()));

                if (!Environment.GetCommandLineArgs().Contains("hidden"))
                    SetMainWindow(desktop);
            }
        }

        public Window SetMainWindow(IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (WasConnected && NodeGlobalState.AuthInfo?.SessionId is not null)
                return lifetime.MainWindow;

            WasConnected |= NodeStateUpdater.IsConnectedToNode.Value && NodeGlobalState.AuthInfo?.SessionId is not null;

            if (lifetime.MainWindow is MainWindow && NodeStateUpdater.IsConnectedToNode.Value && NodeGlobalState.AuthInfo?.SessionId is not null)
                return lifetime.MainWindow;

            lifetime.MainWindow?.Hide();
            return lifetime.MainWindow =
                (!NodeStateUpdater.IsConnectedToNode.Value)
                ? new InitializingWindow()
                : NodeGlobalState.AuthInfo?.SessionId is null
                    ? new LoginWindow()
                    : new MainWindow();
        }
    }
}