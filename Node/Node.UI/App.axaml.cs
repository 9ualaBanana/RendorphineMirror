using Autofac;
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
        public required NodeStateUpdater NodeStateUpdater { get; init; }
        public required Updaters.BalanceUpdater BalanceUpdater { get; init; }
        public required Updaters.SoftwareUpdater SoftwareUpdater { get; init; }
        public required Updaters.SoftwareStatsUpdater SoftwareStatsUpdater { get; init; }
        public required ILogger<App> Logger { get; init; }
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

                this.InitializeTrayIndicator(NodeStateUpdater);
                MainTheme.Apply(Resources, Styles);

                BalanceUpdater.Start(NodeStateUpdater.IsConnectedToNode, NodeGlobalState.Balance, default);
                SoftwareUpdater.Start(NodeStateUpdater.IsConnectedToNode, NodeGlobalState.Software, default);
                SoftwareStatsUpdater.Start(NodeStateUpdater.IsConnectedToNode, NodeGlobalState.SoftwareStats, default);

                if (Environment.GetCommandLineArgs().Contains("registryeditor"))
                {
                    desktop.MainWindow = new Window() { Content = new Pages.MainWindowTabs.RegistryEditor(NodeGlobalState), };
                    return;
                }

                StartUpdaterLoop().Consume();

                NodeStateUpdater.IsConnectedToNode.SubscribeChanged(() => Dispatcher.UIThread.Post(() => SetMainWindow(desktop).Show()));
                NodeGlobalState.AuthInfo.SubscribeChanged(() => Dispatcher.UIThread.Post(() => SetMainWindow(desktop).Show()));

                if (!Environment.GetCommandLineArgs().Contains("hidden"))
                    SetMainWindow(desktop);
            }
        }

        public Window SetMainWindow(IClassicDesktopStyleApplicationLifetime lifetime)
        {
            var window = getWindow();
            LogManager.GetCurrentClassLogger().Info($"Switching main window from {lifetime.MainWindow?.ToString() ?? "nothing"} to {window}");

            if (window.GetType() == lifetime.MainWindow?.GetType())
                return lifetime.MainWindow;

            if (lifetime.MainWindow is { } prev)
            {
                // closing the current window immediately will just close the whole app since there's no active main window
                prev.Hide();
                Task.Delay(2000).ContinueWith(_ => Dispatcher.UIThread.Post(prev.Close));
            }

            return lifetime.MainWindow = window;


            Window getWindow()
            {
                if (WasConnected && NodeGlobalState.AuthInfo.Value?.SessionId is not null)
                    return lifetime.MainWindow.ThrowIfNull();

                WasConnected |= NodeStateUpdater.IsConnectedToNode.Value && NodeGlobalState.AuthInfo.Value?.SessionId is not null;

                if (lifetime.MainWindow is MainWindow && NodeStateUpdater.IsConnectedToNode.Value && NodeGlobalState.AuthInfo.Value?.SessionId is not null)
                    return lifetime.MainWindow;

                return (!NodeStateUpdater.IsConnectedToNode.Value)
                    ? new InitializingWindow()
                    : NodeGlobalState.AuthInfo.Value?.SessionId is null
                        ? new LoginWindow()
                        : new MainWindow(NodeStateUpdater);
            }
        }

        async Task StartUpdaterLoop()
        {
            var loadcache = Init.IsDebug;
            var cacheloaded = !loadcache;

            var cachefile = Dirs.DataFile("nodeinfocache");
            if (loadcache)
            {
                NodeGlobalState.AnyChanged.Subscribe(NodeGlobalState, _ =>
                    File.WriteAllText(cachefile, JsonConvert.SerializeObject(NodeGlobalState, JsonSettings.Typed)));
            }


            NodeStateUpdater.OnException += _ =>
            {
                if (cacheloaded) return;
                cacheloaded = true;

                if (!File.Exists(cachefile)) return;
                try { JsonConvert.PopulateObject(File.ReadAllText(cachefile), NodeGlobalState, JsonSettings.Typed); }
                catch { }
            };
            NodeStateUpdater.OnReceive += info =>
            {
                if (info.Type != NodeStateUpdate.UpdateType.State)
                    return;

                var jtoken = info.Value;
                Logger.LogTrace($"Node state updated: {string.Join(", ", (jtoken as JObject)?.Properties().Select(x => x.Name) ?? new[] { jtoken.ToString(Formatting.None) })}");
                cacheloaded = true;

                using var tokenreader = jtoken.CreateReader();
                JsonSettings.TypedS.Populate(tokenreader, NodeGlobalState);
            };


            await NodeStateUpdater.ReceivingLoop();
        }
    }
}
