using Autofac;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Node.UI
{
    public class App : AppBase
    {
        public static new App Current => (App) Instance;

        public required NodeGlobalState NodeGlobalState { get; init; }
        public required NodeStateUpdater NodeStateUpdater { get; init; }
        public required NodeConnectionState NodeConnectionState { get; init; }
        public required Updaters.BalanceUpdater BalanceUpdater { get; init; }
        public required Updaters.SoftwareUpdater SoftwareUpdater { get; init; }
        public required Updaters.SoftwareStatsUpdater SoftwareStatsUpdater { get; init; }
        bool WasConnected = false;

        public static IContainer Initialize(ContainerBuilder builder)
        {
            builder.RegisterType<App>()
                .As<Application>()
                .SingleInstance();

            builder.RegisterType<UIApis>()
                .As<Apis>()
                .SingleInstance();

            builder.RegisterType<UISettings>()
                .SingleInstance();
            builder.RegisterType<NodeStateUpdater>()
                .SingleInstance();
            builder.RegisterType<NodeConnectionState>()
                .SingleInstance();

            builder.RegisterInstance(NodeGlobalState.Instance)
                .SingleInstance();
            builder.Register(ctx => ctx.Resolve<NodeGlobalState>().Software)
                .AsSelf()
                .AsReadOnlyBindable()
                .SingleInstance();

            builder.RegisterSource<AutoControlRegistrator>();
            var container = builder.Build();

            var init = container.Resolve<Init>();
            if (!init.IsDebug && !Process.GetCurrentProcess().ProcessName.Contains("dotnet", StringComparison.Ordinal))
            {
                SendShowRequest();
                ListenForShowRequests();
            }

            Task.Run(WindowsTrayRefreshFix.RefreshTrayArea);

            return container;


            /// <summary> Check if another instance is already running, send show request to it and quit </summary>
            static void SendShowRequest()
            {
                if (!FileList.GetAnotherInstances().Any()) return;

                var dir = Path.Combine(Path.GetTempPath(), "renderfinuireq");
                if (!Directory.Exists(dir)) return;

                File.Create(Path.Combine(dir, "show")).Dispose();
                Environment.Exit(0);
            }
            /// <summary> Start listening for outside requests to show the window </summary>
            static void ListenForShowRequests()
            {
                new Thread(() =>
                {
                    var dir = Path.Combine(Path.GetTempPath(), "renderfinuireq");
                    if (Directory.Exists(dir)) Directory.Delete(dir, true);
                    Directory.CreateDirectory(dir);

                    using var watcher = new FileSystemWatcher(dir);
                    watcher.Created += (obj, e) =>
                    {
                        var action = Path.GetFileName(e.FullPath);
                        if (action == "show")
                            Dispatcher.UIThread.Post(() => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Show());

                        new Thread(() =>
                        {
                            while (true)
                            {
                                try
                                {
                                    if (File.Exists(e.FullPath))
                                        File.Delete(e.FullPath);
                                    return;
                                }
                                catch { Thread.Sleep(1000); }
                            }
                        })
                        { IsBackground = true }.Start();
                    };

                    watcher.EnableRaisingEvents = true;
                    Thread.Sleep(-1);
                })
                { IsBackground = true }.Start();
            }
        }

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

                this.InitializeTrayIndicator(NodeConnectionState);
                if (!Init.IsDebug) Task.Run(CreateShortcuts);
                MainTheme.Apply(Resources, Styles);

                BalanceUpdater.Start(NodeConnectionState.IsConnectedToNode, NodeGlobalState.Balance, default).Consume();
                SoftwareUpdater.Start(NodeConnectionState.IsConnectedToNode, NodeGlobalState.Software, default).Consume();
                SoftwareStatsUpdater.Start(NodeConnectionState.IsConnectedToNode, NodeGlobalState.SoftwareStats, default).Consume();

                StartUpdaterLoop().Consume();

                NodeConnectionState.IsConnectedToNode.SubscribeChanged(() => Dispatcher.UIThread.Post(() => SetMainWindow(desktop)?.Show()));
                NodeGlobalState.AuthInfo.SubscribeChanged(() => Dispatcher.UIThread.Post(() => SetMainWindow(desktop)?.Show()));

                if (!Environment.GetCommandLineArgs().Contains("hidden"))
                    SetMainWindow(desktop);
            }
        }

        public Window? SetMainWindow(IClassicDesktopStyleApplicationLifetime lifetime)
        {
            var window = getWindow();
            LogManager.GetCurrentClassLogger().Info($"Switching main window from {lifetime.MainWindow?.ToString() ?? "nothing"} to {window}");

            if (window?.GetType() == lifetime.MainWindow?.GetType())
                return lifetime.MainWindow;

            if (lifetime.MainWindow is { } prev)
            {
                // closing the current window immediately will just close the whole app since there's no active main window
                prev.Hide();
                Task.Delay(2000).ContinueWith(_ => Dispatcher.UIThread.Post(prev.Close));
            }

            return lifetime.MainWindow = window;


            Window? getWindow()
            {
                if (WasConnected && NodeGlobalState.AuthInfo.Value?.SessionId is not null)
                    return lifetime.MainWindow.ThrowIfNull();

                WasConnected |= NodeConnectionState.IsConnectedToNode.Value && NodeGlobalState.AuthInfo.Value?.SessionId is not null;

                if (lifetime.MainWindow is MainWindow && NodeConnectionState.IsConnectedToNode.Value && NodeGlobalState.AuthInfo.Value?.SessionId is not null)
                    return lifetime.MainWindow;

                return (!NodeConnectionState.IsConnectedToNode.Value)
                    ? null
                    : NodeGlobalState.AuthInfo.Value?.SessionId is null
                        ? new LoginWindow()
                        : new MainWindow(NodeConnectionState);
            }
        }

        async Task StartUpdaterLoop()
        {
            if (Environment.GetCommandLineArgs().Contains("--debug"))
            {
                var updater = new DebugNodeStateUpdater() { NodeConnectionState = NodeConnectionState };
                var window = new DebugNodeStateUpdaterWindow(updater);

                window.Show();
                return;
            }

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

        void CreateShortcuts()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return;

            var settings = Settings;
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var startmenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            try
            {
                File.Delete(Path.Combine(desktop, "Renderphine.url"));
                settings.ShortcutsCreated = false;
            }
            catch { }
            try
            {
                File.Delete(Path.Combine(startmenu, "Renderphine.url"));
                settings.ShortcutsCreated = false;
            }
            catch { }


            if (settings.ShortcutsCreated) return;

            try
            {
                var ico = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath!)!, "Resources", "img", "icon.ico");
                var data = @$"
                [InternetShortcut]
                URL=file:///{FileList.GetUpdaterExe()}
                IconIndex=0
                IconFile={ico.Replace('\\', '/')}
            ".TrimLines();


                write(Path.Combine(desktop, "Renderfin.url"), data);

                Directory.CreateDirectory(startmenu);
                write(Path.Combine(startmenu, "Renderfin.url"), data);
            }
            catch { }
            finally { settings.ShortcutsCreated = true; }


            void write(string linkpath, string data)
            {
                Logger.LogInformation($"Creating shortcut {linkpath}");
                File.WriteAllText(linkpath, data);
            }
        }
    }
}
