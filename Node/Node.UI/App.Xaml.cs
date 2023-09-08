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

                if (UISettings.Language is { } lang) LocalizedString.SetLocale(lang);
                else UISettings.Language = LocalizedString.Locale;

                this.InitializeTrayIndicator();
                MainTheme.Apply(Resources, Styles);

                if (Environment.GetCommandLineArgs().Contains("registryeditor"))
                {
                    desktop.MainWindow = new Window() { Content = new Pages.MainWindowTabs.JsonRegistryTab(), };
                    return;
                }

                NodeStateUpdater.Start();
                NodeStateUpdater.IsConnectedToNode.SubscribeChanged(() => Dispatcher.UIThread.Post(() => SetMainWindow(desktop).Show()));
                NodeGlobalState.Instance.BAuthInfo.SubscribeChanged(() => Dispatcher.UIThread.Post(() => SetMainWindow(desktop).Show()));

                if (!Environment.GetCommandLineArgs().Contains("hidden"))
                    SetMainWindow(desktop);
            }
        }

        public static Window SetMainWindow(IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (WasConnected && NodeGlobalState.Instance.AuthInfo?.SessionId is not null)
                return lifetime.MainWindow;

            WasConnected |= NodeStateUpdater.IsConnectedToNode.Value && NodeGlobalState.Instance.AuthInfo?.SessionId is not null;

            if (lifetime.MainWindow is MainWindow && NodeStateUpdater.IsConnectedToNode.Value && NodeGlobalState.Instance.AuthInfo?.SessionId is not null)
                return lifetime.MainWindow;

            lifetime.MainWindow?.Hide();
            return lifetime.MainWindow =
                (!NodeStateUpdater.IsConnectedToNode.Value)
                ? new InitializingWindow()
                : NodeGlobalState.Instance.AuthInfo?.SessionId is null
                    ? new LoginWindow()
                    : new MainWindow();
        }
    }
}